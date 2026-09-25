// Keep files and session IDs across navigation and worker restarts; no external CDN dependency.
const CHUNK_SIZE = 5 * 1024 * 1024;
const active = new Map();
const cancelling = new Set();
let configuration = { apiBaseUrl: '', authToken: null };
let database;

function owner(token) {
    try {
        const claims = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
        return claims.userId ? `${claims.userId}:${claims.blogId || ''}` : null;
    } catch { return null; }
}

function openDatabase() {
    database ??= new Promise((resolve, reject) => {
        const request = indexedDB.open('video-background-uploads', 1);
        request.onupgradeneeded = () => {
            request.result.createObjectStore('jobs', { keyPath: 'postId' });
            request.result.createObjectStore('files');
        };
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => { database = undefined; reject(request.error); };
    });
    return database;
}

async function read(store, key) {
    const db = await openDatabase();
    return new Promise((resolve, reject) => {
        const objectStore = db.transaction(store).objectStore(store);
        const request = key === undefined ? objectStore.getAll() : objectStore.get(key);
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}

async function save(job, file) {
    const db = await openDatabase();
    await new Promise((resolve, reject) => {
        const tx = db.transaction(['jobs', 'files'], 'readwrite');
        tx.objectStore('jobs').put(job);
        if (file) tx.objectStore('files').put(file, job.postId);
        if (job.status === 'completed' || job.status === 'cancelled') tx.objectStore('files').delete(job.postId);
        tx.oncomplete = resolve;
        tx.onabort = () => reject(tx.error || new Error('Не удалось сохранить видео на устройстве'));
        tx.onerror = () => reject(tx.error);
    });
}

function snapshot(job) {
    return { postId: job.postId, status: job.status, progress: job.progress, error: job.error };
}

async function broadcast(message) {
    const clients = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
    clients.forEach(client => client.postMessage(message));
}

async function report(job, status, progress = job.progress, error) {
    Object.assign(job, { status, progress, error });
    await save(job);
    await broadcast({ type: 'UPLOAD_STATUS', payload: snapshot(job) });
}

async function api(job, path, data, signal) {
    if (!configuration.authToken || owner(configuration.authToken) !== job.owner) {
        throw new Error('Войдите в аккаунт, с которого начата загрузка');
    }
    const response = await fetch(`${job.apiBaseUrl}/video/api/VideoUpload/${path}`, {
        method: data === undefined ? 'GET' : 'POST',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${configuration.authToken}` },
        credentials: 'include',
        body: data === undefined ? undefined : JSON.stringify(data), signal
    });
    if (!response.ok) {
        const error = new Error(response.status === 401 ? 'Войдите снова, чтобы продолжить загрузку' : `Ошибка загрузки: HTTP ${response.status}`);
        error.status = response.status;
        throw error;
    }
    const text = await response.text();
    if (!text) return null;
    try { return JSON.parse(text); } catch { return text; }
}

async function upload(job, controller) {
    const signal = controller.signal;
    try {
        const file = await read('files', job.postId);
        if (!file) throw new Error('Локальный видеофайл не найден');
        await report(job, 'uploading');
        if (!job.uploadId) {
            const session = await api(job, 'initiate', {
                postId: job.postId, objectName: file.name, fileName: file.name,
                size: file.size, contentType: file.type, duration: job.duration,
                fileExtension: file.name.includes('.') ? file.name.slice(file.name.lastIndexOf('.')).toLowerCase() : ''
            }, signal);
            if (!session?.uploadId) throw new Error('Сервис не вернул идентификатор загрузки');
            job.uploadId = session.uploadId;
            await save(job);
        }
        const session = await api(job, `session/${encodeURIComponent(job.uploadId)}`, undefined, signal);
        if (session.status !== 0 && session.status !== 1) throw new Error('Сессия загрузки больше недоступна');
        // Repeat complete even when storage finished: the API also schedules server processing.
        const parts = session.status === 1 ? [] : await api(job, `parts/${encodeURIComponent(job.uploadId)}`, undefined, signal);
        const total = Math.ceil(file.size / CHUNK_SIZE);
        const uploaded = new Set(parts.map(part => part.partNumber));
        const pending = session.status === 1 ? [] : Array.from({ length: total }, (_, i) => i + 1).filter(n => !uploaded.has(n));
        await report(job, 'uploading', Math.min(99, Math.round(parts.length / total * 100)));
        for (let index = 0; index < pending.length; index += 5) {
            signal.throwIfAborted();
            const results = await Promise.allSettled(pending.slice(index, index + 5).map(async partNumber => {
                for (let attempt = 0; attempt < 3; attempt++) {
                    signal.throwIfAborted();
                    try {
                        const { url } = await api(job, 'generate-url', { uploadId: job.uploadId, partNumber, expiryMinutes: 10 }, signal);
                        if (!url) throw new Error('Сервис не вернул URL загрузки');
                        const chunk = file.slice((partNumber - 1) * CHUNK_SIZE, partNumber * CHUNK_SIZE);
                        const response = await fetch(url, { method: 'PUT', body: chunk, signal, headers: { 'Content-Type': 'application/octet-stream' } });
                        if (!response.ok) throw new Error(`Ошибка отправки части: HTTP ${response.status}`);
                        const eTag = response.headers.get('ETag')?.replace(/"/g, '');
                        if (!eTag) throw new Error('Хранилище не предоставило заголовок ETag');
                        parts.push({ partNumber, eTag, size: chunk.size, uploadedAt: new Date().toISOString() });
                        await report(job, 'uploading', Math.min(99, Math.round(parts.length / total * 100)));
                        return;
                    } catch (error) {
                        if (signal.aborted || error.status === 401 || attempt === 2) throw error;
                        await new Promise(resolve => setTimeout(resolve, 500 * 2 ** attempt));
                    }
                }
            }));
            // Settle the entire batch before retry/cancel can start another upload.
            const failed = results.find(result => result.status === 'rejected');
            if (failed) throw failed.reason;
        }
        signal.throwIfAborted();
        await report(job, 'completing', 99);
        await api(job, 'complete', { uploadId: job.uploadId, parts: parts.sort((a, b) => a.partNumber - b.partNumber) }, signal);
        await report(job, 'completed', 100);
    } catch (error) {
        await report(job, 'failed', job.progress, error.message || 'Не удалось загрузить видео');
        if (error.status === 401) await broadcast({ type: 'UPLOAD_AUTH_REQUIRED' });
    }
}

function start(job) {
    if (cancelling.has(job.postId)) return Promise.resolve();
    if (active.has(job.postId)) return active.get(job.postId).promise;
    if (['completed', 'cancelled'].includes(job.status) || job.owner !== owner(configuration.authToken)) return Promise.resolve();
    const controller = new AbortController();
    const entry = { controller, promise: null };
    active.set(job.postId, entry);
    entry.promise = upload(job, controller).finally(() => active.delete(job.postId));
    return entry.promise;
}

async function resumeAll() {
    const jobs = await read('jobs');
    await Promise.all(jobs.map(start));
}

self.addEventListener('install', event => event.waitUntil(self.skipWaiting()));
self.addEventListener('activate', event => event.waitUntil(self.clients.claim()));
self.addEventListener('message', event => {
    // Call synchronously; replying to the form must not end the upload event's lifetime.
    event.waitUntil(handleMessage(event).catch(error => {
        event.ports[0]?.postMessage({ ok: false, error: error.message || 'Ошибка фоновой загрузки' });
    }));
});

async function handleMessage(event) {
    const { type, payload } = event.data || {};
    const reply = value => event.ports[0]?.postMessage({ ok: true, value });
    if (type === 'CONFIGURE' || type === 'SET_AUTH_TOKEN') {
        const previousOwner = owner(configuration.authToken);
        configuration = type === 'CONFIGURE'
            ? { apiBaseUrl: payload.apiBaseUrl.replace(/\/$/, ''), authToken: payload.authToken }
            : { ...configuration, authToken: payload };
        if (previousOwner !== owner(configuration.authToken)) {
            const running = [...active.values()];
            running.forEach(entry => entry.controller.abort());
            await Promise.allSettled(running.map(entry => entry.promise));
        }
        reply();
        if (type === 'SET_AUTH_TOKEN' || payload.resume) return resumeAll();
        return;
    }
    if (type === 'ENQUEUE_UPLOAD') {
        if (!owner(configuration.authToken) || !configuration.apiBaseUrl) throw new Error('Авторизуйтесь перед загрузкой');
        const existing = await read('jobs', payload.postId);
        if (existing) {
            if (existing.status === 'cancelled') throw new Error('Загрузка отменена. Создайте новую публикацию.');
            // Queue entries are immutable: never mix the parts of different files.
            if (existing.owner !== owner(configuration.authToken) || existing.fileName !== payload.file.name
                || existing.fileSize !== payload.file.size || existing.lastModified !== payload.file.lastModified) {
                throw new Error('Для этого поста уже сохранён другой файл. Удалите пост в профиле и создайте новый.');
            }
            reply(snapshot(existing));
            return start(existing);
        }
        const job = { postId: payload.postId, duration: payload.duration, owner: owner(configuration.authToken),
            apiBaseUrl: configuration.apiBaseUrl, fileName: payload.file.name, fileSize: payload.file.size,
            lastModified: payload.file.lastModified, status: 'queued', progress: 0 };
        await save(job, payload.file);
        reply(snapshot(job));
        return start(job);
    }
    if (type === 'GET_UPLOAD') {
        const job = await read('jobs', payload.postId);
        reply(job?.owner === owner(configuration.authToken) ? snapshot(job) : null);
    }
    if (type === 'RETRY_UPLOAD') {
        const job = await read('jobs', payload.postId);
        if (!job || job.owner !== owner(configuration.authToken)) throw new Error('Загрузка не найдена');
        reply();
        return start(job);
    }
    if (type === 'CANCEL_UPLOAD') {
        cancelling.add(payload.postId);
        try {
            let job = await read('jobs', payload.postId);
            if (!job) { reply(); return; }
            if (job.owner !== owner(configuration.authToken)) throw new Error('Загрузка принадлежит другому аккаунту');
            const running = active.get(job.postId);
            if (running) {
                running.controller.abort();
                await running.promise;
                job = await read('jobs', job.postId);
            }
            const wasCompleted = job.status === 'completed';
            // Persist first so a reload cannot resume a post being deleted.
            await report(job, 'cancelled');
            if (job.uploadId && !wasCompleted) {
                const session = await api(job, `session/${encodeURIComponent(job.uploadId)}`);
                if (session?.status === 0) await api(job, 'abort', { uploadId: job.uploadId });
            }
            reply();
        } finally {
            cancelling.delete(payload.postId);
        }
    }
}

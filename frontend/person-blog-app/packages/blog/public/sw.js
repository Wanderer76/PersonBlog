
importScripts('https://cdn.jsdelivr.net/npm/idb@7/build/umd.js');
let isUploadingAllChunks = false;
let hasNewChunks = false;
let apiBaseUrl = null;
let authToken = null;


self.addEventListener('install', (event) => {
    console.log('[SW] Installed');
    self.skipWaiting();
});

self.addEventListener('activate', (event) => {
    console.log('[SW] Activated');
    self.clients.claim();
});

self.addEventListener('message', async (event) => {
    if (!event.data || typeof event.data.type !== 'string') return;

    const { type, payload } = event.data;

    if (type === 'CONFIGURE') {
        if (!payload || typeof payload.apiBaseUrl !== 'string') return;
        apiBaseUrl = payload.apiBaseUrl.replace(/\/$/, '');
        authToken = typeof payload.authToken === 'string' ? payload.authToken : null;
        return;
    }

    if (type === 'SET_AUTH_TOKEN') {
        authToken = typeof payload === 'string' ? payload : null;
        return;
    }

    if (type === 'UPLOAD_CHUNK') {
        console.log('[SW] Received chunk upload message', payload.chunkId);
        await uploadChunk(payload.chunkId);
    }

    if (type === 'UPLOAD_ALL_CHUNKS') {

        if (!apiBaseUrl) {
            console.error('[SW] Cannot upload chunks before API configuration');
            return;
        }

        if (isUploadingAllChunks) {
            console.log('[SW] Upload of all chunks already in progress, skipping duplicate call.');
            return;
        }
        isUploadingAllChunks = true;
        hasNewChunks = true;
        console.log('[SW] Triggered upload of all pending chunks');
        const db = await idb.openDB('video-upload-db', 1);

        while (hasNewChunks) {
            const allChunks = await db.getAll('chunks');
            if (allChunks.length > 0) {
                hasNewChunks = true;
            }
            else {
                hasNewChunks = false;
            }
            const grouped = allChunks.reduce((acc, chunk) => {
                const postId = chunk.meta.postId;
                if (!acc[postId]) {
                    acc[postId] = [];
                }
                acc[postId].push(chunk);

                return acc;
            }, {});

            for (const postId in grouped) {
                grouped[postId].sort((a, b) => a.meta.chunkNumber - b.meta.chunkNumber);
            }


            for (const postId in grouped) {
                for (const chunk of grouped[postId]) {
                    console.log(chunk.chunkNumber);
                    await uploadChunk(chunk.id);
                }
            }
        }
        isUploadingAllChunks = false;
    }
});

async function uploadChunk(chunkId) {
    if (!apiBaseUrl) {
        console.error('[SW] API URL is not configured');
        return;
    }

    const db = await idb.openDB('video-upload-db', 1);
    const chunkEntry = await db.get('chunks', chunkId);

    if (!chunkEntry) {
        console.log(`[SW] No chunk found in IDB for ${chunkId}`);
        return;
    }

    const { chunk, meta } = chunkEntry;
    const formData = new FormData();

    Object.entries(meta).forEach(([key, value]) => {
        formData.append(key, value);
    });

    formData.append("chunkData", chunk);

    try {
        const response = await fetch(`${apiBaseUrl}/profile/api/Post/uploadChunk`, {
            method: 'POST',
            body: formData,
            headers: authToken ? { Authorization: `Bearer ${authToken}` } : undefined,
        });

        if (response.ok) {
            console.log(`[SW] Chunk ${meta.chunkNumber} uploaded successfully`);
            await db.delete('chunks', chunkId);
            sendMessageToAllClients({
                type: 'CHUNK_UPLOADED',
                payload: meta
            });
        } else {

            console.error(`[SW] Upload failed for chunk ${meta.chunkNumber}`);
        }
    } catch (error) {
        console.error('[SW] Upload error', error);
    }
}

function sendMessageToAllClients(message) {
    self.clients.matchAll().then(clients => {
        clients.forEach(client => {
            client.postMessage(message);
        });
    });
}

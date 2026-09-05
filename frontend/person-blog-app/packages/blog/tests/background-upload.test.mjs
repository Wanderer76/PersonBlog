// Real Chromium service worker + IndexedDB, with an HTTP stub for the multipart API.
// Run: node --test tests/background-upload.test.mjs (requires playwright).
import { test, before, after, beforeEach, afterEach } from 'node:test';
import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { pathToFileURL } from 'node:url';

const { chromium } = await import(process.env.PLAYWRIGHT_MODULE
  ? pathToFileURL(process.env.PLAYWRIGHT_MODULE).href : 'playwright');
let browser, server, context, page, origin, state;
const script = await readFile(new URL('../public/sw.js', import.meta.url), 'utf8');

before(async () => {
  server = createServer(async (req, res) => {
    const url = new URL(req.url, origin);
    if (url.pathname === '/sw.js') {
      res.setHeader('Content-Type', 'application/javascript');
      res.end(script);
      return;
    }
    if (url.pathname === '/') { res.end('<!doctype html><title>Upload test</title>'); return; }
    const chunks = [];
    for await (const chunk of req) chunks.push(chunk);
    const body = Buffer.concat(chunks);
    const input = req.method === 'POST' ? JSON.parse(body.toString()) : null;
    res.setHeader('Content-Type', 'application/json');
    if (url.pathname.endsWith('/initiate')) {
      state.initiates++;
      res.end(JSON.stringify({ uploadId: 'session-1' }));
    } else if (url.pathname.includes('/session/')) {
      res.end(JSON.stringify({ status: state.completed ? 1 : state.aborted ? 2 : 0 }));
    } else if (url.pathname.includes('/parts/')) {
      res.end(JSON.stringify([...state.parts.values()]));
    } else if (url.pathname.endsWith('/generate-url')) {
      res.end(JSON.stringify({ url: `${origin}/storage/${input.partNumber}` }));
    } else if (url.pathname.startsWith('/storage/')) {
      const n = Number(url.pathname.split('/').pop());
      state.puts.push(n);
      state.concurrent++;
      state.maxConcurrent = Math.max(state.maxConcurrent, state.concurrent);
      if (state.holdParts) await state.holdParts;
      state.concurrent--;
      if (state.failParts.has(n)) { res.writeHead(503); res.end(); return; }
      state.parts.set(n, { partNumber: n, eTag: `etag-${n}`, size: body.length });
      res.setHeader('ETag', `"etag-${n}"`);
      res.end();
    } else if (url.pathname.endsWith('/complete')) {
      state.completeCalls++;
      state.completeParts = input.parts;
      if (state.holdComplete) await state.holdComplete;
      if (state.failComplete) { res.writeHead(503); res.end(); return; }
      state.completed = true;
      res.end(JSON.stringify('done'));
    } else if (url.pathname.endsWith('/abort')) {
      state.aborted = true;
      res.end();
    } else { res.writeHead(404); res.end(); }
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  origin = `http://127.0.0.1:${server.address().port}`;
  browser = await chromium.launch({ headless: true, channel: process.env.PLAYWRIGHT_CHANNEL || undefined });
});

after(async () => {
  await browser?.close();
  server?.closeAllConnections();
  await new Promise(resolve => server?.close(resolve));
});

beforeEach(async () => {
  state = { initiates: 0, parts: new Map(), puts: [], failParts: new Set(), concurrent: 0, maxConcurrent: 0, completeCalls: 0 };
  context = await browser.newContext();
  page = await context.newPage();
  await page.goto(origin);
  await page.evaluate(async () => {
    await navigator.serviceWorker.register('/sw.js');
    await navigator.serviceWorker.ready;
  });
  await configure();
});
afterEach(async () => { await context?.close(); });

function message(type, payload) {
  return page.evaluate(async ({ type, payload }) => {
    const worker = (await navigator.serviceWorker.ready).active;
    return new Promise(resolve => {
      const channel = new MessageChannel();
      channel.port1.onmessage = ({ data }) => { channel.port1.close(); resolve(data); };
      worker.postMessage({ type, payload }, [channel.port2]);
    });
  }, { type, payload });
}

function configure(userId = 'user-1') {
  return message('CONFIGURE', { apiBaseUrl: origin, resume: true, authToken: `x.${Buffer.from(JSON.stringify({ userId, blogId: 'blog-1' })).toString('base64url')}.x` });
}

function enqueue(size = 1024, name = 'video.mp4') {
  return page.evaluate(async ({ size, name }) => {
    const file = new File([new Uint8Array(size)], name, { type: 'video/mp4', lastModified: 1 });
    return new Promise(resolve => {
      const channel = new MessageChannel();
      channel.port1.onmessage = ({ data }) => { channel.port1.close(); resolve(data); };
      navigator.serviceWorker.ready.then(reg => reg.active.postMessage({
        type: 'ENQUEUE_UPLOAD', payload: { postId: 'post-1', file, duration: 10 }
      }, [channel.port2]));
    });
  }, { size, name });
}

async function waitForStatus(status) {
  let response;
  for (let i = 0; i < 200; i++) {
    response = await message('GET_UPLOAD', { postId: 'post-1' });
    if (response.value?.status === status) return response.value;
    await new Promise(resolve => setTimeout(resolve, 25));
  }
  assert.fail(`Expected ${status}, got ${JSON.stringify(response)}`);
}

test('acknowledges the durable queue before upload; never reports 100 before complete', async () => {
  let release;
  state.holdComplete = new Promise(resolve => { release = resolve; });
  assert.equal((await enqueue()).value.status, 'queued');
  assert.equal((await waitForStatus('completing')).progress, 99);
  assert.equal(state.completed, undefined);
  release();
  assert.equal((await waitForStatus('completed')).progress, 100);
  const file = await page.evaluate(() => new Promise(resolve => {
    const req = indexedDB.open('video-background-uploads', 1);
    req.onsuccess = () => {
      const get = req.result.transaction('files').objectStore('files').get('post-1');
      get.onsuccess = () => resolve(Boolean(get.result));
    };
  }));
  assert.equal(file, false, 'release the local file after successful completion');
});

test('failed complete stays below 100 and retry does not resend uploaded parts', async () => {
  state.failComplete = true;
  await enqueue();
  assert.equal((await waitForStatus('failed')).progress, 99);
  state.failComplete = false;
  await message('RETRY_UPLOAD', { postId: 'post-1' });
  await waitForStatus('completed');
  assert.deepEqual(state.puts, [1]);
  assert.equal(state.initiates, 1);
});

test('rejects a replacement file rather than mixing it with saved parts', async () => {
  state.failComplete = true;
  await enqueue();
  await waitForStatus('failed');
  const response = await enqueue(2048, 'replacement.mp4');
  assert.equal(response.ok, false);
  assert.match(response.error, /другой файл/);
  assert.equal(state.initiates, 1);
});

test('resumes from persisted session after worker termination and page reload', async () => {
  state.failComplete = true;
  await enqueue();
  await waitForStatus('failed');
  const cdp = await context.newCDPSession(page);
  await cdp.send('ServiceWorker.enable');
  await cdp.send('ServiceWorker.stopAllWorkers');
  state.failComplete = false;
  await page.reload();
  await configure();
  await waitForStatus('completed');
  assert.equal(state.initiates, 1);
  assert.deepEqual(state.puts, [1]);
});

test('settles failed batches and retries only missing parts with at most five concurrent PUTs', async () => {
  state.failParts.add(2);
  await enqueue(6 * 5 * 1024 * 1024);
  await waitForStatus('failed');
  const previousPuts = state.puts.length;
  state.failParts.clear();
  await message('RETRY_UPLOAD', { postId: 'post-1' });
  await waitForStatus('completed');
  assert.deepEqual(state.puts.slice(previousPuts).sort(), [2, 6]);
  assert.ok(state.maxConcurrent <= 5);
  assert.deepEqual(state.completeParts.map(p => p.partNumber), [1, 2, 3, 4, 5, 6]);
});

test('cancellation stops retries and cannot be restarted by configuration', async () => {
  state.failParts.add(1);
  await enqueue();
  const result = await message('CANCEL_UPLOAD', { postId: 'post-1' });
  assert.equal(result.ok, true);
  await waitForStatus('cancelled');
  const previousPuts = state.puts.length;
  await configure();
  await message('RETRY_UPLOAD', { postId: 'post-1' });
  await new Promise(resolve => setTimeout(resolve, 100));
  assert.equal(state.puts.length, previousPuts);
  assert.equal(state.completeCalls, 0);
});

test('another account cannot resume, inspect or cancel the queued file', async () => {
  state.failComplete = true;
  await enqueue();
  await waitForStatus('failed');
  await configure('user-2');
  assert.equal((await message('GET_UPLOAD', { postId: 'post-1' })).value, null);
  assert.equal((await message('RETRY_UPLOAD', { postId: 'post-1' })).ok, false);
  assert.equal((await message('CANCEL_UPLOAD', { postId: 'post-1' })).ok, false);
  assert.equal(state.completeCalls, 1);
});

// idbHelper.js
import { openDB } from 'idb';

export const dbPromise = openDB('video-upload-db', 1, {
  upgrade(db) {
    db.createObjectStore('chunks', { keyPath: 'id' });
  }
});

export async function saveChunk(id, chunk, meta) {
  const db = await dbPromise;
  await db.put('chunks', { id, chunk, meta });
}

export async function getAllChunks() {
  const db = await dbPromise;
  return await db.getAll('chunks');
}

export async function deleteChunk(id) {
  const db = await dbPromise;
  await db.delete('chunks', id);
}

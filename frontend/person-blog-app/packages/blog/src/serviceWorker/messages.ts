export interface UploadChunkMetadata {
  postId: string;
  chunkNumber: number;
  totalChunks: number;
  [key: string]: string | number | boolean | undefined;
}

export type ServiceWorkerRequest =
  | { type: 'CONFIGURE'; payload: { apiBaseUrl: string; authToken: string | null } }
  | { type: 'SET_AUTH_TOKEN'; payload: string | null }
  | { type: 'UPLOAD_CHUNK'; payload: { chunkId: string } }
  | { type: 'UPLOAD_ALL_CHUNKS' };

export type ServiceWorkerResponse =
  | { type: 'CHUNK_UPLOADED'; payload: UploadChunkMetadata };

export function postServiceWorkerMessage(message: ServiceWorkerRequest): void {
  navigator.serviceWorker?.controller?.postMessage(message);
}

export function isServiceWorkerResponse(value: unknown): value is ServiceWorkerResponse {
  if (typeof value !== 'object' || value === null) return false;

  const message = value as { type?: unknown; payload?: unknown };
  if (message.type !== 'CHUNK_UPLOADED' || typeof message.payload !== 'object' || message.payload === null) {
    return false;
  }

  const payload = message.payload as Partial<UploadChunkMetadata>;
  return typeof payload.postId === 'string'
    && typeof payload.chunkNumber === 'number'
    && typeof payload.totalChunks === 'number';
}

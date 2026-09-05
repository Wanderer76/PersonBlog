export type ServiceWorkerRequest =
  | { type: 'CONFIGURE'; payload: { apiBaseUrl: string; authToken: string | null; resume?: boolean } }
  | { type: 'SET_AUTH_TOKEN'; payload: string | null }
  | { type: 'ENQUEUE_UPLOAD'; payload: { postId: string; file: File; duration: number } }
  | { type: 'GET_UPLOAD' | 'RETRY_UPLOAD' | 'CANCEL_UPLOAD'; payload: { postId: string } };

export interface BackgroundUpload {
  postId: string;
  status: 'queued' | 'uploading' | 'completing' | 'completed' | 'failed' | 'cancelled';
  progress: number;
  error?: string;
}

export type ServiceWorkerResponse =
  | { type: 'UPLOAD_STATUS'; payload: BackgroundUpload }
  | { type: 'UPLOAD_AUTH_REQUIRED' };

export function postServiceWorkerMessage(message: ServiceWorkerRequest): void {
  navigator.serviceWorker?.controller?.postMessage(message);
}

export function isServiceWorkerResponse(value: unknown): value is ServiceWorkerResponse {
  if (typeof value !== 'object' || value === null) return false;

  const message = value as { type?: unknown; payload?: unknown };
  if (message.type === 'UPLOAD_AUTH_REQUIRED') return true;
  if (message.type === 'UPLOAD_STATUS' && typeof message.payload === 'object' && message.payload !== null) {
    const payload = message.payload as Partial<BackgroundUpload>;
    return typeof payload.postId === 'string' && typeof payload.progress === 'number'
      && ['queued', 'uploading', 'completing', 'completed', 'failed', 'cancelled'].includes(payload.status ?? '');
  }
  return false;
}

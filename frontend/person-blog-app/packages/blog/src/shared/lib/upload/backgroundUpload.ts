import { getAccessToken } from '@/shared/auth/tokenStorage';
import type { BackgroundUpload, ServiceWorkerRequest } from '@/shared/lib/service-worker/messages';

let registration: Promise<ServiceWorkerRegistration> | undefined;

export function registerUploadWorker(): Promise<ServiceWorkerRegistration> {
  if (!('serviceWorker' in navigator)) return Promise.reject(new Error('Браузер не поддерживает фоновую загрузку'));
  registration ??= navigator.serviceWorker.register('/sw.js').catch(error => {
    registration = undefined;
    throw error;
  });
  return registration;
}

async function getWorker(): Promise<ServiceWorker> {
  const reg = await registerUploadWorker();
  const worker = reg.installing || reg.waiting;
  if (!worker && reg.active) return reg.active;
  if (!worker) throw new Error('Не удалось запустить фоновую загрузку');
  await new Promise<void>((resolve, reject) => {
    const timeout = window.setTimeout(() => finish(new Error('Фоновая загрузка не запустилась. Повторите попытку')), 15000);
    const finish = (error?: Error) => {
      clearTimeout(timeout);
      worker.removeEventListener('statechange', check);
      if (error) reject(error); else resolve();
    };
    const check = () => {
      if (worker.state === 'activated') finish();
      if (worker.state === 'redundant') finish(new Error('Не удалось активировать фоновую загрузку'));
    };
    worker.addEventListener('statechange', check);
    check();
  });
  return worker;
}

function request<T>(worker: ServiceWorker, message: ServiceWorkerRequest): Promise<T> {
  return new Promise((resolve, reject) => {
    const channel = new MessageChannel();
    const timeout = window.setTimeout(() => {
      channel.port1.close();
      reject(new Error('Фоновая загрузка не ответила. Повторите попытку'));
    }, 120000);
    channel.port1.onmessage = ({ data }) => {
      clearTimeout(timeout);
      channel.port1.close();
      if (data.ok) resolve(data.value as T);
      else reject(new Error(data.error || 'Ошибка фоновой загрузки'));
    };
    try {
      worker.postMessage(message, [channel.port2]);
    } catch (error) {
      clearTimeout(timeout);
      channel.port1.close();
      channel.port2.close();
      reject(error);
    }
  });
}

export async function configureUploadWorker(resume = false): Promise<ServiceWorker> {
  const worker = await getWorker();
  await request(worker, { type: 'CONFIGURE', payload: {
    apiBaseUrl: import.meta.env.VITE_API_BASE_URL || window.location.origin,
    authToken: getAccessToken(),
    resume,
  } });
  return worker;
}

export async function enqueueVideo(postId: string, file: File, duration: number): Promise<BackgroundUpload> {
  const worker = await configureUploadWorker();
  return request(worker, { type: 'ENQUEUE_UPLOAD', payload: { postId, file, duration } });
}

export async function getBackgroundUpload(postId: string): Promise<BackgroundUpload | null> {
  return request(await configureUploadWorker(), { type: 'GET_UPLOAD', payload: { postId } });
}

export async function retryBackgroundUpload(postId: string): Promise<void> {
  return request(await configureUploadWorker(), { type: 'RETRY_UPLOAD', payload: { postId } });
}

export async function cancelBackgroundUpload(postId: string): Promise<void> {
  if (!('serviceWorker' in navigator)) return;
  return request(await configureUploadWorker(), { type: 'CANCEL_UPLOAD', payload: { postId } });
}

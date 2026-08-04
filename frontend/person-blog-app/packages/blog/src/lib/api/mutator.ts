// src/lib/api/mutator.ts
import axios, { AxiosResponse, type AxiosRequestConfig, type InternalAxiosRequestConfig } from 'axios';
import { JwtTokenService } from '../../shared/TokenStrorage.js';

declare global {
  interface ImportMetaEnv {
    readonly VITE_API_BASE_URL: string;
    readonly VITE_API_SWAGGER_URL: string;
  }
  interface ImportMeta {
    readonly env: ImportMetaEnv;
  }
}

// ✅ Создаём ОДИН instance на всё приложение
const instance = axios.create({
  baseURL: `${import.meta.env.VITE_API_BASE_URL}/video`,
  withCredentials: false,
  timeout: 30000,
});

// ✅ Очередь запросов, ожидающих refresh token
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value?: any) => void;
  reject: (reason?: any) => void;
}> = [];

const processQueue = (error: any | null, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

// ✅ Интерсептор request — добавляем токен
instance.interceptors.request.use((config) => {
  const token = JwtTokenService.getFormatedTokenForHeader();
  if (token) {
    config.headers.Authorization = token;
  }
  return config;
});

// ✅ Интерсептор response — обрабатываем 401
instance.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    if (error.response?.status === 403) {
      JwtTokenService.cleanAuth();
      void JwtTokenService.redirectToAuth(window.location.href).catch(() => undefined);
      return Promise.reject(error);
    }

    // Если не 401 или уже пытались retry — отклоняем
    if (error.response?.status !== 401 || originalRequest._retry) {
      return Promise.reject(error);
    }

    // Если уже идёт refresh — добавляем запрос в очередь
    if (isRefreshing) {
      return new Promise((resolve, reject) => {
        failedQueue.push({ resolve, reject });
      })
        .then((token) => {
          originalRequest.headers.Authorization = token;
          return instance(originalRequest);
        })
        .catch((err) => Promise.reject(err));
    }

    originalRequest._retry = true;
    isRefreshing = true;

    try {
      const status = await JwtTokenService.refreshToken();
      
      if (status !== 200) {
        throw new Error('Refresh token failed');
      }

      const newToken = JwtTokenService.getFormatedTokenForHeader();
      
      // ✅ Обрабатываем очередь ожидающих запросов
      processQueue(null, newToken);
      
      // ✅ Повторяем оригинальный запрос с новым токеном
      originalRequest.headers.Authorization = newToken;
      return instance(originalRequest);
      
    } catch (refreshError) {
      // ✅ Refresh не удался — очищаем очередь и перенаправляем на авторизацию
      processQueue(refreshError, null);
      JwtTokenService.cleanAuth();
      void JwtTokenService.redirectToAuth(window.location.href).catch(() => undefined);
      return Promise.reject(refreshError);
      
    } finally {
      isRefreshing = false;
    }
  }
);

// ✅ Функция-обёртка для Orval
export const customInstance = async <T>(config: AxiosRequestConfig): Promise<AxiosResponse<T>> => {
  return instance.request<T>(config);
};

export default customInstance;

// src/lib/api/mutator.ts
import axios, { AxiosResponse, type AxiosRequestConfig } from 'axios';
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

let refreshTokenPromise: Promise<void> | null = null;

// Функция с дженерик типом для Orval
export const customInstance = async <T>(config: AxiosRequestConfig): Promise<AxiosResponse<T>> => {
  const instance = axios.create({
    baseURL:   `${import.meta.env.VITE_API_BASE_URL}/video`,
    withCredentials: false,
  });

  instance.interceptors.request.use((config) => {
    config.headers.Authorization = JwtTokenService.getFormatedTokenForHeader();
    return config;
  });

  instance.interceptors.response.use(
    (response) => response,
    async (error) => {
      const originalRequest = error.config;
      if (error.response?.status === 401 && !originalRequest._retry) {
        originalRequest._retry = true;

        if (!refreshTokenPromise) {
          refreshTokenPromise = JwtTokenService.refreshToken()
            .then((status) => {
              if (status !== 200) {
                throw new Error('Refresh token failed');
              }
              refreshTokenPromise = null;
            })
            .catch((refreshError) => {
              refreshTokenPromise = null;
              JwtTokenService.cleanAuth();
              return Promise.reject(refreshError);
            });
        }

        await refreshTokenPromise;

        return instance(originalRequest);
      }

      return Promise.reject(error);
    }
  );

  const res = await instance.request(config);
  return res;
};

function redirectToAuth() {
  const authPaths = ['/auth'];
  if (!authPaths.includes(window.location.pathname)) {
    window.location.href = '/auth';
  }
}

export default customInstance;
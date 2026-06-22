// src/lib/api/mutator.ts
import axios, { type AxiosResponse, type AxiosRequestConfig } from 'axios';

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
    baseURL:   `${import.meta.env.VITE_API_BASE_URL}/`,
    withCredentials: false,
  });

  instance.interceptors.request.use((config) => {
    return config;
  });

  instance.interceptors.response.use(
    (response) => response,
    async (error) => {
      const originalRequest = error.config;
      if (error.response?.status === 401 && !originalRequest._retry) {
        originalRequest._retry = true;

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
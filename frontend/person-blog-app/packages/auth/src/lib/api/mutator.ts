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

// Функция с дженерик типом для Orval
export const customInstance = async <T>(config: AxiosRequestConfig): Promise<AxiosResponse<T>> => {
  const instance = axios.create({
    baseURL:   `${import.meta.env.VITE_API_BASE_URL}/`,
    withCredentials: false,
  });

  instance.interceptors.request.use((config) => {
    return config;
  });

  const res = await instance.request(config);
  return res;
};

export default customInstance;

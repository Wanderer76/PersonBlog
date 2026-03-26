// src/lib/api/client.ts
import axios from 'axios';

export const BaseApUrl = import.meta.env.VITE_API_BASE_URL;

export const API = axios.create({
  baseURL: BaseApUrl,
  withCredentials: false,
});

let refreshTokenPromise: Promise<void> | null = null;

API.interceptors.request.use(config => {
  return config;
});

API.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      await refreshTokenPromise;
      return API(originalRequest);
    }

    return Promise.reject(error);
  }
);

function redirectToAuth() {
  const authPaths = ['/auth'];
  if (!authPaths.includes(window.location.pathname)) {
    window.location.href = '/auth';
  }
}

// Экспортируем для orval
export default API;
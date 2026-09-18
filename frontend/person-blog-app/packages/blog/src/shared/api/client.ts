// src/lib/api/client.ts
import axios from 'axios';
import { beginForbiddenReauth, completeForbiddenReauth, JwtTokenService } from '@/shared/auth/tokenStorage';

export const BaseApUrl = import.meta.env.VITE_API_BASE_URL;

export const API = axios.create({
  baseURL: BaseApUrl,
  withCredentials: false,
});

let refreshTokenPromise: Promise<void> | null = null;

API.interceptors.request.use(config => {
  config.headers.Authorization = JwtTokenService.getFormatedTokenForHeader();
  return config;
});

API.interceptors.response.use(
  (response) => {
    completeForbiddenReauth(response.config.url);
    return response;
  },
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 403) {
      if (beginForbiddenReauth(originalRequest.url)) {
        JwtTokenService.cleanAuth();
        void JwtTokenService.redirectToAuth(window.location.href).catch(() => undefined);
      }
      return Promise.reject(error);
    }

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      if (!refreshTokenPromise) {
        refreshTokenPromise = JwtTokenService.refreshToken()
          .then((status) => {
            if (status !== 200) {
              void JwtTokenService.redirectToAuth(window.location.href).catch(() => undefined);
              throw new Error('Refresh token failed');
            }
          })
          .finally(() => {
            refreshTokenPromise = null;
          });
      }
      await refreshTokenPromise;
      return API(originalRequest);
    }

    return Promise.reject(error);
  }
);

// Экспортируем для orval
export default API;

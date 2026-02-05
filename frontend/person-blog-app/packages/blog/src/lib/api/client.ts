// src/lib/api/client.ts
import axios from 'axios';
import { JwtTokenService } from '../../scripts/TokenStrorage';

export const BaseApiUrl = 'http://localhost:7892';

export const API = axios.create({
  baseURL: BaseApiUrl,
  withCredentials: false,
});

let refreshTokenPromise: Promise<void> | null = null;

API.interceptors.request.use(config => {
  config.headers.Authorization = JwtTokenService.getFormatedTokenForHeader();
  return config;
});

API.interceptors.response.use(
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
            redirectToAuth();
            return Promise.reject(refreshError);
          });
      }

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
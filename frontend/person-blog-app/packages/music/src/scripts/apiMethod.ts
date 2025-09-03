import axios, { type AxiosInstance } from 'axios';
import { JwtTokenService } from './TokenStrorage';

export const BaseApUrl: string = 'http://localhost:7892/music/api';
export const AuthUrl: string = 'http://localhost:7892';
export const AuthPageUrl: string = 'http://localhost:3000/auth';
export const BlogPageUrl: string = 'http://localhost:3000';

const API: AxiosInstance = axios.create({
    baseURL: BaseApUrl, // Ваш базовый URL
    withCredentials: false,
});

let refreshTokenPromise: Promise<void | Number> | null = null;

API.interceptors.request.use(config => {
    config.headers.Authorization = JwtTokenService.getFormatedTokenForHeader();
    return config;
});

API.interceptors.response.use(
    (response) => response,
    async (error) => {
        const originalRequest = error.config;
        if (error.response && error.response.status === 401 && !originalRequest._retry) {
            originalRequest._retry = true;

            // Проверяем, есть ли уже активный процесс обновления токена
            if (!refreshTokenPromise) {
                refreshTokenPromise = JwtTokenService.refreshToken().then((status: any) => {
                    if (status !== 200) {
                        throw new Error('Refresh token failed');
                    }
                    // После успешного обновления токена, сбрасываем promise
                    refreshTokenPromise = null;
                }).catch((refreshError: any) => {
                    // Обработка ошибки обновления токена
                    refreshTokenPromise = null;
                    JwtTokenService.cleanAuth();
                    redirectToAuth();
                    return Promise.reject(refreshError);
                });
            }

            // Ждем завершения обновления токена
            await refreshTokenPromise;

            // Повторяем оригинальный запрос
            return API(originalRequest);
        }

        return Promise.reject(error);
    }
);

// Функция перенаправления с защитой от циклов
function redirectToAuth() {
    const authPaths = ['/auth']; // Добавьте все пути авторизации
    if (!authPaths.includes(window.location.pathname)) {
        window.location.href = '/auth';
    }
}

export default API;

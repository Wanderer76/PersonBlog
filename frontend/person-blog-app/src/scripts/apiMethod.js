import axios from 'axios';
import { getSession, JwtTokenService } from './TokenStrorage';

export const BaseApUrl = 'http://localhost:7892';
const API = axios.create({
    baseURL: BaseApUrl, // Ваш базовый URL
    withCredentials: true,
});

let refreshTokenPromise = null;

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
                refreshTokenPromise = JwtTokenService.refreshToken().then((status) => {
                    if (status !== 200) {
                        throw new Error('Refresh token failed');
                    }
                    // После успешного обновления токена, сбрасываем promise
                    refreshTokenPromise = null;
                }).catch((refreshError) => {
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

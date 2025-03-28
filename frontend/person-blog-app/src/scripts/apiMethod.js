import axios from 'axios';
import { JwtTokenService } from './TokenStrorage';

export const BaseApUrl = 'http://localhost:7892'
const API = axios.create({
    baseURL: BaseApUrl, // Ваш базовый URL
    withCredentials: true,
});

const refreshState = {
    isRefreshing: false,
    retryCount: 0,
    maxRetries: 1
};

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

            if (refreshState.retryCount >= refreshState.maxRetries) {
                JwtTokenService.cleanAuth();
                redirectToAuth();
                return Promise.reject(error);
            }

            if (!refreshState.isRefreshing) {
                refreshState.isRefreshing = true;
                refreshState.retryCount += 1;

                try {
                    const status = await JwtTokenService.refreshToken();

                    if (status !== 200) {
                        throw new Error('Refresh token failed');
                    }

                    // Сброс счетчика при успешном обновлении
                    refreshState.retryCount = 0;
                    return API(originalRequest);
                } catch (refreshError) {
                    // Обработка превышения попыток
                    if (refreshState.retryCount >= refreshState.maxRetries) {
                        JwtTokenService.cleanAuth();
                        redirectToAuth();
                    }
                    return Promise.reject(refreshError);
                } finally {
                    refreshState.isRefreshing = false;
                }
            }
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
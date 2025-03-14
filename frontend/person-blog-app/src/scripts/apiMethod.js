import axios from 'axios';
import { JwtTokenService } from './TokenStrorage';

export const BaseApUrl = 'http://localhost:7892'
let isRefreshing = false;
const API = axios.create({
    baseURL: BaseApUrl, // Ваш базовый URL
    withCredentials: true,
});

API.interceptors.request.use(config => {
    config.headers.Authorization = JwtTokenService.getFormatedTokenForHeader();
    return config;
});

API.interceptors.response.use(
    (response) => response,
    async (error) => {
        const originalRequest = error.config;
        if (error.response && error.response.status === 401) {
            originalRequest._retry = true;
            if (!isRefreshing) {
                isRefreshing = true;
                // Обработка ошибки 401
                console.error('Ошибка авторизации (401):', error);
                var status = await JwtTokenService.refreshToken()
                if (status !== 200) {
                    JwtTokenService.cleanAuth();
                    //const navigate = useNavigate();
                    // Проверяем, чтобы не было бесконечной петли редиректов
                    // if (window.location.pathname !== '/auth') {
                    //     window.location.href = '/auth'
                    // }
                }
                return API(originalRequest); 
            }
            return Promise.reject(error); // Передать ошибку дальше
        } else if (error.response && error.response.status === 400) { // Пример обработки другой ошибки
            // Обработка ошибки 400
            console.error('Ошибка 400:', error);
            // Обработка ошибки, например, показать модальное окно пользователю
            return Promise.reject(error);
        }
        return Promise.reject(error); // Передать остальные ошибки дальше
    }
);

export default API;
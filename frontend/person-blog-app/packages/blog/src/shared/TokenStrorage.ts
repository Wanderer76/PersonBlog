import API, { BaseApUrl } from "../lib/api/client";

export const ACCESS_TOKEN_KEY = 'ACCESS_TOKEN_KEY';
export const REFRESH_TOKEN_KEY = 'REFRESH_TOKEN_KEY';

/**
 * Сохраняет access token в localStorage и уведомляет Service Worker
 */
export function saveAccessToken(token: string | null): void {
    if (token === null) {
        localStorage.removeItem(ACCESS_TOKEN_KEY);
        return;
    }
    
    localStorage.setItem(ACCESS_TOKEN_KEY, token);
    
    if (navigator.serviceWorker?.controller) {
        navigator.serviceWorker.controller.postMessage({
            type: 'SET_AUTH_TOKEN',
            payload: token
        });
    }
}

/**
 * Сохраняет refresh token в localStorage
 */
export function saveRefreshToken(token: string | null): void {
    if (token === null) {
        localStorage.removeItem(REFRESH_TOKEN_KEY);
        return;
    }
    localStorage.setItem(REFRESH_TOKEN_KEY, token);
}

/**
 * Получает access token из localStorage
 */
export function getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
}

/**
 * Получает refresh token из localStorage
 */
export function getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
}

/**
 * Ответ от эндпоинта refresh token
 */
interface RefreshTokenResponse {
    accessToken: string;
    refreshToken: string;
}

/**
 * Сообщение для Service Worker
 */
interface ServiceWorkerAuthMessage {
    type: 'SET_AUTH_TOKEN';
    payload: string;
}

export class JwtTokenService {
    /**
     * Возвращает токен в формате для HTTP-заголовка Authorization
     */
    static getFormatedTokenForHeader(): string | null {
        const token = getAccessToken();
        return token ? `Bearer ${token}` : null;
    }

    /**
     * Обновляет пару токенов через API
     * @returns HTTP status code или 401 при ошибке
     */
    static async refreshToken(): Promise<number> {
        try {
            const refreshToken = getRefreshToken();
            
            if (!refreshToken) {
                this.cleanAuth();
                return 401;
            }

            const response = await fetch(
                `${BaseApUrl}/video/api/Auth/refresh?refreshToken=${encodeURIComponent(refreshToken)}`,
                {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    }
                }
            );

            if (response.ok) {
                const data: RefreshTokenResponse = await response.json();
                saveAccessToken(data.accessToken);
                saveRefreshToken(data.refreshToken);
                return response.status;
            }
            
            if (response.status === 401) {
                this.cleanAuth();
            }
            
            return response.status;
        } catch {
            this.cleanAuth();
            return 401;
        }
    }

    /**
     * Удаляет токены из localStorage
     */
    static cleanAuth(): void {
        localStorage.removeItem(ACCESS_TOKEN_KEY);
        localStorage.removeItem(REFRESH_TOKEN_KEY);
    }

    /**
     * Проверяет, авторизован ли пользователь
     */
    static isAuth(): boolean {
        return getAccessToken() !== null;
    }
}
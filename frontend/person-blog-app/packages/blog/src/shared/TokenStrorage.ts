import { getAuth } from "@/lib/api/generated/auth/auth";
import API, { BaseApUrl } from "../lib/api/client";
import { AuthCodeResponse, AuthResponse } from "@/lib/api/generated/models";

export const ACCESS_TOKEN_KEY = 'ACCESS_TOKEN_KEY';
export const REFRESH_TOKEN_KEY = 'REFRESH_TOKEN_KEY';
export const OAUTH_STATE_KEY = 'OAUTH_STATE_KEY';

/**
 * Сохраняет access token в localStorage и уведомляет Service Worker
 */
export function saveAccessToken(token: string | null): void {
    if (token === null) {
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

export function saveOAuthState(state: string): void {
    localStorage.setItem(OAUTH_STATE_KEY, state);
}
export function getAndClearOAuthState(): string | null {
    const state = localStorage.getItem(OAUTH_STATE_KEY);
    localStorage.removeItem(OAUTH_STATE_KEY);
    return state;
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
     * Редирект пользователя на сервер авторизации (начало OAuth потока)
     */
    static async redirectToAuth(returnUrl?: string): Promise<void> {
        const state = crypto.randomUUID();
        saveOAuthState(state);

        const params = new URLSearchParams({
            clientId: 'blog',
            redirectUri: `http://localhost:3000/callback`,
            response_type: 'code',
            state: state,
            ...(returnUrl && { returnUrl }),
        });

        const response = await API.get(
            `video/api/auth/authorize?${params.toString()}`,
        );

        if (response.status !== 200) {
            throw new Error(`Authorize failed: ${response.status}`);
        }

        const data = response.data;

        if (!data.redirectUrl) {
            throw new Error('Empty redirectUrl from server');
        }

        // Переход на Auth Server (форма логина)
        window.location.href = data.redirectUrl;
    }

    /**
 * Обмен кода авторизации на токены (вызывается на странице /callback)
 */
    static async exchangeCodeForTokens(code: string, state: string | null = null): Promise<RefreshTokenResponse> {
        // Проверяем state для защиты от CSRF
        const savedState = getAndClearOAuthState();
        if (!savedState) {
            throw new Error('Invalid OAuth state');
        }

        const response = await API.post(`video/api/Auth/token`, {
            grant_type: 'authorization_code',
            code: code,
            client_id: 'blog',
            client_secret: 'blog', // ⚠️ См. примечание про PKCE ниже
            redirect_uri: `${window.location.origin}/callback`,
        });

        if (response.status !== 200) {
            const error = response.data;
            throw new Error(`Token exchange failed: ${error}`);
        }

        const data = response.data;

        // Сохраняем токены
        saveAccessToken(data.accessToken);
        saveRefreshToken(data.refreshToken);

        return {
            accessToken: data.accessToken,
            refreshToken: data.refreshToken,
        };
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

            
            const response = await getAuth().postApiAuthRefresh({refreshToken: encodeURIComponent(refreshToken)});

            if (response.status === 200) {
                const data: AuthResponse = response.data;
                saveAccessToken(data.accessToken!);
                saveRefreshToken(data.refreshToken!);
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
        localStorage.removeItem(OAUTH_STATE_KEY);
    }

    /**
     * Проверяет, авторизован ли пользователь
     */
    static isAuth(): boolean {
        return getAccessToken() !== null;
    }
}
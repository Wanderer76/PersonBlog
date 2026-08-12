import axios from "axios";
import { AuthResponse } from "@/lib/api/generated/models";

export const ACCESS_TOKEN_KEY = 'ACCESS_TOKEN_KEY';
export const REFRESH_TOKEN_KEY = 'REFRESH_TOKEN_KEY';
export const OAUTH_STATE_KEY = 'OAUTH_STATE_KEY';
export const OAUTH_RETURN_URL_KEY = 'OAUTH_RETURN_URL_KEY';
export const AUTH_STATE_CHANGED_EVENT = 'auth-state-changed';
const AUTH_API_URL = import.meta.env.VITE_AUTH_API_URL || 'http://localhost:5078';
const authTransport = axios.create({ baseURL: AUTH_API_URL, withCredentials: false });

function notifyAuthStateChanged(): void {
    window.dispatchEvent(new Event(AUTH_STATE_CHANGED_EVENT));
}

export function subscribeToAuthState(listener: () => void): () => void {
    const handleStorage = (event: StorageEvent) => {
        if (event.key === ACCESS_TOKEN_KEY || event.key === REFRESH_TOKEN_KEY) {
            listener();
        }
    };

    window.addEventListener(AUTH_STATE_CHANGED_EVENT, listener);
    window.addEventListener('storage', handleStorage);

    return () => {
        window.removeEventListener(AUTH_STATE_CHANGED_EVENT, listener);
        window.removeEventListener('storage', handleStorage);
    };
}

/**
 * Сохраняет access token в localStorage и уведомляет Service Worker
 */
export function saveAccessToken(token: string | null): void {
    if (token === null) {
        return;
    }

    localStorage.setItem(ACCESS_TOKEN_KEY, token);
    notifyAuthStateChanged();

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
    notifyAuthStateChanged();
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
    sessionStorage.setItem(OAUTH_STATE_KEY, state);
}
export function getAndClearOAuthState(): string | null {
    const state = sessionStorage.getItem(OAUTH_STATE_KEY);
    sessionStorage.removeItem(OAUTH_STATE_KEY);
    return state;
}

function saveOAuthReturnUrl(returnUrl: string): void {
    sessionStorage.setItem(OAUTH_RETURN_URL_KEY, returnUrl);
}

export function getAndClearOAuthReturnUrl(): string {
    const returnUrl = sessionStorage.getItem(OAUTH_RETURN_URL_KEY);
    sessionStorage.removeItem(OAUTH_RETURN_URL_KEY);
    if (!returnUrl) return '/';

    try {
        const parsed = new URL(returnUrl, window.location.origin);
        return parsed.origin === window.location.origin
            ? `${parsed.pathname}${parsed.search}${parsed.hash}`
            : '/';
    } catch {
        return '/';
    }
}

/**
 * Ответ от эндпоинта refresh token
 */
interface RefreshTokenResponse {
    accessToken: string;
    refreshToken: string;
}

export class JwtTokenService {
    private static refreshPromise: Promise<number> | null = null;
    private static authRedirectPromise: Promise<void> | null = null;
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
        if (this.authRedirectPromise) return this.authRedirectPromise;

        this.authRedirectPromise = this.performAuthRedirect(returnUrl);
        try {
            await this.authRedirectPromise;
        } catch (error) {
            this.authRedirectPromise = null;
            throw error;
        }
    }

    private static async performAuthRedirect(returnUrl?: string): Promise<void> {
        const state = crypto.randomUUID();
        saveOAuthState(state);
        saveOAuthReturnUrl(returnUrl || window.location.href);

        const params = new URLSearchParams({
            clientId: 'blog',
            redirectUri: `http://localhost:3000/callback`,
            response_type: 'code',
            state: state,
            ...(returnUrl && { returnUrl }),
        });

        const response = await authTransport.get(`/api/Auth/authorize?${params.toString()}`);

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
        if (!savedState || !state || savedState !== state) {
            throw new Error('Invalid OAuth state');
        }

        const response = await authTransport.post(`/api/Auth/token`, {
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
        if (this.refreshPromise) return this.refreshPromise;

        this.refreshPromise = this.performRefresh();
        try {
            return await this.refreshPromise;
        } finally {
            this.refreshPromise = null;
        }
    }

    private static async performRefresh(): Promise<number> {
        try {
            const refreshToken = getRefreshToken();

            if (!refreshToken) {
                this.cleanAuth();
                return 401;
            }

            
            // Отдельный transport не содержит 401 interceptor, поэтому refresh не может вызвать сам себя.
            const response = await authTransport.post(`/api/Auth/refresh`, null, {
                params: { refreshToken },
            });

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
        sessionStorage.removeItem(OAUTH_STATE_KEY);
        sessionStorage.removeItem(OAUTH_RETURN_URL_KEY);
        notifyAuthStateChanged();
    }

    /**
     * Проверяет, авторизован ли пользователь
     */
    static isAuth(): boolean {
        // Истёкший access token всё ещё допускается до HTTP-слоя: interceptor
        // обменяет валидный refresh token и повторит исходный запрос.
        return getAccessToken() !== null && getRefreshToken() !== null;
    }
}

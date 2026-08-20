import axios from "axios";
import { getAuth } from '@/shared/api/generated/auth-gateway/auth/auth';
import type { AuthResponse } from '@/shared/api/generated/auth-gateway/models';
import { postServiceWorkerMessage } from '@/shared/lib/service-worker/messages';

export const ACCESS_TOKEN_KEY = 'ACCESS_TOKEN_KEY';
export const REFRESH_TOKEN_KEY = 'REFRESH_TOKEN_KEY';
export const OAUTH_STATE_KEY = 'OAUTH_STATE_KEY';
export const OAUTH_RETURN_URL_KEY = 'OAUTH_RETURN_URL_KEY';
export const AUTH_STATE_CHANGED_EVENT = 'auth-state-changed';
const AUTH_REDIRECT_URI = `${window.location.origin}/callback`;
const authGatewayClient = getAuth();

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

    postServiceWorkerMessage({ type: 'SET_AUTH_TOKEN', payload: token });
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
type TokenPair = {
    accessToken: NonNullable<AuthResponse['accessToken']>;
    refreshToken: NonNullable<AuthResponse['refreshToken']>;
};

function isTokenResponse(value: AuthResponse): value is TokenPair {
    if (typeof value !== 'object' || value === null) return false;

    const response = value as Record<string, unknown>;
    return typeof response.accessToken === 'string'
        && response.accessToken.length > 0
        && typeof response.refreshToken === 'string'
        && response.refreshToken.length > 0;
}

function saveTokenPair(tokens: TokenPair): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, tokens.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);
    notifyAuthStateChanged();

    postServiceWorkerMessage({ type: 'SET_AUTH_TOKEN', payload: tokens.accessToken });
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

        const response = await authGatewayClient.getApiAuthAuthorize({
            clientId: 'blog',
            redirectUri: AUTH_REDIRECT_URI,
            response_type: 'code',
            state,
            returnUrl,
        });

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
    static async exchangeCodeForTokens(code: string, state: string | null = null): Promise<TokenPair> {
        // Проверяем state для защиты от CSRF
        const savedState = getAndClearOAuthState();
        if (!savedState || !state || savedState !== state) {
            throw new Error('Invalid OAuth state');
        }

        const response = await authGatewayClient.postApiAuthToken({
            grant_type: 'authorization_code',
            code: code,
            client_id: 'blog',
            client_secret: 'blog', // ⚠️ См. примечание про PKCE ниже
            redirect_uri: AUTH_REDIRECT_URI,
        });

        const data = response.data;

        if (!isTokenResponse(data)) {
            throw new Error('Invalid token response');
        }

        // Сохраняем токены
        saveTokenPair(data);

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
            const response = await authGatewayClient.postApiAuthRefresh({ refreshToken });

            if (response.status === 200) {
                const data: AuthResponse = response.data;
                if (!isTokenResponse(data)) {
                    throw new Error('Invalid refresh response');
                }
                saveTokenPair(data);
                return response.status;
            }

            return response.status;
        } catch (error) {
            if (axios.isAxiosError(error)) {
                const status = error.response?.status;
                if (status === 400 || status === 401 || status === 403) {
                    this.cleanAuth();
                    return status;
                }
            }

            throw error;
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
        postServiceWorkerMessage({ type: 'SET_AUTH_TOKEN', payload: null });
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

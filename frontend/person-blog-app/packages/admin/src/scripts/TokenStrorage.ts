import API, { BaseApUrl } from "./apiMethod";


export const ACCESS_TOKEN_KEY = 'ACCESS_TOKEN_KEY';
export const REFRESH_TOKEN_KEY = 'REFRESH_TOKEN_KEY';

export function saveAccessToken(token: string) {
    localStorage.setItem(ACCESS_TOKEN_KEY, token);
}

export function saveRefreshToken(token: string) {
    localStorage.setItem(REFRESH_TOKEN_KEY, token);
}
export function getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
}

export function getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
}

export class JwtTokenService {
    static getFormatedTokenForHeader(): string | null {
        let token = getAccessToken();
        return token === null ? null : "Bearer " + token;
    }

    static async refreshToken(): Promise<Number> {
        try {
            var response = await fetch(`${BaseApUrl}/video/api/Auth/refresh?refreshToken=${getRefreshToken()}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                }
            })
            if (response.status === 200) {
                const data = await response.json();
                saveAccessToken(data.accessToken);
                saveRefreshToken(data.refreshToken);
            }
            else if (response.status === 401) {
                this.cleanAuth();
            }
            return response.status;
        } catch (e) {
            return 401;
        }
    }

    static cleanAuth() {
        localStorage.removeItem(ACCESS_TOKEN_KEY);
        localStorage.removeItem(REFRESH_TOKEN_KEY);
    }

    static isAuth(): boolean {
        return getAccessToken() !== null;
    }
}
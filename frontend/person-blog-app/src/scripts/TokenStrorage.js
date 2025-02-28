import { BaseApUrl } from "./apiMethod";


export const ACCESS_TOKEN_KEY = 'ACCESS_TOKEN_KEY';
export const REFRESH_TOKEN_KEY = 'REFRESH_TOKEN_KEY';

export function saveAccessToken(token) {
    sessionStorage.setItem(ACCESS_TOKEN_KEY, token);
}

export function saveRefreshToken(token) {
    sessionStorage.setItem(REFRESH_TOKEN_KEY, token);
}
export function getAccessToken() {
    return sessionStorage.getItem(ACCESS_TOKEN_KEY);
}

export function getRefreshToken() {
    return sessionStorage.getItem(REFRESH_TOKEN_KEY);
}

export class JwtTokenService {
    static getFormatedTokenForHeader() {
        let token = getAccessToken();
        return token === null ? null : "Bearer " + token;
    }

    static async refreshToken() {
        const url = `${BaseApUrl}/auth/api/Auth/refresh?refreshToken=` + getRefreshToken();
        console.log(url)

        if (this.isAuth()) {
            var response = await fetch(url, {
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
            if (response.status === 401) {
                this.cleanAuth();
            }
            return response.status;
        }
        return 401;
    }

    static cleanAuth() {
        sessionStorage.removeItem(ACCESS_TOKEN_KEY);
        sessionStorage.removeItem(REFRESH_TOKEN_KEY);
    }

    static isAuth() {
        return getAccessToken() !== null;
    }
}
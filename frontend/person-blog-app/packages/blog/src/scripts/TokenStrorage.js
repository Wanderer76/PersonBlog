import API, { BaseApUrl } from "./apiMethod";


export const ACCESS_TOKEN_KEY = 'ACCESS_TOKEN_KEY';
export const REFRESH_TOKEN_KEY = 'REFRESH_TOKEN_KEY';

export function saveAccessToken(token) {
    
    sessionStorage.setItem(ACCESS_TOKEN_KEY, token);
console.log('store seestion tokjen')
    if (navigator.serviceWorker?.controller && token) {
        navigator.serviceWorker.controller.postMessage({
        type: 'SET_AUTH_TOKEN',
        payload: token
    });
}
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
        sessionStorage.removeItem(ACCESS_TOKEN_KEY);
        sessionStorage.removeItem(REFRESH_TOKEN_KEY);
    }

    static isAuth() {
        return getAccessToken() !== null;
    }
}
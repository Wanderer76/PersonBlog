

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
        return "Bearer " + getAccessToken();
    }

    static async refreshToken() {
        const url = 'http://localhost:7892/auth/api/Auth/refresh?refreshToken=' + getRefreshToken();
        console.log(url)
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
    }
}
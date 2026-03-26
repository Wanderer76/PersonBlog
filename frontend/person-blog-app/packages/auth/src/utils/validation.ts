

export const validateRedirectUri = (
    uri: string | null,
    allowedDomains: string[] = []
): string => {
    if (!uri) return '/';

    // Разрешаем относительные пути
    if (uri.startsWith('/') && !uri.startsWith('//')) {
        return uri;
    }

    // Проверяем абсолютные URL
    try {
        const parsed = new URL(uri);

        // Проверяем домен из whitelist
        if (allowedDomains.length > 0) {
            if (allowedDomains.includes(parsed.origin)) {
                return parsed.pathname + parsed.search;
            }
        }
        // Или проверяем текущий origin (для локальной разработки)
        else if (parsed.origin === window.location.origin) {
            return parsed.pathname + parsed.search;
        }
    } catch {
        // Неверный URL
    }

    return '/';
};

/**
 * Построение URL для редиректа с OAuth-параметрами
 */
export const buildAuthRedirectUrl = (
    baseUrl: string,
    authCode: string,
    state?: string | null
): string => {
    const url = new URL(baseUrl, window.location.origin);
    url.searchParams.set('code', authCode);

    if (state) {
        url.searchParams.set('state', state);
    }

    return url.toString();
};
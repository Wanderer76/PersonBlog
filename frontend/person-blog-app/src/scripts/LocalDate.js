

export function getLocalDateTime(datetime) {
    const date = new Date(datetime);
    return `${date.toLocaleDateString('ru')} ${date.toLocaleTimeString()}`;
}
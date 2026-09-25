/**
 * Форматирует дату и время в локальном формате RU
 */
export function getLocalDateTime(datetime: string | number | Date): string {
    const date = new Date(datetime);
    
    if (isNaN(date.getTime())) {
        throw new Error('Invalid date input');
    }
    
    return `${date.toLocaleDateString('ru-RU')} ${date.toLocaleTimeString('ru-RU')}`;
}

/**
 * Форматирует дату в локальном формате RU
 */
export function getLocalDate(datetime: string | number | Date): string {
    const date = new Date(datetime);
    
    if (isNaN(date.getTime())) {
        throw new Error('Invalid date input');
    }
    
    return date.toLocaleDateString('ru-RU');
}

/**
 * Конвертирует секунды в человекочитаемый формат (ч, мин, сек)
 */
export function secondsToHumanReadable(seconds: number): string {
    if (typeof seconds !== 'number' || seconds < 0 || !Number.isFinite(seconds)) {
        return "Некорректный ввод";
    }
    
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const remainingSeconds = Math.floor(seconds % 60);
    
    let result = "";
    
    if (hours > 0) {
        result += `${hours}ч ${minutes}мин `;
    } else if (minutes > 0) {
        result += `${minutes}мин `;
    }
    
    result += `${remainingSeconds}сек`;
    
    return result.trim();
}
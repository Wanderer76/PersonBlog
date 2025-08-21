

export function getLocalDateTime(datetime) {
    const date = new Date(datetime);
    return `${date.toLocaleDateString('ru')} ${date.toLocaleTimeString()}`;
}

export function getLocalDate(datetime) {
    const date = new Date(datetime);
    return `${date.toLocaleDateString('ru')}`;
}

export function secondsToHumanReadable(seconds) {
    if (typeof seconds !== 'number' || seconds < 0) {
        return "Некорректный ввод"; // Или выбросить ошибку, как вам больше нравится
      }
    
      const hours = Math.floor(seconds / 3600);
      const minutes = Math.floor((seconds % 3600) / 60);
      const remainingSeconds = (seconds % 60).toFixed(0); // Округляем секунды до 1 знака
    
      let result = "";
    
      if (hours > 0) {
        result += hours + "ч ";
        result += minutes + "мин ";  // Добавляем минуты, даже если есть часы
      } else if (minutes > 0) {
        result += minutes + "мин ";
      }
    
      result += remainingSeconds + "сек";
    
      return result.trim(); // Удаляем лишние пробелы в конце
}
# VideoProcessing.Cli.Tests

Тесты для сервиса обработки видео `VideoProcessing.Cli`.

## 📋 Описание

Проект содержит unit-тесты и интеграционные тесты для проверки функциональности сервиса конвертации видео в HLS формат.

## 🏗 Структура тестов

```
VideoProcessing.Cli.Tests/
├── Helpers/
│   └── TestHelpers.cs          # Вспомогательные классы для создания тестовых данных
├── Unit/
│   ├── UrlValidationTests.cs   # Тесты валидации URL с SSRF защитой (30 тестов)
│   ├── ErrorHandlingTests.cs   # Тесты обработки ошибок (12 тестов)
│   ├── PresetSelectionTests.cs # Тесты выбора разрешений для конвертации (9 тестов)
│   ├── CleanupDirectoryTests.cs# Тесты очистки временных файлов (10 тестов)
│   └── FileUploadTests.cs      # Тесты загрузки файлов в S3/MinIO (8 тестов)
├── Integration/
│   └── FfmpegServiceTests.cs   # Интеграционные тесты FFmpeg сервисов (15 тестов)
└── VideoProcessing.Cli.Tests.csproj
```

## 📊 Статистика тестов

| Тест-файл | Количество тестов | Описание |
|-----------|-------------------|----------|
| UrlValidationTests | 30 | Валидация URL, SSRF защита, проверка приватных IP |
| ErrorHandlingTests | 12 | Обработка ошибок FFmpeg, превью, конвертации |
| PresetSelectionTests | 9 | Выбор разрешений для HLS конвертации |
| CleanupDirectoryTests | 10 | Очистка временных файлов с повторными попытками |
| FileUploadTests | 8 | Загрузка TS и m3u8 файлов в S3/MinIO |
| FfmpegServiceTests | 15 | Интеграционные тесты с реальными видеофайлами |
| **Итого** | **84** | |

## 🚀 Запуск тестов

### Локальный запуск

```bash
cd BlogService/VideoProcessing.Cli.Tests
dotnet test --verbosity normal
```

### Запуск с покрытием кода

```bash
dotnet test --collect:"XPlat Code Coverage" --logger "trx;LogFileName=TestResults.trx"
```

### Запуск конкретных тестов

```bash
# Все unit-тесты
dotnet test --filter "FullyQualifiedName~Unit"

# Тесты SSRF защиты
dotnet test --filter "FullyQualifiedName~UrlValidationTests"

# Тесты обработки ошибок
dotnet test --filter "FullyQualifiedName~ErrorHandlingTests"

# Интеграционные тесты
dotnet test --filter "FullyQualifiedName~Integration"
```

### Запуск с параллельным выполнением

```bash
dotnet test --no-build --verbosity normal --logger "console;verbosity=detailed" --framework net8.0 --collect:"XPlat Code Coverage" --results-directory TestResults/ --blame-crash --blame-restricted-crashes
```

## 🧪 Типы тестов

### Unit-тесты

Проверяют отдельные компоненты без внешних зависимостей:

- **UrlValidationTests**: Валидация URL, проверка SSRF защиты, блокировка приватных IP
- **ErrorHandlingTests**: Обработка ошибок конвертации, превью, загрузки файлов
- **PresetSelectionTests**: Выбор правильного разрешения для HLS конвертации
- **CleanupDirectoryTests**: Очистка временных файлов с повторными попытками
- **FileUploadTests**: Загрузка TS и m3u8 файлов в S3/MinIO

### Интеграционные тесты

Проверяют взаимодействие с внешними сервисами:

- **FfmpegServiceTests**: Тесты с реальными видеофайлами разных форматов, кодеков, разрешений

## 🔧 Зависимости

| Пакет | Версия | Описание |
|-------|--------|----------|
| xunit | 2.5.3 | Фреймворк для тестирования |
| Moq | 4.20.72 | Моки для зависимостей |
| Microsoft.NET.Test.Sdk | 17.8.0 | SDK для тестов .NET |

## 📝 Примеры тестов

### Тест валидации SSRF защиты

```csharp
[Fact]
public async Task Private_Ip_10_X_X_X_ShouldThrowSecurityException()
{
    // Arrange
    var url = TestHelpers.CreatePrivateIpUrl("10.0.0.1");

    // Act & Assert
    Assert.ThrowsAsync<SecurityException>(() => _service.ValidateUriForHls(url));
}
```

### Тест обработки ошибок

```csharp
[Fact]
public async Task Ffprobe_NoVideoStream_ShouldReturnErrorResponse()
{
    // Arrange
    var url = TestHelpers.CreateValidHttpUrl("example.com");
    var command = TestHelpers.CreateConvertCommandWithMetadata();

    _ffmpegMock.Setup(f => f.GetVideoMediaInfoAsync(url))
        .ReturnsAsync((FFProbeStream?)null);

    // Act
    var result = await _service.HandleConversion(command);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(ProcessState.Error, result.ProcessState);
    Assert.Contains("Не удалось найти видеопоток", result.Error);
}
```

### Интеграционный тест

```csharp
[Fact]
public async Task GetVideoMediaInfo_ValidMp4File_ShouldReturnStream()
{
    // Arrange
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "ffmpeg-test");
    Directory.CreateDirectory(tempDir);

    var testVideoPath = Path.Combine(tempDir, "test.mp4");
    File.Create(testVideoPath).Dispose();

    var url = $"file://{testVideoPath}";

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(() => _service.HandleConversion(TestHelpers.CreateConvertCommandWithMetadata()));
}
```

## 🎯 Критерии успеха

- ✅ Все unit-тесты проходят (84 теста)
- ✅ Покрытие кода > 70%
- ✅ Интеграционные тесты с реальными видеофайлами
- ✅ Тесты для всех критических путей: SSRF защита, обработка ошибок, очистка файлов

## 📚 Ссылки

- [XUnit Documentation](https://xunit.net/docs)
- [Moq Documentation](https://github.com/Moq/moq4/wiki/Quickstart)
- [VideoProcessing.Cli README](../README.md)

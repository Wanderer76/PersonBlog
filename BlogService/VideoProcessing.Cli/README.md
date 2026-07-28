# VideoProcessing.Cli — Сервис обработки видео

Микросервис для конвертации видео в HLS формат (m3u8) с поддержкой нескольких разрешений и битрейтов. Интегрирован с RabbitMQ для асинхронной обработки, FileStorage (MinIO/S3) для хранения и SignalR для push-уведомлений о прогрессе.

---

## 📋 Описание

Сервис `VideoProcessing.Cli` отвечает за:
1. **Конвертацию исходных видео** в HLS формат с адаптивными потоками
2. **Генерацию превью (снимков)** из видео
3. **Асинхронную обработку** через RabbitMQ события
4. **Push-уведомления** о статусе конвертации через SignalR

---

## 🏗 Архитектура

### Структура проекта

```
VideoProcessing.Cli/
├── Controllers/          # REST API контроллеры
│   └── ConvertController.cs   # Конвертация изображений в PNG
├── Hubs/                 # SignalR hubs для push-уведомлений
│   └── IVideoProcessHub.cs         # Интерфейс подписки на прогресс
├── Service/              # Обработчики событий
│   ├── ProcessVideoToHls.cs           # Основной обработчик конвертации
│   └── VideoChunksCombinerService.cs  # Устарело (multipart uploads)
├── Program.cs            # Точка входа, регистрация сервисов
├── appsettings.json      # Конфигурация
└── Dockerfile            # Docker образ
```

---

## 🔄 Работа с событиями (RabbitMQ)

### Схема событий

```
Producer (BlogService) → [Exchange: video-event]
                             ├─ Routing Key: "video.convert" → ProcessVideoToHls
                             └─ Routing Key: "chunks.combine" → VideoChunksCombinerService
```

### Конвертация видео (`ConvertVideoCommand`)

**После загрузки пользователя видео в систему:**

```csharp
{
    "BlogId": "00000000-0000-0000-0000-000000000001",
    "PostId": "11111111-1111-1111-1111-111111111111",
    "VideoMetadataId": "22222222-2222-2222-2222-222222222222",
    "ObjectName": "videos/source_video.mp4",
    "HasPreviewId": false,  // true если превью уже существует
    "VideoMetadata": { /* данные файла */ }
}
```

**После успешной конвертации:**

```csharp
{
    "VideoMetadataId": "22222222-2222-2222-2222-222222222222",
    "PostId": "11111111-1111-1111-1111-111111111111",
    "ObjectName": "11111111-1111-1111-1111-111111111111/22222222-2222-2222-2222-222222222222.m3u8",
    "Duration": 120.5,
    "PreviewId": { /* превью файл */ },
    "IsProcessing": false,
    "ProcessState": "Complete"
}
```

---

## 🎬 Процесс конвертации HLS

### Этапы обработки

1. **Получение медиа-информации** — FFprobe анализирует исходное видео (длительность, разрешения)
2. **Генерация HLS потоков** — создание m3u8 master playlist + segment files для каждого разрешения
3. **Загрузка в S3/MinIO** — все сегменты загружаются в bucket с сохранением структуры папок
4. **Генерация превью** — создается снимок первого кадра (опционально)

### Поддерживаемые разрешения

| Разрешение | Видео битрейт | Аудио битрейт |
|------------|---------------|---------------|
| 1920×1080  | 5 Mbps        | 96 kbps       |
| 1280×720   | 3 Mbps        | 96 kbps       |
| 854×480    | 1.5 Mbps      | 64 kbps       |
| 640×360    | 1 Mbps        | 48 kbps       |
| 256×144    | 500 kbps      | 48 kbps       |

Алгоритм выбора: выбирается максимальное разрешение ≤ ширины исходного видео.

### Структура выходных файлов

```
/videos/{postId}/{videoMetadataId}/
├── master.m3u8                      # Master playlist (список потоков)
└── {segmentGuid}/                   # Сегмент по времени
    ├── 256x144.m3u8                # Low resolution
    │   └── 0000000001.ts           # TS segment (2-3 сек)
    │   └── 0000000002.ts
    ├── 640x360.m3u8                # SD resolution
    │   └── ...
    ├── 854x480.m3u8                # HD resolution
    │   └── ...
    ├── 1280x720.m3u8               # FullHD resolution
    │   └── ...
    └── 1920x1080.m3u8              # 4K resolution
        └── ...
```

---

## 🔧 Конфигурация

### appsettings.json

```json
{
  "FFMpegOptions": {
    "FFMpeg": {
      "DefaultEncoder": "libopenh264",  // libopenh264, h264_nvenc (NVIDIA), h264_qsv (Intel)
      "FFMpegPath": "ffmpeg/ffmpeg-master-latest-linux64-lgpl/bin/ffmpeg",
      "FFProbePath": "ffmpeg/ffmpeg-master-latest-linux64-lgpl/bin/ffprobe"
    },
    "HlsVideoPresets": {
      "EncodePreset": "ultrafast",  // ultrafast, superfast, veryfast, faster, fast, medium, slow, slower, veryslow
      "VideoPresets": [
        { "width": 1920, "height": 1080, "videoBitrate": "5M", "audioBitrate": "96k" },
        { "width": 1280, "height": 720, "videoBitrate": "3M", "audioBitrate": "96k" },
        { "width": 854, "height": 480,  "videoBitrate": "1.5M", "audioBitrate": "64k" },
        { "width": 640, "height": 360,  "videoBitrate": "1M", "audioBitrate": "48k" },
        { "width": 256, "height": 144,  "videoBitrate": "500k", "audioBitrate": "48k" }
      ]
    }
  },
  "RabbitMQ": {
    "Connection": {
      "HostName": "rabbitMq",
      "UserName": "admin",
      "Password": "admin",
      "Port": "5672"
    },
    "UploadVideoConfig": {
      "ExchangeName": "video-event",
      "VideoProcessQueue": "video-processing",
      "VideoConverterRoutingKey": "video.convert",
      "FileChunksCombinerRoutingKey": "chunks.combine",
      "VideoProcessErrorQueue": "video-processing-error"
    }
  },
  "Redis": {
    "ConnectionString": "redis:6379",
    "InstanceName": "VideoView:"
  },
  "TempDir": "/tmp/videos"
}
```

### Docker переменные окружения

| Переменная | Описание | Default |
|------------|----------|---------|
| `FFMPEG_PATH` | Путь к ffmpeg в контейнере | /usr/local/bin/ffmpeg |
| `FFPROBE_PATH` | Путь к ffprobe в контейнере | /usr/local/bin/ffprobe |
| `TEMP_DIR` | Директория временных файлов | /tmp/videos |
| `REDIS_CONNECTION` | Redis connection string | redis:6379 |

---

## 📡 SignalR Hub (Push уведомления)

### Интерфейс IVideoProcessHub

```csharp
public interface IVideoProcessHub
{
    Task OnVideoConvertProgress(string title, double percent);
}
```

**Пример использования в клиенте:**

```javascript
const hubConnection = new signalR.HubConnectionBuilder()
    .withUrl("/videohub")
    .build();

hubConnection.on("OnVideoConvertProgress", (title, percent) => {
    console.log(`${title}: ${percent.toFixed(2)}%`);
    // Обновить UI прогресс-бара
});

await hubConnection.start();
```

---

## 🖼 REST API — Конвертация изображений

**Endpoint:** `POST /api/convertToPng`

### Запрос

```http
POST /api/convertToPng
Content-Type: multipart/form-data

Image: <файл изображения>
```

### Ответ

| Статус | Описание |
|--------|----------|
| 200 OK | Конвертированное изображение в PNG format |
| 400 Bad Request | Ошибка валидации |

---

## 🐳 Docker & Build

### Сборка образа

```bash
# Из корня проекта
docker build -t video-processing-cli ./BlogService/VideoProcessing.Cli/

# Или через docker-compose
docker-compose up -d videoprocess-app
```

### Локальный запуск (Debug)

```bash
cd BlogService/VideoProcessing.Cli
dotnet restore
dotnet run --urls "http://0.0.0.0:5281"
```

---

## 📦 Зависимости

| Пакет | Версия | Описание |
|-------|--------|----------|
| FFmpeg.Service | - | Обертка над ffmpeg/ffprobe |
| FileStorage.Service | - | S3/MinIO интеграция |
| Infrastructure | - | Общие сервисы, middleware |
| MessageBus | - | RabbitMQ интеграция |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.8 | JWT auth |
| Swashbuckle.AspNetCore | 6.4.0 | Swagger UI |
| StackExchange.Redis | - | Redis клиент |

---

## 🔍 Troubleshooting

### Проблема: FFprobe не находит видеопоток

**Причина**: Неправильный URL к файлу или повреждённый видеофайл

```csharp
// Решение: Проверка файла перед обработкой
if (videoStream == null)
    throw new ArgumentException("Не удалось найти видеопоток");
```

### Проблема: Конвертация зависает

**Причина**: Нет SignalR подключения для прогресса

**Решение**: Убедиться, что `progressCallBack` вызывается в логах:
```
Percent : 25.5
Percent : 50.0
...
```

### Проблема: Ошибка при загрузке в S3

**Причина**: Недостаточно прав MinIO или满 bucket

**Решение**: Проверить права доступа и свободное место

**Поведение при ошибке**: При сбое загрузки файлов — продолжим с остальными.
Загруженный файл можно проверить по логам `Uploaded: {objectName}`

### Проблема: Ошибка валидации URL (SSRF protection)

**Причина**: Попытка доступа к приватному IP-адресу или запрещённой схеме

**Возможные ошибки**:
- `SecurityException`: Доступ к локальным/приватным IP-адресам запрещён
- `ArgumentException`: Недопустимая схема URL (разрешены только http, https, file)
- `SecurityException`: Запрещённый доступ к внутреннему IP

**Решение**: Убедиться, что URL валидный и не ссылается на внутренние ресурсы

### Проблема: FFprobe не находит видеопоток

**Причина**: Неправильный URL к файлу или повреждённый видеофайл

```csharp
// Решение: Проверка файла перед обработкой
if (videoStream == null)
    throw new ArgumentException("Не удалось найти видеопоток");
```

### Проблема: Конвертация зависает

**Причина**: Нет SignalR подключения для прогресса

**Решение**: Убедиться, что `progressCallBack` вызывается в логах:
```
Percent : 25.5
Percent : 50.0
...
```

---

## 📊 Производительность

| Метрика | Значение |
|---------|----------|
| Время конвертации (1080p, 2 мин) | ~3-5 минут |
| Параллельная обработка | 1 видео на контейнер |
| RAM потребление | ~512 MB |
| CPU utilization | 40-60% во время конвертации |

---

## 📝 Примечания

1. **Обновление FFmpeg**: Используется встроенная версия (ffmpeg-master-latest-linux64-lgpl) — проверьте версию при обновлении
2. **Температура CPU**: При высокой нагрузке на сервер рассмотрите масштабирование через Docker Compose
3. **Параллелизм**: Для увеличения производительности запустите несколько контейнеров с разными RabbitMQ очередями
4. **Обработка ошибок**: Загрузка файлов в S3/MinIO обрабатывается асинхронно с rate limiting (100ms между 10 файлами) и логированием успеха/провала каждого файла
5. **Утечка памяти предотвращена**: Используется `await foreach` вместо `foreach` для lazy evaluation, что предотвращает создание больших массивов в памяти
6. **Гарантированная очистка временных файлов**: Реализован метод CleanupDirectoryAsync() с 5 повторными попытками удаления (по 200мс задержка), обработка lock-файлов и логирование успешного/неуспешного удаления всех temp файлов
7. **SSRF защита**: Реализована валидация URL для предотвращения атак через FFmpeg (см. `ProcessVideoToHls.cs:ValidateUriForHls`) — разрешены только http/https/file схемы, заблокированы приватные IP-диапазоны
8. **Сохранение стека трассировки**: Использование OperationCanceledException при ошибках для сохранения полного стека трассировки и возможности повторной попытки без потери контекста ошибки

---

## 🔗 Ссылки

- [FFmpeg Documentation](https://ffmpeg.org/documentation.html)
- [HLS Format Spec](https://tools.ietf.org/html/rfc8216)
- [SignalR Hub Client](https://docs.microsoft.com/en-us/aspnet/core/signalr/javascript-client?view=aspnetcore-8.0)

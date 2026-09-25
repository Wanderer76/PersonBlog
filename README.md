# PersonBlog: Auth и Blog

PersonBlog — микросервисная платформа для публикации текстов и видео. Этот README описывает поддерживаемый сейчас клиентский контур: приложения `auth` и `blog`, а также backend-сервисы, через которые они работают.

> Проект находится в разработке. Локальный запуск через `dotnet run` воспроизводим лучше, чем полный запуск через `docker-compose.yml`: прикладной Compose требует внешнюю сеть `backend` и отсутствующие в репозитории файлы `configs/*/appsettings.json`.

## Архитектура выбранного контура

```text
auth SPA :5174
    │
    └── AuthGateway.API :5078
            └── AuthenticationApplication :5179
                    ├── PostgreSQL (schema Authentication)
                    ├── Redis (сессии и OAuth-коды)
                    └── RabbitMQ (события регистрации)

blog SPA :3000
    ├── OAuth ───────────────► AuthGateway.API :5078
    └── REST /video ─────────► Nginx :7892
                                  └── Gateway.API :5165
                                          ├── Blog.API :5069 через /profile
                                          └── другие сервисы для отдельных экранов

Blog.API
    ├── PostgreSQL (schema Blog)
    ├── Redis
    ├── RabbitMQ
    ├── MinIO
    └── VideoProcessing.Cli :5281 (обработка видео, опционально)
```

### Клиентские приложения

| Workspace | Порт | Назначение |
|---|---:|---|
| `frontend/person-blog-app/packages/auth` | 5174 | Вход и регистрация, возврат OAuth authorization code в вызывающее приложение |
| `frontend/person-blog-app/packages/blog` | 3000 | Лента, каналы, профили, посты, плейлисты, подписки и просмотр видео |

Оба приложения написаны на React и TypeScript и собираются Vite. API-клиенты генерируются Orval из OpenAPI-схем.

### Backend

| Проект | Порт | Роль |
|---|---:|---|
| `AuthService/AuthenticationApplication` | 5179 | Пользователи, пароли, OAuth-коды, access/refresh tokens |
| `GetewayService/AuthGateway.API` | 5078 | Публичный фасад для `auth` и OAuth-вызовов `blog` |
| `BlogService/Blog.API` | 5069 | Блоги, текстовые и видеопосты, категории, реакции и загрузка файлов |
| `GetewayService/Gateway.API` | 5165 | Публичный агрегирующий API клиента `blog` |
| `BlogService/VideoProcessing.Cli` | 5281 | Объединение частей файла, FFmpeg/HLS и SignalR-прогресс |
| `nginx` | 7892 | Локальная маршрутизация `/auth`, `/profile`, `/video` и SignalR hubs |

`blog` содержит клиенты и для Profile, Playlist, Recommendation, Search, Comments и Conference. Соответствующие сервисы нужны только для связанных с ними экранов; базовый auth/blog-контур можно разрабатывать без запуска всего монорепозитория.

## Требования

- .NET SDK 8;
- Node.js 20.19+ или 22.12+ и npm (требование текущего Vite 7);
- Docker с Docker Compose;
- FFmpeg — для локального запуска `VideoProcessing.Cli`.

Инфраструктурный Compose поднимает PostgreSQL 16.6, RabbitMQ Management, Redis, Redis Insight, MinIO, Nginx и Seq 2026.1.

## Локальный запуск

Команды ниже выполняются из корня репозитория, если не указан другой каталог.

### 1. Инфраструктура

```bash
docker compose -f docker-compose-infrastructre.yml up -d
```

Доступные локально интерфейсы:

| Компонент | Адрес |
|---|---|
| Nginx | `http://localhost:7892` |
| PostgreSQL | `localhost:5432` |
| RabbitMQ Management | `http://localhost:15672` |
| Redis | `localhost:6379` |
| Redis Insight | `http://localhost:5540` |
| MinIO API / Console | `http://localhost:9000` / `http://localhost:9001` |
| Seq | `http://localhost:5341` |

Локальные логины и пароли находятся в `docker-compose-infrastructre.yml` и предназначены только для разработки.

### Мониторинг сервисов в Seq

`AspireTest.ServiceDefaults` публикует метрики ASP.NET Core, исходящих HTTP-запросов, .NET Runtime и health-check напрямую в Seq через OTLP/HTTP. Локальный endpoint уже указан в `appsettings.Development.json` сервисов.

Запустите инфраструктуру, а затем нужные сервисы обычным `dotnet run`, например:

```bash
dotnet run --project AuthService/AuthenticationApplication/AuthenticationApplication.csproj
dotnet run --project BlogService/Blog.API/Blog.API.csproj
```

В разделе **Metrics** интерфейса Seq выберите `service.health` и сгруппируйте данные по `service.name`. Дополнительные атрибуты:

- `health.check.name`: `overall`, `self` или имя добавленного прикладного health-check;
- `health.status`: `healthy`, `degraded` или `unhealthy`;
- значение метрики: `1`, `0.5` или `0` соответственно.

Health-check выполняется через 5 секунд после запуска и далее каждые 30 секунд. Если сервис остановлен, новые точки от него перестают поступать; для этого сценария в Seq следует настроить оповещение об отсутствии данных.

Для графиков потребления ресурсов используйте:

- `process.cpu.utilization`: доля общей вычислительной мощности от `0` до `1`; для процентов умножьте значение на 100;
- `process.memory.usage`: физическая память процесса (working set) в байтах.

Обе метрики следует группировать по `@Resource.service.name`. Они отправляются в Seq каждые 30 секунд и одновременно остаются доступны Aspire Dashboard при запуске через AppHost.

При необходимости локальный endpoint можно переопределить переменной окружения:

```powershell
$env:SEQ_OTLP_METRICS_ENDPOINT = "http://localhost:5341/ingest/otlp/v1/metrics"
dotnet run --project AuthService/AuthenticationApplication/AuthenticationApplication.csproj
```

### 2. Auth backend

Запустите в отдельных терминалах:

```bash
dotnet run --project AuthService/AuthenticationApplication/AuthenticationApplication.csproj
dotnet run --project GetewayService/AuthGateway.API/AuthGateway.API.csproj
```

При старте `AuthenticationApplication` автоматически применяет EF Core migrations. OpenAPI:

- Authentication API: `http://localhost:5179/swagger`;
- Auth Gateway: `http://localhost:5078/swagger`.

### 3. Blog backend

```bash
dotnet run --project BlogService/Blog.API/Blog.API.csproj
dotnet run --project GetewayService/Gateway.API/Gateway.API.csproj
```

Для загрузки и конвертации видео дополнительно запустите:

```bash
dotnet run --project BlogService/VideoProcessing.Cli/VideoProcessing.Cli.csproj
```

OpenAPI:

- Blog API: `http://localhost:5069/swagger`;
- Gateway: `http://localhost:5165/swagger`;
- объединённая схема через Nginx: `http://localhost:7892/video/swagger/v1/swagger.json`.

### 4. Frontend

```bash
cd frontend/person-blog-app
npm install
npm run dev
```

`npm run dev` одновременно запускает `blog` на `http://localhost:3000` и `auth` на `http://localhost:5174`.

Переменные окружения по умолчанию:

| Workspace | Переменная | Значение |
|---|---|---|
| `auth` | `VITE_API_BASE_URL` | `http://localhost:5078` |
| `blog` | `VITE_API_BASE_URL` | `http://localhost:7892` |
| `blog` | `VITE_AUTH_API_URL` | необязательна; fallback `http://localhost:5078` |

OAuth callback клиента `blog` сейчас рассчитан на `http://localhost:3000/callback`, поэтому смена frontend-порта требует изменения конфигурации в коде.

## Генерация API-клиентов

Перед обновлением схемы должны работать соответствующие gateway и Nginx.

```bash
cd frontend/person-blog-app

npm run api:update -w=auth
npm run api:update -w=blog
```

Источники схем:

- `auth`: `http://localhost:5078/swagger/v1/swagger.json`;
- `blog`: `http://localhost:7892/video/swagger/v1/swagger.json`.

Сгенерированные файлы находятся в `src/lib/api/generated` и не должны редактироваться вручную.

## Сборка и проверки

Backend выбранного контура:

```bash
dotnet build AuthService/AuthenticationApplication/AuthenticationApplication.csproj
dotnet build GetewayService/AuthGateway.API/AuthGateway.API.csproj
dotnet build BlogService/Blog.API/Blog.API.csproj
dotnet build GetewayService/Gateway.API/Gateway.API.csproj

dotnet test AuthService/Authentication.Test/Authentication.Test.csproj
dotnet test BlogService/VideoProcessing.Cli.Test/VideoProcessing.Cli.Test.csproj
```

Frontend:

```bash
cd frontend/person-blog-app
npm run build -w=blog
npm run build -w=auth
npm run lint -w=blog
npm run lint -w=auth
```

Текущее состояние проверок:

- `blog` собирается, но его script `build` запускает только Vite и не выполняет отдельную TypeScript-проверку;
- dev-сервер и production-сборка `auth` работают;
- ESLint пока не проходит: в `auth` обнаружено 2 ошибки, в `blog` — 10 ошибок и 1 предупреждение;
- сборка backend может показывать `NU1902` для `OpenTelemetry.Exporter.OpenTelemetryProtocol` 1.9.0;
- Vite предупреждает о крупных чанках `blog` размером более 500 kB.

## OAuth-сценарий

1. Защищённый маршрут `blog` вызывает `GET /api/Auth/authorize` через Auth Gateway.
2. Authentication API возвращает адрес приложения `auth` с `clientId`, `redirectUri` и `state`.
3. После входа или регистрации `auth` возвращает authorization code на `/callback` клиента `blog`.
4. `blog` проверяет `state`, обменивает одноразовый code на пару токенов и сохраняет их в `localStorage`.
5. Axios добавляет access token к запросам, а при `401` выполняет один общий refresh и повторяет ожидающие запросы.

## Известные ограничения

- На backend пока отключена проверка OAuth client credentials и разрешённого `redirectUri`; контур нельзя считать готовым к публичному развёртыванию.
- SPA передаёт `client_secret` и хранит access/refresh tokens в `localStorage`; перед production нужен PKCE и пересмотр модели хранения токенов.
- Полный `docker-compose.yml` не является автономным: нужны внешняя сеть `backend` и локальные конфигурации `configs/*`.
- В конфигурации и исходниках остаются dev credentials. Не используйте их вне локальной среды и не коммитьте реальные секреты.

## Структура контура

```text
PersonBlog/
├── AuthService/
│   ├── AuthenticationApplication/     HTTP API и composition root
│   ├── Authentication.Service/         auth/OAuth/token logic
│   ├── Authentication.Domain/          пользователи, клиенты и токены
│   └── Authentication.Peristence/      EF Core и migrations
├── BlogService/
│   ├── Blog.API/                       внутренний Blog API
│   ├── Blog.Service/                   прикладная логика
│   ├── Blog.Domain/                    доменная модель
│   ├── Blog.Persistence/               EF Core и migrations
│   └── VideoProcessing.Cli/            обработка видео
├── GetewayService/
│   ├── AuthGateway.API/                фасад auth
│   └── Gateway.API/                    фасад blog
├── frontend/person-blog-app/packages/
│   ├── auth/
│   └── blog/
├── nginx/
├── docker-compose-infrastructre.yml
└── docker-compose.yml
```

Название каталога `GetewayService` и файла `docker-compose-infrastructre.yml` приведено как в репозитории.

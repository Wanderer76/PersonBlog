# PersonBlog

PersonBlog — экспериментальная платформа для публикации текстов, видео и музыки, построенная как микросервисный монорепозиторий.

Основной backend написан на **.NET 8**. В репозитории также находятся клиентские приложения на **React + TypeScript**, административное приложение на **Spring Boot 3.5.4 / Java 21** и Python-сервис для токенизации и классификации музыки.

> Проект находится в активной разработке. Docker Compose пока покрывает только часть сервисов, а для запуска требуются локальные конфигурации и инфраструктура.

## Архитектура

```text
React applications
        │
   Nginx / Ocelot
        │
 ┌──────┼──────────────────────────────────┐
 │      │                                  │
Auth   Blog / media                  Profile / social
 │      │                                  │
 └──────┴──── RabbitMQ / Kafka ────────────┘
        │
 PostgreSQL / Redis / MinIO
```

.NET-сервисы преимущественно разделены на следующие слои:

```text
*.API          HTTP API, контроллеры и composition root
*.Service      прикладная и бизнес-логика
*.Domain       доменные сущности и интерфейсы
*.Persistence  EF Core, DbContext и миграции
*.Contract     DTO, события и HTTP-клиенты
```

Сервисы взаимодействуют синхронно по HTTP и асинхронно через RabbitMQ или Kafka. Для обновлений в реальном времени используется SignalR.

## Сервисы

| Каталог | Назначение | Запускаемый проект |
|---|---|---|
| `AuthService` | JWT/OAuth, регистрация и управление токенами | `AuthenticationApplication` |
| `BlogService` | Блоги, публикации, категории, подписки и медиа | `Blog.API` |
| `BlogService/VideoProcessing.Cli` | Конвертация видео и подготовка HLS через FFmpeg | `VideoProcessing.Cli` |
| `ProfileService` | Профили, реакции, подписки и история просмотров | `Profile.API` |
| `CommentService` | Комментарии | `Comments.API` |
| `ConferenceService` | Конференции, чат и SignalR | `Conference.API` |
| `MusicService` | Треки, исполнители, жанры и музыкальные плейлисты | `Music.API` |
| `PlayListService` | Плейлисты и связанные файлы | `PlayListService.API` |
| `NotificationService` | Уведомления | `Notification.API` |
| `SearchService` | Поиск через PostgreSQL или Elasticsearch | `SearchService.Application` |
| `RecommendationService` | Рекомендации публикаций | `Recommendation.Application` |
| `MusicRecommendationService` | Рекомендации музыки | `MusicRecommendation.API` |
| `GetewayService/Gateway.API` | Основной API Gateway на Ocelot | `Gateway.API` |
| `GetewayService/AuthGateway.API` | Отдельный gateway для аутентификации | `AuthGateway.API` |
| `GetewayService/ReactionProcessing.Cli` | Фоновая обработка реакций | `ReactionProcessing.Cli` |
| `TokenizerService` | Python API для токенизации и ML-классификации жанров | `main.py` |

Название каталога `GetewayService` сохранено в том виде, в котором оно сейчас используется в solution и ссылках проектов.

## Общие библиотеки

Каталог `Common` содержит переиспользуемые компоненты backend:

- `FFmpeg.Service` — интеграция с FFmpeg;
- `FileStorage.Service` — файловое и S3-совместимое хранилище, загрузка частями;
- `Infrastructure` — общие инфраструктурные сервисы;
- `MessageBus` и `MessageBus.Shared` — абстракции RabbitMQ/Kafka и общие сообщения;
- `Shared` — общие модели и утилиты.

## Frontend

Frontend расположен в `frontend/person-blog-app` и организован как npm workspaces:

| Workspace | Назначение |
|---|---|
| `packages/blog` | Основной интерфейс блога |
| `packages/auth` | Авторизация |
| `packages/admin` | Клиентская административная панель |
| `packages/music` | Музыкальный интерфейс |

Основные технологии: React, TypeScript, Vite, React Router, Axios, Tiptap, Bootstrap/MUI, HLS.js и Video.js. Для `blog` и `auth` предусмотрена генерация API-клиентов из OpenAPI через Orval.

Корневые команды frontend:

```bash
cd frontend/person-blog-app
npm install

npm run dev          # blog + auth
npm run dev:blog
npm run dev:auth
npm run dev:admin
npm run dev:music

npm run build        # blog + admin
npm run build:blog
npm run build:admin
npm run build:music
```

Команда `npm run dev` не запускает `admin` и `music`, а корневая команда `npm run build` пока не собирает `auth` и `music`.

## Административное приложение

В `adminPanel` находится отдельное серверное приложение:

- Java 21;
- Spring Boot 3.5.4;
- Spring MVC и Thymeleaf;
- Spring Data JPA и PostgreSQL;
- RabbitMQ;
- springdoc-openapi;
- Maven Wrapper.

Запуск:

```bash
cd adminPanel
./mvnw spring-boot:run
```

В Windows PowerShell используйте `./mvnw.cmd spring-boot:run`.

## Инфраструктура

Файл `docker-compose-infrastructre.yml` описывает:

- PostgreSQL 16.6;
- RabbitMQ Management;
- Redis и Redis Insight;
- MinIO;
- Nginx.

Файл `docker-compose.yml` собирает только следующий прикладной контур:

| Контейнер | Порт хоста |
|---|---:|
| `auth-app` | 5179 |
| `blog-app` | 5069 |
| `videoprocess-app` | внутренний 8080 |
| `playlist-app` | 5147 |
| `gateway-app` | 5165 |
| `profile-app` | 5153 |
| `recommendation-app` | 5209 |

Остальные API пока необходимо запускать отдельно или добавить в Compose.

Прикладной Compose ожидает внешнюю Docker-сеть `backend` и файлы `configs/<service>/appsettings.json`. Каталог `configs` не хранится в репозитории, поэтому перед контейнерным запуском необходимо подготовить конфигурации с адресами PostgreSQL, RabbitMQ, Redis и MinIO, а также создать сеть:

```bash
docker network create backend
```

При совместном запуске обоих Compose-файлов проверьте, что инфраструктурные и прикладные контейнеры подключены к одной сети. Текущие файлы не являются полностью автономным production deployment.

## Локальная разработка

### Требования

- .NET SDK 8;
- Node.js и npm, совместимые с используемой версией Vite;
- Java 21 для `adminPanel`;
- Python и зависимости из `TokenizerService/requirements.txt` для tokenizer;
- PostgreSQL, RabbitMQ, Redis, MinIO и FFmpeg — в зависимости от запускаемого сервиса;
- Docker — опционально.

### Backend

Восстановление и сборка .NET solution:

```bash
dotnet restore PersonBlog.sln
dotnet build PersonBlog.sln
```

Запуск отдельного сервиса, например Auth API:

```bash
dotnet run --project AuthService/AuthenticationApplication/AuthenticationApplication.csproj
```

Другие API запускаются аналогично через их `.csproj`. Настройки разработки находятся в соответствующих `appsettings.json`, `appsettings.Development.json` и `Properties/launchSettings.json`.

### TokenizerService

```bash
cd TokenizerService
python -m venv .venv
# активируйте виртуальное окружение
pip install -r requirements.txt
python main.py
```

## Тесты

Запустить все тестовые проекты, подключённые к solution:

```bash
dotnet test PersonBlog.sln
```

Отдельные тестовые проекты находятся в `AuthService/Authentication.Test`, `BlogService/VideoProcessing.Cli.Test` и `Tests`.

### Тесты производительности MessageBus

Проект `Tests/MessageBus.Benchmarks` содержит микробенчмарки горячего пути диспетчеризации событий, общего для реализаций RabbitMQ и Kafka. Он сравнивает:

- прежнюю диспетчеризацию с заранее известным generic-типом события;
- динамическое определение типа и вызов через reflection;
- динамическую диспетчеризацию с кэшированным delegate;
- вариант с однократным разбором JSON и десериализацией только `EventData`.

Полный прогон следует выполнять в конфигурации Release:

```bash
dotnet run --project Tests/MessageBus.Benchmarks/MessageBus.Benchmarks.csproj -c Release -- --filter "*EventDispatchBenchmarks*"
```

Для быстрой проверки сборки и запуска без статистически значимых измерений используйте `Dry`-режим:

```bash
dotnet run --project Tests/MessageBus.Benchmarks/MessageBus.Benchmarks.csproj -c Release -- --filter "*EventDispatchBenchmarks*" --job dry
```

BenchmarkDotNet сохраняет подробные отчёты в `BenchmarkDotNet.Artifacts`. Этот каталог исключён из Git. Результаты зависят от оборудования, версии runtime и фоновой нагрузки, поэтому сравнивать варианты следует в рамках одного прогона на одной машине.

## Структура репозитория

```text
PersonBlog/
├── adminPanel/                    Spring Boot admin application
├── AuthService/                   authentication and authorization
├── BlogService/                   blog API and video processing
├── CommentService/                comments
├── Common/                        shared .NET infrastructure
├── ConferenceService/             conferences and real-time chat
├── frontend/person-blog-app/      React npm workspaces
├── GetewayService/                gateways and reaction processing
├── MusicRecommendationService/    music recommendations
├── MusicService/                  music catalog and playback
├── NotificationService/           notifications
├── PlayListService/               playlists
├── ProfileService/                profiles and social activity
├── RecommendationService/         post recommendations
├── SearchService/                 content search
├── Tests/                         unit, regression and performance tests
├── TokenizerService/              Python tokenizer and ML models
├── nginx/                         local reverse-proxy configuration
├── docker-compose.yml             partial application stack
├── docker-compose-infrastructre.yml
└── PersonBlog.sln
```

## Состояние проекта

- Все собственные .NET-проекты ориентированы на `net8.0`.
- Docker Compose не включает все сервисы репозитория.
- Конфигурации могут содержать секреты: не коммитьте реальные пароли, ключи JWT и OAuth credentials.
- В репозитории присутствуют крупные FFmpeg/ML-артефакты; при дальнейшем росте проекта стоит перенести их в Git LFS или объектное хранилище.

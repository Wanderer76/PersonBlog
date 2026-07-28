# PersonBlog - Личный блог микросервисная архитектура

Персональная блогирующая платформа с микросервисной архитектурой, реализованная на .NET 8/10 (backend) и Spring Boot (admin panel), с фронтендом на React 18.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-8%2F10-purple.svg)
![Spring Boot](https://img.shields.io/badge/Spring-3.5.4-green.svg)
![React](https://img.shields.io/badge/React-18-blue.svg)

---

## 🏗 Архитектура

Проект организован как **monorepo** с микросервисной архитектурой:

```
PersonBlog (Monorepo)
├── Backend (Microservices - .NET 8/10)
│   ├── AuthService         # Аутентификация
│   ├── BlogService         # Управлени постами
│   ├── ProfileService      # Профили пользователей
│   ├── VideoService        # Обработка видео (FFmpeg)
│   ├── PlayListService     # Плейлисты
│   ├── NotificationService # Уведомления
│   ├── MusicService        # Музыка
│   ├── RecommendationService # Рекомендации
│   ├── SearchService       # Поиск
│   └── GatewayService      # API Gateway (Ocelot)
├── Admin Panel             # Spring Boot админка
└── Frontend                # React 18 приложение
```

---

## 📦 Микросервисы

### Блог и медиа-сервисы

| Сервис | Порт | Описание |
|--------|------|----------|
| **Blog.API** | 5069 | Основной API блога, управление постами |
| **VideoProcessing.Cli** | 5281 | Обработка видео через FFmpeg |
| **PlayListService.API** | 5147 | Управление плейлистами и треками |
| **Music.API** | - | Воспроизведение музыки, HLS |
| **Notification.API** | - | Системные уведомления |

### Пользовательские сервисы

| Сервис | Порт | Описание |
|--------|------|----------|
| **AuthenticationApplication** | 5179 | Аутентификация и авторизация (JWT) |
| **Profile.API** | 5153 | Профили пользователей |
| **Comments.API** | - | Система комментариев |

### Инфраструктурные сервисы

| Сервис | Порт | Описание |
|--------|------|----------|
| **Gateway.API** | 5165 | API Gateway (Ocelot), routing |
| **Recommendation.Application** | 5209 | Рекомендательная система |
| **Search.Service** | - | Поиск контента |
| **ConferenceService** | - | Конференции/мероприятия |

---

## 🎨 Frontend (React)

**frontend/person-blog-app/** — React 18 + TypeScript workspace application

### Технологии
- **React 18** с React Router для навигации
- **Tiptap** - WYSIWYG редактор для постов
- **HLS.js / Video.js** - воспроизведение видео
- **Bootstrap 5.3.3** - стилизация
- **Axios** - HTTP клиент

### Workspaces
```json
"workspaces": [
  "packages/blog",      # Клиент блога
  "packages/admin",     # Админ панель
  "packages/music",     # Музыкальный плеер
  "packages/auth",      # Авторизация
  "packages/shared"     # Общие компоненты
]
```

### Скрипты
```bash
npm run dev           # Запуск всех сервисов concurrently
npm run dev:blog      # Только блог
npm run dev:auth      # Только auth
npm run build         # Построение всех пакетов
```

---

## 🖥 Admin Panel (Spring Boot)

**adminPanel/** — Административная панель на Spring Boot 3.5.4

### Технологии
- **Spring Boot 3.5.4**
- **Spring Data JPA** - ORM
- **Thymeleaf** - templating
- **PostgreSQL** - база данных
- **RabbitMQ** - мессенджер
- **springdoc-openapi 2.6.0** - OpenAPI документация

### Зависимости (ключевые)
```xml
<dependencies>
    <dependency>
        <groupId>org.springframework.boot</groupId>
        <artifactId>spring-boot-starter-data-jpa</artifactId>
    </dependency>
    <dependency>
        <groupId>org.springframework.boot</groupId>
        <artifactId>spring-boot-starter-thymeleaf</artifactId>
    </dependency>
    <dependency>
        <groupId>org.springframework.boot</groupId>
        <artifactId>spring-boot-starter-amqp</artifactId>
    </dependency>
</dependencies>
```

---

## 🛠 Технологический стек

| Слой | Технологии |
|------|------------|
| **Backend .NET** | .NET 8/10, ASP.NET Core Web API, EF Core, Ocelot Gateway |
| **Admin Panel** | Spring Boot 3.5.4, Maven, Thymeleaf, PostgreSQL |
| **Frontend** | React 18, TypeScript, Tiptap, Bootstrap 5 |
| **Database** | PostgreSQL |
| **Messaging** | RabbitMQ, Kafka |
| **Media Processing** | FFmpeg (через Common/FFmpeg.Service) |
| **Containerization** | Docker, Docker Compose |

---

## 🏗 Архитектурные паттерны

### DDD (Domain-Driven Design) для .NET сервисов

Каждый сервис разделен на слои:
```
ServiceName/
├── .Domain           # Domain entities, DTOs, интерфейсы репозиториев
├── .Persistence      # EF Core DbContext, конфигурации实体
├── .Service          # Business logic, сервисные методы
├── .Contract         # API contracts, запросы/ответы
├── .API              # Web API entry point, контроллеры
└── .Test             # Unit и integration тесты
```

### Общие сервисы (Cross-cutting concerns)

**Common/** - разделяемая инфраструктура:
- **FFmpeg.Service** - обработка видео
- **FileStorage.Service** - хранение файлов
- **Infrastructure** - базовая инфраструктура (logging, config)
- **MessageBus / MessageBus.Shared** - мессенджер абстракции (RabbitMQ/Kafka)
- **Shared** - общие модели и утилиты

---

## 🐳 Docker & Инфраструктура

### Сервисы в Docker Compose

```yaml
Services:
  auth-app          # 5179:8080
  blog-app          # 5069:8080
  video-process-app # 5281:8080
  playlist-app      # 5147:8080
  gateway-app       # 5165:8080
  profile-app       # 5153:8080
  recommendation-app# 5209:8080

Networks:
  backend           # External network для подключения сервисов
```

### Конфигурация окружения
Каждый сервис монтирует свой `appsettings.json` из папки `configs/<service-name>/`

---

## 📁 Структура проекта

```
e:\PersonBlog\
├── adminPanel/                   # Spring Boot админка
│   ├── pom.xml
│   └── src/main/java/com/personBlog/adminPanel/
├── AuthService/                  # Аутентификация (.NET)
│   ├── Authentication.Domain/
│   ├── Authentication.Peristence/
│   ├── Authentication.Service/
│   ├── Authentication.Application/
│   └── AuthenticationApplication/
├── BlogService/                  # Блог (.NET)
│   ├── Blog.API/
│   ├── Blog.Domain/
│   ├── Blog.Persistence/
│   ├── Blog.Service/
│   └── VideoProcessing.Cli/     # Видео обработка
├── Common/                       # Общие сервисы
│   ├── FFmpeg.Service/
│   ├── FileStorage.Service/
│   ├── Infrastructure/
│   ├── MessageBus/              # RabbitMQ/Kafka
│   └── Shared/
├── frontend/                     # React приложение
│   └── person-blog-app/
│       └── packages/            # Workspaces
├── GetewayService/               # Gateway (Ocelot)
├── NotificationService/          # Уведомления
├── ProfileService/               # Профили
├── RecommendationService/        # Рекомендации
├── PlayListService/              # Плейлисты
├── MusicService/                 # Музыка
├── docker-compose.yml
├── PersonBlog.sln                # .NET решение
└── QWEN.md                       # Контекст проекта
```

---

## 🚀 Запуск

### Локально (.NET сервисы)
```bash
# Восстановить зависимости
dotnet restore PersonBlog.sln

# Задолжательства все пакеты
dotnet build PersonBlog.sln

# Запуск решения
dotnet run --project AuthService/AuthenticationApplication/AuthenticationApplication.csproj
```

### Docker
```bash
docker-compose up -d
```

### Frontend
```bash
cd frontend/person-blog-app
npm install
npm run dev
```

---

## 🔑 Ключевые особенности

1. **Микросервисная архитектура** с API Gateway (Ocelot)
2. **DDD паттерн** для .NET сервисов (Domain/Service/Persistence/Contract separation)
3. **Cross-cutting concerns**: общие сервисы FFmpeg, FileStorage, MessageBus
4. **Real-time коммуникация**: SignalR для уведомлений, RabbitMQ/Kafka для мессенджера
5. **Media processing**: FFmpeg для обработки видео
6. **Multi-modal контент**: текстовые посты + видео + музыка + конференции

---

## 📝 Лицензия

MIT License

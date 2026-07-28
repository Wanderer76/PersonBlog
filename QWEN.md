# PersonBlog - Личный блог (Микросервисная архитектура)

## 📋 Описание проекта

Личная блогирующая платформа с микросервисной архитектурой на базе .NET 8/10 и Spring Boot. Поддерживает текстовые посты, видео, музыку, конференции и систему рекомендаций.

**Текущая версия**: 0.1.0  
**Последнее обновление**: 2026-07-28

---

## 🏗 Архитектура проекта

### Общая структура (Monorepo)

```
PersonBlog/
├── Backend (Microservices - .NET 8/10)
│   ├── AuthService         # Аутентификация & авторизация
│   ├── BlogService         # Управление постами
│   ├── ProfileService      # Профили пользователей
│   ├── CommentService      # Комментарии
│   ├── VideoService        # Обработка видео (FFmpeg)
│   ├── PlayListService     # Плейлисты и музыка
│   ├── NotificationService # Уведомления (SignalR)
│   ├── MusicService        # Воспроизведение музыки (HLS/Video.js)
│   ├── RecommendationService # Рекомендации контента
│   ├── SearchService       # Поиск по контенту
│   ├── ConferenceService   # Мероприятия/конференции
│   ├── TokenizerService    # Обработка текста
│   └── GetewayService      # API Gateway (Ocelot)
│
├── Admin Panel             # Spring Boot 3.5.4 административная панель
├── Frontend                # React 18 + TypeScript workspace
├── Common/                 # Общие сервисы (.NET)
│   ├── FFmpeg.Service      # Обработка видео
│   ├── FileStorage.Service # Хранение файлов
│   ├── Infrastructure      # Базовая инфраструктура
│   ├── MessageBus          # RabbitMQ/Kafka мессенджер
│   └── Shared              # Общие модели и утилиты
├── Tests/                  # Тесты (xUnit)
└── nginx/                  # Конфигурация Nginx
```

---

---
На данный момент adminPanel, MusicService, MusicRecommendationService и все что не относится к .net и react не рассматриваются
---


## 🛠 Технологический стек

### Backend (.NET)
| Технология | Версия | Описание |
|------------|--------|----------|
| .NET | 8.0 / 10.0 | Основной фреймворк |
| ASP.NET Core Web API | - | REST API сервисы |
| Entity Framework Core | - | ORM и миграции |
| Ocelot | - | API Gateway |
| SignalR | - | Real-time уведомления |

### Admin Panel (Java)
| Технология | Версия | Описание |
|------------|--------|----------|
| Spring Boot | 3.5.4 | Основной фреймворк |
| Maven | - | Build система |
| Thymeleaf | - | Templating engine |
| Spring Data JPA | - | ORM |
| springdoc-openapi | 2.6.0 | OpenAPI документация |

### Frontend (React)
| Технология | Версия | Описание |
|------------|--------|----------|
| React | 18.x | UI framework |
| TypeScript | - | Типизация |
| Tiptap | 3.20.5 | WYSIWYG редактор |
| Video.js / HLS.js | - | Видео плеер |
| Bootstrap | 5.3.3 | Стилизация |
| React Router | 7.1.1 | Навигация |

### Инфраструктура
| Технология | Описание |
|------------|----------|
| PostgreSQL | Основная база данных |
| RabbitMQ | Message broker |
| Kafka | Event streaming |
| FFmpeg | Обработка видео |
| Docker / Docker Compose | Контейнеризация |
| Nginx | Reverse proxy, static files |

---

## 🏭 Микросервисы (подробно)

### BlogService
- **Порт**: 5069
- **Функции**: CRUD постов, медиафайлы, rich-text редактор
- **Проекты в решении**:
  - `Blog.API` — Web API контроллеры
  - `Blog.Domain` — доменная логика
  - `Blog.Persistence` — EF Core DbContext
  - `Blog.Service` — бизнес-логика
  - `VideoProcessing.Cli` — обработка видео (FFmpeg)

### AuthService
- **Порт**: 5179
- **Функции**: JWT аутентификация, ролевая модель
- **Проекты в решении**:
  - `AuthenticationApplication` — entry point
  - `Authentication.Domain/Service/Persistence/Contract` — слои DDD
  - `Authentication.Test` — тесты

### GatewayService
- **Порт**: 5165
- **Функции**: Routing, rate limiting, auth gateway
- **Проекты в решении**:
  - `Gateway.API` — основной gateway
  - `AuthGateway.API` — аутентификационный gateway
  - `AspireTest.AppHost` — Aspire тестовое приложение

### ProfileService
- **Порт**: 5153
- **Функции**: Профили пользователей, аватары, настройки
- **Проекты в решении**:
  - `Profile.API/Domain/Persistence/Service` — слои DDD

### PlayListService
- **Порт**: 5147
- **Функции**: Управление плейлистами, треками, жанры
- **Проекты в решении**:
  - `PlayListService.API/Domain/Services/Persistence/Contract` — слои DDD

### MusicService + MusicRecommendationService
- **Функции**: Воспроизведение музыки через HLS, рекомендации
- **Технологии**: Video.js для плеера

### NotificationService
- **Функции**: Real-time уведомления через SignalR
- **Интеграции**: Push notifications

### RecommendationService
- **Порт**: 5209
- **Функции**: Рекомендации постов, музыки, плейлистов

### Common (Shared Services)
| Проект | Описание |
|--------|----------|
| `FFmpeg.Service` | Обработка видео конвертация |
| `FileStorage.Service` | S3/локальное хранение файлов |
| `MessageBus` / `MessageBus.Shared` | Абстракции над RabbitMQ и Kafka |
| `Infrastructure` | Logging, configuration, DI setup |
| `Shared` | Общие DTOs, enums, extensions |

---

## 🏗 Архитектурные паттерны

### Domain-Driven Design (DDD) для .NET сервисов

Структура каждого сервиса:
```
ServiceName/
├── .Domain           # Entities, Value Objects, Interfaces
├── .Persistence      # DbContext, Entity Configurations
├── .Service          # Business logic, Services
├── .Contract         # DTOs, API requests/responses
├── .API              # Controllers, Program.cs
└── .Test             # Unit/Integration tests
```

### Clean Architecture принципы
- Separation of concerns (Domain/Service/Persistence)
- Dependency inversion через interfaces
- CQRS паттерн для отдельных сервисов

---

## 📦 Frontend Workspace (React)

**frontend/person-blog-app/** — React application с yarn workspaces:

```json
"workspaces": [
  "packages/blog",      # Blog client components
  "packages/admin",     # Admin dashboard
  "packages/music",     # Music player UI
  "packages/auth",      # Auth flows
  "packages/shared"     # Shared React components/hooks
]
```

**Основные скрипты**:
```bash
npm run dev          # Запуск всех сервисов concurrently
npm run dev:blog     # Только блог
npm run dev:auth     # Только auth
npm run build        # Построение всех пакетов
```

---

## 🖥 Admin Panel (Spring Boot)

**adminPanel/** — Административная панель на Spring Boot 3.5.4

### Ключевые зависимости
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
    <dependency>
        <groupId>org.springdoc</groupId>
        <artifactId>springdoc-openapi-starter-webmvc-ui</artifactId>
        <version>2.6.0</version>
    </dependency>
</dependencies>
```

**Технологии**: Thymeleaf templating, PostgreSQL, RabbitMQ integration

---

## 🐳 Docker & Инфраструктура

### docker-compose.yml - Services
| Сервис | Контейнер | Порт внутри |
|--------|-----------|-------------|
| auth-app | auth-app:latest | 8080 → 5179 |
| blog-app | blog-app:latest | 8080 → 5069 |
| video-process-app | videoprocess-app:latest | 8080 → 5281 |
| playlist-app | playlist-app:latest | 8080 → 5147 |
| gateway-app | gateway-app:latest | 8080 → 5165 |
| profile-app | profile-app:latest | 8080 → 5153 |
| recommendation-app | recommendation-app:latest | 8080 → 5209 |

**Network**: `backend` (external) — для подключения внешних сервисов

**Вolumes**: Каждый сервис монтирует свой конфиг из `configs/<service-name>/appsettings.json`

---

## 📝 Правила работы (Coding Standards)

### .NET / C#
- Использовать C# 12 фичи (primary constructors, raw string literals)
- Именованные инициализаторы для всех свойств
- Async/await везде где возможно
- Использовать `IRepository<T>` паттерн в Persistence слое
- DTOs через AutoMapper или manual mapping
- Logging через Microsoft.Extensions.Logging
- Игнорировать проблемы secrets manager

### React / TypeScript
- Функциональные компоненты с hooks
- Type-safe props и state
- Custom hooks для reusable логики
- ESLint + Prettier для форматирования

### Админ панель (Java)
- Java 21
- Lombok для boilerplate reduction
- REST controllers с Spring MVC
- Thymeleaf templates для UI

---

## 🔑 Ключевые особенности проекта

1. **Микросервисная архитектура** с Ocelot Gateway для routing между сервисами
2. **DDD паттерн** — четкое разделение Domain/Service/Persistence/Contract
3. **Real-time коммуникация** через SignalR (уведомления) и RabbitMQ/Kafka
4. **Media processing** — FFmpeg для видео обработки, HLS для потокового аудио
5. **Multi-modal контент** — текстовые посты + видео + музыка + конференции
6. **Рекомендательная система** — персонализация контента
7. **Cross-cutting concerns** — общие сервисы (FFmpeg, FileStorage, MessageBus)

---

## 📚 Полезные ссылки

- [OpenAPI Swagger](http://localhost:5165/swagger) — документация Gateway
- [Admin Panel (Spring Boot)](http://localhost:8080/admin) — админка
- [Frontend](http://localhost:3000) — основной фронтенд

---

## 🗺 Структура директорий проекта

```
e:\PersonBlog\
├── adminPanel/              # Spring Boot Maven проект
├── AuthService/             # .NET аутентификация (DDD)
├── BlogService/             # .NET блог сервис + видео обработка
├── CommentService/          # .NET комментарии
├── ConferenceService/       # .NET мероприятия
├── Common/                  # Общие .NET сервисы (FFmpeg, Storage, MessageBus)
├── frontend/person-blog-app/# React workspace приложение
├── GetewayService/          # Ocelot API Gateway
├── NotificationService/     # SignalR уведомления
├── PlayListService/         # .NET плейлисты и музыка
├── ProfileService/          # .NET профили пользователей
├── RecommendationService/   # .NET рекомендательная система
├── SearchService/           # .NET поиск
├── TokenizerService/        # .NET обработка текста
├── VideoService/            # .NET видео сервис (игнорируется git)
├── Tests/                   # xUnit тесты
├── docker-compose.yml       # Docker конфигурация
├── docker-compose-infrastructre.yml # Инфраструктура (БД, MQ)
├── nginx/                   # Nginx конфигурация
├── PersonBlog.sln           # .NET решение
├── README.md                # Документация проекта
└── QWEN.md                  # Контекст для Qwen агента
```

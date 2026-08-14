# Recommendation Service

## Цель

Превратить текущий `RecommendationService` в независимый микросервис рекомендаций постов. Сервис должен владеть собственной read model, получать изменения через RabbitMQ, строить персонализированную выдачу эвристическим алгоритмом и не читать базы данных Blog/Profile напрямую.

Первый production-вариант не использует ML. Архитектура должна позволять позднее добавить embeddings и обучаемый ranker без изменения публичного API.

## Границы ответственности

Recommendation Service отвечает за:

- локальную проекцию доступного для рекомендаций контента;
- локальную историю пользовательских сигналов и агрегированные предпочтения;
- генерацию кандидатов, расчёт score, фильтрацию и диверсификацию;
- cursor pagination;
- фиксацию показов рекомендаций;
- версионирование алгоритма и метрики качества выдачи.

Recommendation Service не должен:

- ссылаться на `Blog.Persistence`, `Profile.Persistence` или их EF-сущности;
- читать таблицы, принадлежащие другим сервисам;
- синхронно обращаться к Profile Service при построении каждой ленты;
- принимать решение о доступе к приватному или платному контенту без необходимых данных в локальном snapshot;
- генерировать временные MinIO URL в процессе ранжирования.

## Целевой поток данных

1. Blog Service публикует versioned-события создания, обновления и удаления постов.
2. Profile Service публикует просмотры, реакции, подписки и отписки.
3. Consumers Recommendation Service идемпотентно обновляют собственную PostgreSQL read model.
4. API формирует несколько наборов кандидатов, объединяет их и рассчитывает score.
5. После обязательных фильтров и диверсификации API возвращает IDs постов и cursor.
6. Gateway получает актуальные карточки одним bulk-запросом к Blog Service.
7. Факт показа записывается как impression и используется для оценки рекомендаций.

## Предлагаемая структура solution

```text
RecommendationService/
  Recommendation.Application/   HTTP API, DI, consumers, hosted services
  Recommendation.Contracts/     публичные DTO и versioned integration events
  Recommendation.Domain/        scoring, policies, domain models
  Recommendation.Persistence/   DbContext, migrations, repositories, inbox
  Recommendation.Tests.Unit/
  Recommendation.Tests.Integration/
```

`Recommendation.Service` следует постепенно заменить проектами выше. Общие transport-контракты не должны зависеть от EF или доменных сущностей Blog/Profile.

## Этап 0. Зафиксировать продуктовые правила

- [ ] Определить основной сценарий первой версии: рекомендации рядом с видео, главная лента или оба сценария.
- [ ] Определить, рекомендуются ли только видео или также текстовые посты.
- [ ] Зафиксировать правила для приватного и платного контента.
- [ ] Решить, исключается ли контент собственного блога пользователя.
- [ ] Определить окно повторного показа просмотренного поста.
- [ ] Установить максимум постов одного блога в одной странице.
- [ ] Определить cold-start поведение для анонимного и нового пользователя.
- [ ] Утвердить максимальный `limit`, целевой p95 и допустимую задержку проекции событий.

### Результат этапа

Короткий ADR с продуктовыми правилами, которые можно выразить тестами.

## Этап 1. Разделить проекты и зависимости

- [x] Создать `Recommendation.Contracts` без ссылок на Blog/Profile проекты.
- [x] Создать `Recommendation.Domain` без ASP.NET Core и EF Core.
- [ ] Создать `Recommendation.Persistence` с отдельным `RecommendationDbContext`.
- [ ] Перенести use cases и интерфейсы из старого `Recommendation.Service`.
- [ ] Удалить ссылку Application на `Blog.Persistence`.
- [ ] Удалить ссылки Recommendation-проектов на `Blog.Domain` и `Profile.Domain`.
- [ ] Переименовать `AddBlogServices` в `AddRecommendationServices`.
- [ ] Добавить отдельную connection string и отдельного пользователя PostgreSQL.
- [ ] Добавить Recommendation DB в Docker Compose и Aspire AppHost.
- [ ] Настроить health checks PostgreSQL, RabbitMQ и Redis.

### Критерии готовности

- Recommendation API запускается без Blog DbContext.
- Пользователь Recommendation DB не имеет доступа к схемам Blog/Profile.
- Сборка не содержит project reference на persistence другого bounded context.

## Этап 2. Собственная read model

Создать следующие таблицы.

### `PostSnapshots`

- `PostId` — primary key;
- `SourceVersion` — версия aggregate для защиты от перестановки событий;
- `BlogId`, `PostType`;
- `Title`, `Description`;
- `CategoryIds` либо нормализованная `PostCategories`;
- `Visibility`, `ProcessState`;
- `IsDeleted`, `IsBanned`;
- `PaymentSubscriptionId` при необходимости;
- `CreatedAt`, `DurationSeconds`;
- `ViewCount`, `LikeCount`, `DislikeCount`;
- минимальные presentation-поля, необходимые на переходном этапе;
- `UpdatedAt`.

### `UserInteractions`

- `EventId` — unique key;
- `UserId` или `AnonymousSessionId`;
- `PostId`;
- `InteractionType`;
- `WatchedSeconds`, `WatchRatio`;
- `OccurredAt`.

### `UserCategoryAffinities`

- составной ключ `UserId + CategoryId`;
- `Score`, `UpdatedAt`.

### `UserBlogAffinities`

- составной ключ `UserId + BlogId`;
- `Score`, `UpdatedAt`.

### `UserSubscriptions`

- составной ключ `UserId + BlogId`;
- `CreatedAt`.

### `RecommendationImpressions`

- `RequestId`, `UserId`/`AnonymousSessionId`;
- `PostId`, `Position`;
- `AlgorithmVersion`, `CandidateSource`;
- `Score`, `ShownAt`.

### `InboxMessages`

- `EventId` — unique key;
- `EventType`, `ReceivedAt`, `ProcessedAt`;
- сведения об ошибке и количестве попыток при необходимости.

### Задачи

- [x] Реализовать доменные сущности.
- [ ] Реализовать EF-конфигурации.
- [ ] Создать первую миграцию.
- [ ] Добавить индексы по доступности поста, времени создания, блогу и категориям.
- [ ] Добавить индексы по `UserId + OccurredAt` для истории взаимодействий.
- [ ] Настроить retention для сырых interactions и impressions.
- [ ] Запретить физическое удаление snapshot до обработки delete-события и необходимого retention.

## Этап 3. Versioned integration events

Не изменять существующий `PostUpdateEvent` несовместимым способом: его используют Search, Notification и Profile. Добавить новые контракты.

### `PostCatalogChangedV2`

Обязательные поля:

- `EventId`, `AggregateVersion`, `OccurredAt`;
- `PostId`, `BlogId`, `PostType`;
- `Title`, `Description`, `CategoryIds`;
- `Visibility`, `ProcessState`;
- `IsDeleted`, `IsBanned`;
- `PaymentSubscriptionId`;
- `PreviewObjectName`, `DurationSeconds`, `CreatedAt`;
- агрегаты просмотров и реакций, если они передаются этим событием.

### `UserInteractionRecordedV1`

- `EventId`, `OccurredAt`;
- `UserId` или `AnonymousSessionId`;
- `PostId`;
- `Type`: `Impression`, `Open`, `Progress`, `Complete`, `Like`, `Dislike`;
- `WatchedSeconds`, `WatchRatio`.

### `SubscriptionChangedV1`

- `EventId`, `OccurredAt`;
- `UserId`, `BlogId`;
- `IsSubscribed`.

### Задачи publisher-ов

- [x] Blog Service публикует полный snapshot после создания и обновления поста.
- [x] Blog Service публикует ban/unban и delete как изменение snapshot.
- [x] Blog Service использует монотонный `AggregateVersion`.
- [x] Profile Service создаёт новый уникальный `EventId` для каждого взаимодействия.
- [x] Profile Service сохраняет recommendation events через outbox до публикации.
- [ ] Gateway/frontend передаёт стабильный случайный anonymous session ID вместо IP.
- [x] Добавить contract tests сериализации и маршрутизации событий.

### Задачи consumer-ов

- [x] Подписать Recommendation Service на новые routing keys отдельными durable queues.
- [x] Реализовать атомарную обработку `InboxMessage + изменение проекции`.
- [x] Повторно доставленное событие считать успешно обработанным.
- [x] Игнорировать событие поста с `AggregateVersion` ниже сохранённой.
- [ ] Настроить retry с backoff и dead-letter queue.
- [ ] Добавить метрики consumer lag, retry и DLQ.

## Этап 4. Backfill и сверка проекции

- [ ] Добавить внутренний paged endpoint/export в Blog Service для полного snapshot опубликованных постов.
- [ ] Реализовать idempotent backfill-команду в Recommendation Service.
- [ ] Сохранить watermark начала backfill, чтобы совместить snapshot с текущими событиями.
- [ ] Добавить сверку количества и контрольных сумм по статусам/типам контента.
- [ ] Предусмотреть повторный rebuild read model без остановки API.
- [ ] Документировать recovery-процедуру при потере или повреждении проекции.

## Этап 5. Candidate generation

Определить интерфейс `ICandidateSource`, возвращающий `PostId`, исходный score и причину попадания.

Реализовать источники:

- [ ] `SubscriptionCandidateSource` — свежие посты подписанных блогов.
- [ ] `CategoryAffinityCandidateSource` — посты любимых категорий.
- [ ] `CurrentPostSimilarityCandidateSource` — посты с общими категориями текущего поста.
- [ ] `TrendingCandidateSource` — популярное за 24 часа и 7 дней.
- [ ] `FreshCandidateSource` — новые публикации для exploration и cold start.

Каждый источник должен возвращать ограниченный набор, например 50–200 кандидатов. Все источники объединяются по `PostId`; причина и вклад каждого источника сохраняются для диагностики.

## Этап 6. Эвристический ranker

Начальная формула:

```text
score =
    0.30 * categoryAffinity
  + 0.20 * currentPostSimilarity
  + 0.18 * trendingWithTimeDecay
  + 0.15 * subscriptionAffinity
  + 0.10 * freshness
  + 0.07 * exploration
  - seenPenalty
  - authorRepeatPenalty
```

### Задачи

- [ ] Привести каждый feature к диапазону `0..1`.
- [ ] Вынести веса и half-life в typed configuration.
- [ ] Присвоить конфигурации `AlgorithmVersion`, например `heuristic-v1`.
- [ ] Реализовать time decay для популярности и пользовательских интересов.
- [ ] Начальные веса событий: complete `+4`, watch > 70% `+3`, like `+5`, подписка после просмотра `+8`, быстрый выход `-2`, dislike `-6`.
- [ ] Не использовать абсолютные счётчики без логарифмирования/нормализации.
- [ ] Добавить deterministic exploration, чтобы пагинация одного request оставалась стабильной.
- [ ] Сохранять feature breakdown в debug-режиме, но не отдавать его публично.

## Этап 7. Hard filters и диверсификация

Фильтры выполняются независимо от score:

- [x] Только `Public`, `Complete`, `!IsDeleted`, `!IsBanned`.
- [x] Исключить `currentPostId`.
- [x] Исключить недоступный пользователю платный контент.
- [x] Исключить недавно просмотренные посты либо применить настроенный штраф.
- [ ] Исключить заблокированные пользователем блоги, когда появится такой сигнал.
- [x] Ограничить число постов одного автора на странице.
- [ ] Не допускать длинной последовательности одной категории.
- [x] Добавить стабильный tie-breaker: `score`, затем `CreatedAt`, затем `PostId`.

## Этап 8. API и cursor pagination

Целевой контракт:

```http
GET /api/v1/feed?limit=20&cursor=...&currentPostId=...
Authorization: Bearer ...
X-Anonymous-Session-Id: ...
```

```json
{
  "requestId": "guid",
  "algorithmVersion": "heuristic-v1",
  "items": [
    {
      "postId": "guid",
      "reason": "similar_category"
    }
  ],
  "nextCursor": "opaque-value"
}
```

### Задачи

- [x] Получать текущего пользователя из доверенного JWT, а не из query string.
- [x] Поддержать anonymous session ID.
- [x] Валидировать `1 <= limit <= 100`.
- [x] Сделать cursor непрозрачным, versioned и защищённым от изменения.
- [x] Передавать `CancellationToken` во все async-операции.
- [x] Возвращать `requestId` и `algorithmVersion`.
- [ ] Добавить endpoint readiness/liveness через Service Defaults.
- [ ] Ограничить внутренний debug endpoint авторизацией.
- [x] На время миграции сохранить adapter для текущего `/recommendations`.
- [ ] Удалить `postListByIds` из Recommendation API после появления bulk hydration в Blog API.

## Этап 9. Интеграция Gateway и Blog Service

- [ ] Добавить в Blog Service bulk endpoint получения карточек по списку IDs.
- [ ] Сохранять порядок IDs из Recommendation при hydration.
- [ ] Проверять актуальную доступность постов в Blog Service.
- [ ] Не считать пропавшую карточку ошибкой всей страницы.
- [ ] Добавить timeout, retry только для безопасных запросов и circuit breaker.
- [ ] Увеличить/настроить текущий двухсекундный timeout на основании измерений.
- [ ] Передавать Authorization, correlation ID и anonymous session ID.
- [ ] Обновить OpenAPI-клиент frontend.

## Этап 10. Redis

Redis используется только как ускоритель, PostgreSQL остаётся источником состояния Recommendation Service.

- [ ] Кэшировать global trending и cold-start выдачу.
- [ ] При необходимости кэшировать первую страницу пользователя с коротким TTL.
- [ ] Включать `AlgorithmVersion` в ключи.
- [ ] Инвалидировать кэш при критичных изменениях доступности поста.
- [ ] При недоступности Redis продолжать выдачу из PostgreSQL.
- [ ] Не хранить единственную копию affinity или interactions только в Redis.

## Этап 11. Impressions и аналитика

- [ ] Записывать показ только тогда, когда карточка действительно показана клиентом, а не при генерации ответа.
- [ ] Передавать `requestId`, `postId`, `position` и `algorithmVersion`.
- [ ] Связывать последующий open/watch/like с impression, когда это возможно.
- [ ] Ввести продуктовые метрики: CTR, watch time per impression, completion rate, like rate, subscription conversion.
- [ ] Ввести guardrail-метрики: доля пустых выдач, повторов, недоступных карточек и концентрация авторов.
- [ ] Не логировать JWT, IP и персональные данные в технические логи.

## Этап 12. Наблюдаемость и эксплуатация

- [ ] Добавить structured logging с `CorrelationId`, `RequestId`, `UserIdHash`, `AlgorithmVersion`.
- [ ] Добавить OpenTelemetry spans: candidate generation, ranking, filters, DB и hydration.
- [ ] Метрики API: RPS, p50/p95/p99, ошибки и timeout.
- [ ] Метрики алгоритма: число кандидатов по источникам и причины фильтрации.
- [ ] Метрики данных: consumer lag, последнее обработанное событие, stale snapshots.
- [ ] Добавить alert на рост DLQ, пустую выдачу и отставание consumers.
- [ ] Подготовить dashboard и runbook восстановления.

## Этап 13. Тестирование

### Unit tests

- [ ] Нормализация features и расчёт score.
- [ ] Time decay и affinity updates.
- [ ] Hard filters.
- [ ] Diversification и лимит автора.
- [ ] Стабильность tie-breaker и cursor.
- [ ] Cold-start сценарии.

### Integration tests

- [ ] Consumers с реальным PostgreSQL и RabbitMQ через test containers.
- [ ] Повторная доставка одного события.
- [ ] Доставка событий не по порядку.
- [ ] Delete/ban немедленно исключает пост из выдачи.
- [ ] Redis unavailable не ломает API.
- [ ] Backfill одновременно с поступлением новых событий.
- [ ] API возвращает стабильные следующие страницы без дублей.

### Contract и end-to-end tests

- [ ] Совместимость JSON-контрактов publisher/consumer.
- [ ] Полный путь: публикация поста → событие → snapshot → recommendation → hydration.
- [ ] Полный путь персонализации: watch/like → обновление affinity → изменение порядка.
- [ ] Проверка прав доступа к приватному и платному контенту.

## Этап 14. Стратегия запуска

- [ ] Запустить consumers и выполнить backfill без переключения трафика.
- [ ] Добавить shadow mode: строить новую выдачу, но не показывать пользователю.
- [ ] Сравнить полноту, latency, фильтрацию и разнообразие со старой выдачей.
- [ ] Включить новую выдачу для небольшой доли пользователей feature flag-ом.
- [ ] Постепенно увеличить долю трафика при нормальных guardrail-метриках.
- [ ] Сохранить быстрый rollback на старый endpoint на время стабилизации.
- [ ] После полного переключения удалить прямой доступ к Blog DB и старый код.

## Порядок реализации MVP

Для первой полезной версии достаточно выполнить задачи в таком порядке:

1. Разделение проектов и собственная PostgreSQL read model.
2. `PostCatalogChangedV2`, inbox consumer и backfill.
3. `UserInteractionRecordedV1` и `SubscriptionChangedV1`.
4. Candidate sources: subscription, category, trending, fresh, current post.
5. Ranker, hard filters, diversity и cursor.
6. Новый API и bulk hydration через Gateway/Blog Service.
7. Impressions, метрики, integration tests и shadow rollout.

Redis не является блокером MVP. Embeddings, vector search и ML не входят в эту итерацию.

## Definition of Done MVP

- [ ] Recommendation Service не имеет доступа к базе Blog/Profile.
- [ ] Все необходимые данные поступают через versioned events и backfill.
- [ ] Повторные и переставленные события обрабатываются безопасно.
- [ ] Пользователь получает персонализированную выдачу по категориям, блогам, просмотрам, реакциям и подпискам.
- [ ] Анонимный пользователь получает разнообразную trending/fresh выдачу.
- [ ] Приватные, удалённые, заблокированные и неготовые посты не выдаются.
- [ ] Пагинация не создаёт дублей внутри одного recommendation request.
- [ ] Есть unit, integration и contract tests критических сценариев.
- [ ] Есть latency, consumer lag, DLQ и продуктовые метрики.
- [ ] Новая реализация включается и отключается feature flag-ом.

## Отложенные задачи

После накопления надёжных impressions и interactions:

- embeddings текста и категорий;
- `pgvector` для semantic candidate source;
- user embedding;
- offline evaluation на исторических данных;
- обучаемый ranker;
- A/B experimentation platform.

Эти задачи не должны задерживать выпуск эвристического MVP.

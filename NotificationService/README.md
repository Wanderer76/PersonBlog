# NotificationService

## Реализованные слои

`Notification.Application` содержит сценарии создания, истории, прочтения, настроек и fanout. Все порты внешних операций возвращают `Result` / `Result<T>`. Ошибки порта передаются вызывающему коду без обращения к `Value` неуспешного результата. Отмена операции сохраняет стандартную семантику `OperationCanceledException`.

`Notification.Infrastructure` реализует:

- `IRecipientDirectory`: HTTP-клиент `ProfileRecipientDirectory`, одна страница за вызов, UTC cutoff, экранирование непрозрачного cursor, проверка ответа, безопасные ошибки HTTP/JSON/timeout.
- `INotificationDelivery`: SignalR InApp и подключаемый Push через `IPushNotificationSender`. Push sender возвращает `Result`; конкретный провайдер подключается приложением.
- `IDateTimeManager`: заменяемый `TimeProvider`.
- `NotificationWorkProcessor` / `NotificationWorker`: durable inbox → прикладной сценарий → fanout → доставка, ограниченные пачки, задержка повторов и карантин после лимита попыток. Перед доставкой повторно проверяются настройки и срок действия. Отправка не отмечает уведомление прочитанным.

`Notification.Persistence` реализует `INotificationStore`, `INotificationPreferenceStore`, `IFanoutStore`, `INotificationWorkStore` через PostgreSQL/EF Core. Регистрация не открывает соединение с БД.

История хранится непосредственно доменной сущностью `UserNotification` в `Notification.Notifications`. Она содержит типизированное содержимое, источник события, бизнес-ключ и `ReadAt`; `MarkRead` сохраняет время первого прочтения. Типы содержимого и источника находятся в Domain, JSON-конвертация настроена только во Fluent-конфигурации EF. Содержимое и данные шаблона неизменяемы внутри сущности. Отдельная модель `NotificationRecord`, таблица `NotificationHistory` и legacy-модель `NotificationType`/`UserNotificationTypes` удалены. Настройки хранятся в `NotificationPreferences`, inbox/campaign/delivery jobs — в `NotificationWork`.

Сохранение уведомления, InApp job и завершение inbox выполняются в одной SERIALIZABLE-транзакции. Уникальный ключ истории: `(UserId, Kind, BusinessId)`; inbox дедуплицируется по `(Producer, EventId, ConsumerName)`, кампания — по `(PublicationId, Kind)`. Дубликат истории не создаёт новую доставку. Конкурентный конфликт возвращает `Storage.Unavailable`: вызывающий код должен повторить операцию.

Claims выбираются PostgreSQL `FOR UPDATE SKIP LOCKED`; истёкший lease доступен повторному захвату. Запись проверяет lease, а commit fanout — ещё и ожидаемый cursor. Ошибка пачки откатывает все изменения. Каждая операция использует отдельный DbContext, поэтому не сохраняет чужие изменения из DI scope.

## Подключение

API регистрирует оба слоя и фонового обработчика. До запуска нужно задать:

- `ConnectionStrings:NotificationDbContext` — PostgreSQL connection string, пароль через окружение/secret store.
- `AppUrls:NotificationProfile` — корневой URL Profile, например `http://profile:8080/`, без `/api/`.
- `AppUrls:Auth` — URL API сервера авторизации с завершающим `/api/`.
- `Redis:ConnectionString` и `Redis:InstanceName` — подключение и префикс Redis для общего кэша пользовательских сессий.
- Настройки JWT, необходимые существующему `AddCustomJwtAuthentication`.

Для другого host:

```csharp
services.AddNotificationInfrastructure(); // Включает AddNotificationApplication и SignalR
services.AddUserSessionServices(options => options.BaseUrl = configuration["AppUrls:Auth"]!);
services.AddRedisCache(configuration);
services.AddNotificationPersistence(configuration);
services.AddNotificationWorkers(options =>
    configuration.GetSection("Notification:Worker").Bind(options));
```

Worker defaults: PollInterval 2 секунды, LeaseDuration 5 минут, RetryDelay 30 секунд, MaxAttempts 10, FanoutPageSize 500. Lease должен покрывать обработку одной пачки; при истечении commit отклоняется, работа повторяется. Нет автоматического продления lease. Задания в Failed остаются в БД для диагностики; административный replay endpoint не добавлен.

`ICurrentUserService` подключается в API стандартным `Authentication.Contract.AddUserSessionServices`: существующий `HttpContextCachedUserService` использует общий кэш и HTTP-запрос `Auth/me` к серверу авторизации. Собственная реализация текущего пользователя в Notification отсутствует.

Применить начальную миграцию **до запуска worker**, явно указав соединение:

```powershell
$env:NOTIFICATION_MIGRATIONS_CONNECTION = '<connection string>'
dotnet ef database update --project NotificationService/Notification.Persistence
```

Начальная миграция пересоздана под текущую модель: сервис не в production, совместимость со старой схемой не сохраняется. Существующую локальную тестовую БД нужно пересоздать перед применением этой миграции. `IDbInitializer.Initialize()` также применяет миграции, но API автоматически его не вызывает.

## Приём событий и внешние зависимости

Точка durable intake — `INotificationWorkStore.EnqueueAsync(NotificationIngress, cancellationToken)`. Consumer подтверждает сообщение только после успешного результата enqueue. Source.EventId должен быть стабильным при повторной доставке; CorrelationId его не заменяет. Для прямого уведомления передаётся RecipientUserId, для публикации — PublicationId, BlogId, AudienceCutoff и IsPublic. Данные проверяются до сохранения.

Mapping новых событий Blog/Comments/Conference и изменения их producers в эту реализацию слоёв не входят. Старый `PostUpdateEvent` не содержит необходимых данных для надёжного mapping; legacy-подписка больше не включается API. Метод `AddLegacyPostNotifications` остаётся отдельным диагностическим адаптером, который лишь логирует получателей. Его бесконечный retry удалён; ошибки передаются шине, а не маскируются успешным завершением.

`ProfileRecipientDirectory` ожидает защищённый сервисной аутентификацией endpoint:

```text
GET /api/internal/blogs/{blogId}/subscribers?cursor=...&limit=500&cutoff=...
{ "userIds": ["guid"], "nextCursor": "opaque", "hasMore": true }
```

В текущем ProfileService этого endpoint **ещё нет**. Адаптер реализован и протестирован с HTTP stub; для рабочей рассылки публикаций нужно реализовать endpoint в Profile и настроить сервисную аутентификацию HTTP-клиента. Его контракт: активные подписки на момент чтения страницы, созданные не позднее cutoff. Авторизация настраивается стандартным typed HTTP client / DelegatingHandler для `IRecipientDirectory, ProfileRecipientDirectory`; пользовательский JWT автоматически не пересылается.

InApp доступен по `/hubs/notifications`, hub защищён `[Authorize]`. Клиент должен объединять события по NotificationId: crash после отправки до commit допускает повторную доставку. Успешный SignalR send не гарантирует получение офлайн-клиентом; история остаётся в БД. REST-контроллеры истории и frontend в эту задачу не входят.

## Проверка

```powershell
dotnet build NotificationService/Notification.API/Notification.API.csproj
dotnet test Tests/NotificationDomainTests/NotificationDomainTests.csproj
dotnet test Tests/NotificationApplicationTests/NotificationApplicationTests.csproj
$env:NOTIFICATION_TEST_CONNECTION = '<test PostgreSQL server connection>'
dotnet test Tests/NotificationIntegrationTests/NotificationIntegrationTests.csproj
```

PostgreSQL-тесты создают отдельную БД `notification_test_<guid>` на каждый тест, применяют миграции и удаляют эту БД по завершении. Учётной записи нужны права создания БД. Без переменной подключения эти тесты явно пропускаются. Проверяются дедупликация, конкурентный commit, восстановление lease, rollback пачки, cursor, настройки, snapshot/read ownership, retry/quarantine и полный worker pipeline. Остальные тесты проверяют HTTP-клиент, DI, доставку, Result и отмену без внешних сервисов.

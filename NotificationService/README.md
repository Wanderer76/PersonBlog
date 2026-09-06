# NotificationService

Каркас слоёв из пункта 4 [предложения](IMPLEMENTATION_PROPOSAL.md).

## Проекты

- `Notification.Domain`: существующая модель уведомления, без ссылок на Shared, MessageBus, EF и другие сервисы. Идентификатор и время создания в UTC передаются в `UserNotification.Create(...)`; `MarkAsViewed(changedAt)` идемпотентен и обновляет ChangedAt при первом прочтении.
- `Notification.Application`: сценарии и порты; зависит только от Domain. Каталоги Abstractions, Notifications, Fanout и Preferences подготовлены для следующих этапов.
- `Notification.Infrastructure`: адаптеры внешних систем — PostCreateEventHandler и SystemDateTimeManager.
- `Notification.Persistence`: отдельная сборка хранения — NotificationDbContext, Fluent-конфигурации, NotificationDbInitializer и NotificationPersistenceExtensions. Зависит от Application/Domain и общих библиотек; Infrastructure не ссылается на Persistence.
- `Notification.API`: composition root и будущие HTTP/SignalR endpoints. Зависит от Application, Infrastructure, Persistence и Contract.
- `Notification.Contract`: существующий внешний контракт. Legacy CreateNotificationEvent пока сохраняет MessageBus-атрибут для совместимости с обнаружением событий общей шиной. Новые DTO должны быть независимы от транспорта.

Каталоги будущих компонентов добавляются вместе с реализацией; файлы .gitkeep не используются. Будущие NotificationPreference, порты хранилища/получателей и прикладные команды вводятся вместе с реализацией соответствующих сценариев, без методов-заглушек.

## Регистрация

`AddNotificationInfrastructure()` регистрирует существующий `Shared.Services.IDateTimeManager` через `SystemDateTimeManager` и заменяемый `TimeProvider`. Текущее время получается вызовом экземпляра `UtcNow()`, поэтому реализацию можно подменить в DI. Domain продолжает получать время явно через конструктор.
`AddLegacyPostNotifications(configuration)` сохраняет существующую RabbitMQ-подписку и HTTP-клиент Blog.

Старый обработчик по-прежнему только логирует получателей; неизвестный Blog endpoint и бесконечный цикл повторов при ошибке остаются ограничениями legacy-заготовки, описанными в предложении. Надёжный Inbox, новый Profile API и рабочая рассылка в этом этапе не реализованы.

`AddNotificationPersistence(configuration)` в `Notification.Persistence/NotificationPersistenceExtensions.cs` читает `ConnectionStrings:NotificationDbContext` и использует общий `AddNpgSqlDbContext`, как Blog.Persistence. Контекст наследуется от `BaseDbContext`, используется пул контекстов, схема `Notification` и таблица истории `_Notification_MigrationsHistory`. Также регистрируются `IDbInitializer` и стандартные read/write/read-write репозитории для `INotificationEntity`. Вызов `IDbInitializer.Initialize()` применяет миграции; сама регистрация к БД не подключается. API пока метод не вызывает: миграций и сценариев хранения ещё нет. Маппинг сохраняет текущую модель, а переход к Kind/Channel/ReadAt выполняется на этапе модели данных.

API пока сохраняет прежний маршрут-заглушку weatherforecast. Авторизация, Hub и REST истории появятся с соответствующим вертикальным срезом.

## Проверка

```powershell
dotnet build NotificationService/Notification.API/Notification.API.csproj
dotnet test Tests/NotificationDomainTests/NotificationDomainTests.csproj
dotnet test Tests/NotificationApplicationTests/NotificationApplicationTests.csproj
dotnet test Tests/NotificationIntegrationTests/NotificationIntegrationTests.csproj
```

Тесты проверяют поведение доменной модели, направления ссылок Core-проектов, DI и реляционный EF-маппинг. На этом этапе IntegrationTests не подключаются к PostgreSQL/RabbitMQ и не проверяют доставку сообщений.

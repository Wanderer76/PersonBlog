# Gateway.API

`Gateway.API` — Backend for Frontend (BFF), который агрегирует данные и проксирует запросы к Auth, Blog, Profile, Recommendation, Search, Conference, Comments и PlayList API.

Несмотря на название, проект сейчас не использует Ocelot: маршруты реализованы контроллерами и HTTP-клиентами. Документ фиксирует проблемы, обнаруженные при аудите текущей реализации.

## Текущее состояние

- Целевая платформа: .NET 8.
- Сборка проекта проходит без ошибок.
- Отдельного тестового проекта для `Gateway.API` нет.
- При сборке выводится большое количество предупреждений из Gateway и транзитивно подключённых проектов.
- Конфигурация Release зависит от внешнего `appsettings.json`, которого нет в репозитории.

## P1 — высокий приоритет

### 1. Произвольное чтение объектов из файлового хранилища

Endpoint `GET /Video/{blogId}/{postId}/{*file}` принимает идентификатор bucket и полный object key от клиента. `postId` не используется, принадлежность файла публикации не проверяется, проверка видимости публикации отсутствует.

Последствия:

- IDOR и доступ к объектам другой публикации;
- обход правил доступа к приватному или заблокированному контенту;
- возможность перебора предсказуемых ключей объектов.

Место: `Controllers/VideoController.cs`, метод `GetVideoSegmentsOrManifest`.

Рекомендации:

- принимать идентификатор медиа, а не произвольный object key;
- получать object key из доверенных метаданных Blog API;
- проверять связь `blogId → postId → file`;
- проверять видимость публикации и права пользователя;
- запретить `..`, абсолютные пути и неожиданные форматы ключей.

### 2. Неполная production-конфигурация

При старте вызываются `AddPlayListContract` и `AddBlogContract`, которым нужны:

- `AppUrls:PlayList`;
- `AppUrls:Blog`.

В `appsettings.json` этих ключей нет. Без внешней конфигурации создание `Uri` завершается исключением ещё при запуске приложения.

Дополнительно название `AppUrls:Profile` вводит в заблуждение: клиент с этим именем фактически вызывает Blog/Post endpoints, а Profile API задан как `Reacting`.

Рекомендации:

- добавить все обязательные URL в конфигурацию;
- использовать типизированные options с `ValidateOnStart`;
- переименовать URL и клиентов по реальным downstream-сервисам;
- добавить smoke-test запуска для Development и Release.

### 3. `ProfileHttpClient` не передаёт bearer token

Gateway проверяет пользователя перед `GET /api/Profile/my`, но `AddProfileHttpClient` не подключает `HeaderClientHandler`. Downstream endpoint `Profile/profile/my` защищён `[Authorize]`, поэтому запрос Gateway уходит без токена и получает 401.

Место: `ProfileService/Profile.Service/VideoReactingServiceExtensions.cs`.

Рекомендация: подключить `HeaderClientHandler` к typed client и добавить интеграционный тест передачи Authorization и correlation ID.

### 4. Subscribe и unsubscribe скрывают ошибки downstream

Методы `SubscribeToBlog` и `UnSubscribeToBlog` игнорируют `HttpResponseMessage`. `PostAsync` не выбрасывает исключения для HTTP 400/401/403/500, поэтому Gateway отвечает `200 OK` даже при отказе Profile API.

Место: `Controllers/SubscriberController.cs`.

Рекомендации:

- проверять `IsSuccessStatusCode`;
- сохранять исходный статус и тело ответа;
- не преобразовывать downstream 401/403/404/409/500 в 200 или общий 400.

### 5. Нет единой модели авторизации Gateway

В проекте одновременно используются `[Authorize]` и собственный `[AuthFilter]`. Некоторые действия имеют только `AuthFilter`, некоторые только `Authorize`, а часть endpoints, использующих текущего пользователя, не имеет ни одного явного атрибута.

Примеры:

- `BlogController.GetBlogDetail` использует текущего пользователя без явной защиты Gateway;
- `BlogController.CreateBlog` полагается на авторизацию downstream;
- `BlogController.GetBlogViewerInfoByPostId` заявлен как защищённый в документации, но не имеет явного атрибута;
- `VideoController.SetReactionToVideo` допускает анонимный доступ, хотя поведение зависит от IP и токена.

Рекомендации:

- перейти на стандартные authorization policies;
- выразить роли через policies/claims;
- удалить дублирующую JWT-проверку после миграции;
- не полагаться исключительно на защиту downstream-сервиса.

### 6. Секреты и слабые credentials находятся в конфигурации

В `appsettings.json` хранятся стандартные ключи MinIO и учётные данные RabbitMQ вида `admin/admin`. Даже если они предназначены для локальной среды, файл используется как базовая Release-конфигурация.

Рекомендации:

- оставить в репозитории только безопасные placeholders;
- передавать секреты через environment variables, user secrets или secret manager;
- разделить локальную и production-конфигурацию;
- сменить credentials во всех уже развёрнутых окружениях.

## P2 — средний приоритет

### 7. HLS-сегменты полностью буферизуются в памяти

`VideoController` сначала копирует весь объект MinIO в `MemoryStream` и только затем начинает HTTP-ответ. При параллельных запросах это создаёт значительную RAM-нагрузку и давление на GC.

Рекомендации:

- передавать поток напрямую в response;
- поддержать Range requests;
- либо возвращать redirect на короткоживущий presigned URL;
- корректно обрабатывать отмену запроса.

### 8. Неверный MIME-тип видеосегментов

Для `.ts` и других медиафайлов возвращается `application/x-mpegURL`, предназначенный для HLS playlist. Для MPEG-TS должен использоваться `video/mp2t`, а для `.m3u8` — `application/vnd.apple.mpegurl`.

Последствия: ошибки воспроизведения, кэширования и проверки контента в браузерах/CDN.

### 9. Утечка `IFileStorage` и DI scope

`VideoController` создаёт хранилище через `IFileStorageFactory.CreateFileStorage()`. Фабрика создаёт скрытый scope, а контроллер не освобождает полученный `IFileStorage`.

Рекомендация: инжектировать scoped `IFileStorage` напрямую и позволить ASP.NET Core управлять его временем жизни.

### 10. HTTP-статусы downstream искажаются

Несколько контроллеров не сохраняют исходную семантику ответа:

- `CommentsController` всегда возвращает 200;
- `SearchController` возвращает 200 независимо от ответа Recommendation API;
- `ConferenceRoomController` и `ConferenceChatController` преобразуют разные ошибки в общий 400;
- `BanController` и `VideoController` возвращают `BadRequest(result.Content)` вместо тела ответа;
- сетевые ошибки часто становятся 500 без контролируемого `ProblemDetails`.

Рекомендация: централизовать преобразование `HttpResponseMessage` в ответ Gateway.

### 11. JSON возвращается как строка

`CommentsController` и `SearchController` читают JSON через `ReadAsStringAsync()`, а затем передают строку в `Ok(...)`. ASP.NET Core сериализует её повторно, поэтому клиент может получить JSON внутри JSON-строки.

Рекомендации:

- десериализовать контракт в конкретную модель;
- либо копировать response stream с исходным `Content-Type`.

### 12. Клиенту возвращаются внутренние исключения

`VideoController.GetPostData` возвращает `BadRequest(ex)`. Это может раскрыть тип исключения, внутренние сообщения и детали реализации. Downstream failure при этом ошибочно становится клиентским HTTP 400.

Рекомендации:

- добавить глобальный exception handler;
- использовать стандартный `ProblemDetails`;
- логировать исключение через `ILogger`;
- возвращать 502 для downstream failure, 504 для timeout и 500 для внутренней ошибки.

### 13. Возможны `NullReferenceException` при пустом downstream-ответе

Небезопасные места:

- `SearchController`: вызов `searchResult.Any()` без проверки `null`;
- `SubscriberController`: обращение к `subscriptions.Items` без проверки результата;
- `PostApiService.GetUserViewInfoAsync`: обращение к `result.PostId` до проверки `result`;
- `ChannelController`: null-forgiving для Blog и subscription response;
- `BlogApiClient.HandleResponseAsync`: успешный ответ с пустым body превращается в success с `null`.

Рекомендация: валидировать status code и body каждого downstream-ответа до использования.

### 14. Нет передачи `CancellationToken`

Контроллеры и API-клиенты почти нигде не передают `HttpContext.RequestAborted`. После отключения клиента Gateway продолжает выполнять downstream-запросы и агрегировать результат.

Рекомендация: добавить `CancellationToken` во все async endpoints и передавать его в `HttpClient`, кэш и файловое хранилище.

### 15. Один таймаут в две секунды для всех запросов

Auth, Blog, Profile, Recommendation, Search, Conference и Comments получают одинаковый timeout в две секунды. Для агрегации, поиска и холодного старта этого может быть недостаточно. Resilience policies отсутствуют.

Рекомендации:

- определить timeout по типу операции;
- добавить retries только для безопасных идемпотентных запросов;
- добавить circuit breaker и ограничение параллелизма;
- преобразовывать timeout в HTTP 504.

### 16. Неверная работа с forwarded headers за Docker/Nginx

`UseForwardedHeaders` включён без настройки доверенных proxy/network. При работе через отдельный контейнер Nginx его адрес может не считаться доверенным, и `RemoteIpAddress` останется адресом proxy.

Это особенно важно, потому что IP используется для анонимных реакций. Разные пользователи могут восприниматься системой как один клиент.

Рекомендации:

- настроить `KnownProxies` или `KnownNetworks`;
- применять forwarded headers до логики, использующей scheme/IP;
- не использовать IP как единственный идентификатор пользователя.

### 17. Обработка HLS manifest недостаточно надёжна

`ProcessHLSManifestAsync`:

- загружает manifest целиком в память;
- обрабатывает URL последовательно;
- распознаёт только строки, буквально заканчивающиеся `.m3u8` или `.ts`;
- может не обработать CRLF, query string и URI внутри HLS attributes;
- использует null-forgiving для `Path.GetDirectoryName`;
- не ограничивает размер и число записей manifest.

Рекомендация: использовать HLS parser либо строгий разбор URI-строк с нормализацией пути.

### 18. N+1-запросы при получении подписок

`SubscriberController.SubscriptionsList` выполняет отдельный запрос Blog API для каждой подписки. Запросы отправляются параллельно, но их количество не ограничено.

Рекомендации:

- добавить batch endpoint `blogsByIds`;
- ограничить допустимый `pageSize`;
- обрабатывать частичные ошибки вместо падения всей агрегации.

### 19. Нет ограничений параметров пагинации

Параметры `page`, `size`, `limit`, `offset` и `count` передаются downstream без проверки диапазона. Клиент может запросить отрицательные значения или чрезмерно большой объём данных.

Рекомендация: добавить модели запросов с `[Range]` и общий максимальный page size.

### 20. CORS разрешён для любого origin

Используется `AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()` во всех окружениях. Для публичного API это может быть допустимо только как осознанная политика, но сейчас список доверенных frontend origins не задаётся конфигурацией.

Рекомендация: завести именованную CORS policy и ограничить production origins.

### 21. Swagger включён во всех окружениях

Условие Development закомментировано, поэтому Swagger публикуется и в Release.

Рекомендация: включать Swagger только в Development или защищать его отдельной policy.

## P3 — качество и сопровождение

### 22. Gateway зависит от внутренних слоёв микросервисов

Проект напрямую ссылается на:

- `Authentication.Service`;
- `Blog.Service`;
- `Profile.Service` и `Profile.Domain`;
- `PlayListService.Services`;
- Domain-проекты Comments и Conference.

Это связывает сборку Gateway с реализацией микросервисов и приводит к сотням транзитивных предупреждений.

Рекомендации:

- оставить зависимости только на Contract-проекты;
- вынести BFF DTO в собственный слой;
- генерировать клиентов из OpenAPI;
- не использовать domain entities как внешний API-контракт.

### 23. Несогласованные способы вызова downstream

Одновременно используются:

- named `HttpClient`;
- typed clients;
- статические extension-методы над `IHttpClientFactory`;
- contract clients;
- ручная передача заголовков;
- `HeaderClientHandler`.

Рекомендация: выбрать единый typed-client подход с общей обработкой заголовков, ошибок, cancellation и observability.

### 24. Несогласованные маршруты Gateway

Большинство контроллеров наследует `/api/[controller]`, но:

- `VideoController` использует корневой `/Video`;
- recommendations опубликованы как абсолютный `/recommendations`;
- health опубликован как абсолютный `/health`.

Это усложняет frontend-конфигурацию и reverse-proxy rules.

### 25. Мёртвый и неиспользуемый код

Примеры:

- `AuthController.cs` целиком закомментирован;
- `_storage` в `ViewController` не инициализируется и не используется;
- `_cache` и `fromPlaylist` в `VideoController` не используются;
- `AuthApiService` не используется активным контроллером;
- вспомогательные методы загрузки содержат устаревшие модели;
- в контроллерах остались крупные закомментированные блоки.

Рекомендация: удалить мёртвый код после проверки истории Git.

### 26. Нет тестов Gateway

Не найдены unit или integration тесты для:

- передачи Authorization/correlation ID;
- сохранения downstream status codes;
- обработки timeout и недоступного сервиса;
- агрегации частичных результатов;
- доступа к видео и проверки object key;
- анонимных и авторизованных сценариев;
- корректности CORS и forwarded headers.

Минимальный набор следует реализовать через `WebApplicationFactory`, mock HTTP handlers и тестовое файловое хранилище.

### 27. Нет обязательного health-check downstream-сервисов

`MapDefaultEndpoints` публикует базовые endpoints текущего процесса, но Gateway не проверяет готовность Auth, Blog, Profile, Redis, MinIO и других обязательных зависимостей.

Рекомендация: разделить liveness и readiness и включить проверки критических downstream-компонентов.

### 28. Логирование и correlation применяются непоследовательно

Часть ошибок выводится через `Console.WriteLine`, часть проглатывается, часть возвращается клиенту. Correlation ID передаётся только клиентами с `HeaderClientHandler`.

Рекомендация: использовать структурированный `ILogger`, единый exception handler и автоматически добавлять correlation ID ко всем исходящим запросам.

## Рекомендуемый порядок исправления

1. Закрыть IDOR в video endpoint и перестать принимать object key от клиента.
2. Исправить Release-конфигурацию и добавить `ValidateOnStart`.
3. Унифицировать авторизацию и передачу bearer token.
4. Начать сохранять downstream status codes и перейти на `ProblemDetails`.
5. Перевести выдачу медиа на streaming/presigned URLs и исправить lifetime `IFileStorage`.
6. Добавить cancellation, resilience policies и ограничения пагинации.
7. Добавить интеграционные тесты ключевых BFF-сценариев.
8. Разорвать ссылки на Service/Domain-проекты и оставить только API contracts.
9. Очистить маршруты, конфигурацию и мёртвый код.

## Проверка после исправлений

Рекомендуемый минимальный pipeline:

```bash
dotnet restore GetewayService/Gateway.API/Gateway.API.csproj
dotnet build GetewayService/Gateway.API/Gateway.API.csproj --no-restore
dotnet test <Gateway.API.Tests.csproj> --no-build
docker compose config --quiet
```

Дополнительно следует выполнять интеграционный smoke-test Gateway со всеми обязательными downstream-сервисами.

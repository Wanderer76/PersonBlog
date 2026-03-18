# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для создания образов для ускорения отладки.

# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080


# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GetewayService/Gateway.API/Gateway.API.csproj", "GetewayService/Gateway.API/"]
COPY ["AuthService/Authentication.Contract/Authentication.Contract.csproj", "AuthService/Authentication.Contract/"]
COPY ["Common/MessageBus/MessageBus.csproj", "Common/MessageBus/"]
COPY ["Common/Infrastructure/Infrastructure.csproj", "Common/Infrastructure/"]
COPY ["Common/Shared/Shared.csproj", "Common/Shared/"]
COPY ["Common/MessageBus.Shared/MessageBus.Shared.csproj", "Common/MessageBus.Shared/"]
COPY ["AuthService/Authentication.Service/Authentication.Service.csproj", "AuthService/Authentication.Service/"]
COPY ["BlogService/Blog.Contracts/Blog.Contracts.csproj", "BlogService/Blog.Contracts/"]
COPY ["BlogService/Blog.Domain/Blog.Domain.csproj", "BlogService/Blog.Domain/"]
COPY ["Common/FileStorage.Service/FileStorage.Service.csproj", "Common/FileStorage.Service/"]
COPY ["NotificationService/Notification.Contract/Notification.Contract.csproj", "NotificationService/Notification.Contract/"]
COPY ["ProfileService/Profile.Domain/Profile.Domain.csproj", "ProfileService/Profile.Domain/"]
COPY ["AuthService/Authentication.Domain/Authentication.Domain.csproj", "AuthService/Authentication.Domain/"]
COPY ["AuthService/Authentication.Peristence/Authentication.Peristence.csproj", "AuthService/Authentication.Peristence/"]
COPY ["BlogService/Blog.Service/Blog.Service.csproj", "BlogService/Blog.Service/"]
COPY ["BlogService/Blog.Persistence/Blog.Persistence.csproj", "BlogService/Blog.Persistence/"]
COPY ["CommentService/Comments.Domain/Comments.Domain.csproj", "CommentService/Comments.Domain/"]
COPY ["ConferenceService/Conference.Domain/Conference.Domain.csproj", "ConferenceService/Conference.Domain/"]
COPY ["PlayListService/PlayListService.Contract/PlayListService.Contract.csproj", "PlayListService/PlayListService.Contract/"]
COPY ["PlayListService/PlayListService.Services/PlayListService.Services.csproj", "PlayListService/PlayListService.Services/"]
COPY ["PlayListService/PlayListService.Domain/PlayListService.Domain.csproj", "PlayListService/PlayListService.Domain/"]
COPY ["ProfileService/Profile.Service/Profile.Service.csproj", "ProfileService/Profile.Service/"]
COPY ["ProfileService/Profile.Persistence/Profile.Persistence.csproj", "ProfileService/Profile.Persistence/"]
COPY ["RecommendationService/Recommendation.Service/Recommendation.Service.csproj", "RecommendationService/Recommendation.Service/"]
COPY ["GetewayService/AspireTest/AspireTest.ServiceDefaults/AspireTest.ServiceDefaults.csproj", "GetewayService/AspireTest/AspireTest.ServiceDefaults/"]
RUN dotnet restore "./GetewayService/Gateway.API/Gateway.API.csproj"
COPY . .
WORKDIR "/src/GetewayService/Gateway.API"
RUN dotnet build "./Gateway.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Gateway.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gateway.API.dll"]
# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для создания образов для ускорения отладки.

# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RecommendationService/Recommendation.Application/Recommendation.Application.csproj", "RecommendationService/Recommendation.Application/"]
COPY ["BlogService/Blog.Persistence/Blog.Persistence.csproj", "BlogService/Blog.Persistence/"]
COPY ["Common/Infrastructure/Infrastructure.csproj", "Common/Infrastructure/"]
COPY ["Common/Shared/Shared.csproj", "Common/Shared/"]
COPY ["BlogService/Blog.Domain/Blog.Domain.csproj", "BlogService/Blog.Domain/"]
COPY ["Common/FileStorage.Service/FileStorage.Service.csproj", "Common/FileStorage.Service/"]
COPY ["Common/MessageBus/MessageBus.csproj", "Common/MessageBus/"]
COPY ["Common/MessageBus.Shared/MessageBus.Shared.csproj", "Common/MessageBus.Shared/"]
COPY ["NotificationService/Notification.Contract/Notification.Contract.csproj", "NotificationService/Notification.Contract/"]
COPY ["ProfileService/Profile.Domain/Profile.Domain.csproj", "ProfileService/Profile.Domain/"]
COPY ["AuthService/Authentication.Contract/Authentication.Contract.csproj", "AuthService/Authentication.Contract/"]
COPY ["GetewayService/AspireTest/AspireTest.ServiceDefaults/AspireTest.ServiceDefaults.csproj", "GetewayService/AspireTest/AspireTest.ServiceDefaults/"]
COPY ["RecommendationService/Recommendation.Service/Recommendation.Service.csproj", "RecommendationService/Recommendation.Service/"]
RUN dotnet restore "./RecommendationService/Recommendation.Application/Recommendation.Application.csproj"
COPY . .
WORKDIR "/src/RecommendationService/Recommendation.Application"
RUN dotnet build "./Recommendation.Application.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Recommendation.Application.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Recommendation.Application.dll"]
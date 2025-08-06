# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для создания образов для ускорения отладки.

# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080


# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ConferenceService/Conference.API/Conference.API.csproj", "ConferenceService/Conference.API/"]
COPY ["ConferenceService/Conference.Domain/Conference.Domain.csproj", "ConferenceService/Conference.Domain/"]
COPY ["Common/Shared/Shared.csproj", "Common/Shared/"]
COPY ["ConferenceService/Conference.Persistence/Conference.Persistence.csproj", "ConferenceService/Conference.Persistence/"]
COPY ["Common/Infrastructure/Infrastructure.csproj", "Common/Infrastructure/"]
COPY ["ConferenceService/Conference.Service/Conference.Service.csproj", "ConferenceService/Conference.Service/"]
RUN dotnet restore "./ConferenceService/Conference.API/Conference.API.csproj"
COPY . .
WORKDIR "/src/ConferenceService/Conference.API"
RUN dotnet build "./Conference.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Conference.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Conference.API.dll"]
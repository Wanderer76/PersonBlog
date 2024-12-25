# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для создания образов для ускорения отладки.

# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y ffmpeg 
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["../Shared/Shared.csproj", "Shared/"]
COPY ["../FileStorage.Service/FileStorage.Service.csproj", "FileStorage.Service/"]
COPY ["../Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["../Profile.Domain/Profile.Domain.csproj", "Profile.Domain/"]
COPY ["../Profile.Persistence/Profile.Persistence.csproj", "Profile.Persistence/"]
COPY ["../Profile.Service/Profile.Service.csproj", "Profile.Service/"]
COPY ["../ProfileApplication/ProfileApplication.csproj", "ProfileApplication/"]
RUN dotnet restore "./ProfileApplication/ProfileApplication.csproj"
COPY . .
WORKDIR "/src/ProfileApplication"
RUN dotnet build "./ProfileApplication.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ProfileApplication.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProfileApplication.dll"]
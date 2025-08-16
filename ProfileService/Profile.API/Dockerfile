# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для создания образов для ускорения отладки.

# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080


# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ViewReactingService/VideoReacting.API/VideoReacting.API.csproj", "ViewReactingService/VideoReacting.API/"]
COPY ["ViewReactingService/VideoReacting.Persistence/VideoReacting.Persistence.csproj", "ViewReactingService/VideoReacting.Persistence/"]
COPY ["ViewReactingService/ViewReacting.Domain/ViewReacting.Domain.csproj", "ViewReactingService/ViewReacting.Domain/"]
COPY ["BlogService/Blog.Contracts/Blog.Contracts.csproj", "BlogService/Blog.Contracts/"]
COPY ["Common/MessageBus/MessageBus.csproj", "Common/MessageBus/"]
COPY ["Common/Infrastructure/Infrastructure.csproj", "Common/Infrastructure/"]
COPY ["Common/Shared/Shared.csproj", "Common/Shared/"]
COPY ["Common/MessageBus.Shared/MessageBus.Shared.csproj", "Common/MessageBus.Shared/"]
COPY ["ViewReactingService/VideoReacting.Service/VideoReacting.Service.csproj", "ViewReactingService/VideoReacting.Service/"]
COPY ["BlogService/Blog.Service/Blog.Service.csproj", "BlogService/Blog.Service/"]
COPY ["BlogService/Blog.Domain/Blog.Domain.csproj", "BlogService/Blog.Domain/"]
COPY ["Common/FileStorage.Service/FileStorage.Service.csproj", "Common/FileStorage.Service/"]
COPY ["BlogService/Blog.Persistence/Blog.Persistence.csproj", "BlogService/Blog.Persistence/"]
RUN dotnet restore "./ViewReactingService/VideoReacting.API/VideoReacting.API.csproj"
COPY . .
WORKDIR "/src/ViewReactingService/VideoReacting.API"
RUN dotnet build "./VideoReacting.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./VideoReacting.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VideoReacting.API.dll"]
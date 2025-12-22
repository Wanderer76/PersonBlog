# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для создания образов для ускорения отладки.

# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080


RUN apt-get update && apt-get install -y nano

# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["VideoService/VideoView.Application/VideoView.Application.csproj", "VideoService/VideoView.Application/"]
COPY ["AuthService/Authentication.Service/Authentication.Service.csproj", "AuthService/Authentication.Service/"]
COPY ["BlogService/Blog.Contracts/Blog.Contracts.csproj", "BlogService/Blog.Contracts/"]
COPY ["Common/MessageBus/MessageBus.csproj", "Common/MessageBus/"]
COPY ["Common/Infrastructure/Infrastructure.csproj", "Common/Infrastructure/"]
COPY ["Common/Shared/Shared.csproj", "Common/Shared/"]
COPY ["Common/MessageBus.Shared/MessageBus.Shared.csproj", "Common/MessageBus.Shared/"]
COPY ["AuthService/Authentication.Contract/Authentication.Contract.csproj", "AuthService/Authentication.Contract/"]
COPY ["AuthService/Authentication.Domain/Authentication.Domain.csproj", "AuthService/Authentication.Domain/"]
COPY ["AuthService/Authentication.Peristence/Authentication.Peristence.csproj", "AuthService/Authentication.Peristence/"]
COPY ["BlogService/Blog.Service/Blog.Service.csproj", "BlogService/Blog.Service/"]
COPY ["BlogService/Blog.Domain/Blog.Domain.csproj", "BlogService/Blog.Domain/"]
COPY ["Common/FileStorage.Service/FileStorage.Service.csproj", "Common/FileStorage.Service/"]
COPY ["BlogService/Blog.Persistence/Blog.Persistence.csproj", "BlogService/Blog.Persistence/"]
COPY ["CommentService/Comments.Domain/Comments.Domain.csproj", "CommentService/Comments.Domain/"]
COPY ["ConferenceService/Conference.Domain/Conference.Domain.csproj", "ConferenceService/Conference.Domain/"]
COPY ["RecommendationService/Recommendation.Service/Recommendation.Service.csproj", "RecommendationService/Recommendation.Service/"]
COPY ["ViewReactingService/ViewReacting.Domain/ViewReacting.Domain.csproj", "ViewReactingService/ViewReacting.Domain/"]
RUN dotnet restore "./VideoService/VideoView.Application/VideoView.Application.csproj"
COPY . .
WORKDIR "/src/VideoService/VideoView.Application"
RUN dotnet build "./VideoView.Application.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./VideoView.Application.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VideoView.Application.dll"]
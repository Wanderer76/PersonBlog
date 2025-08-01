# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для создания образов для ускорения отладки.

# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

RUN apt-get update && apt-get install -y xz-utils nano

# Добавляем и распаковываем FFmpeg
ENV FFMPEG_DIR=/app/ffmpeg
COPY ./ffmpeg-master-latest-linux64-lgpl.tar.xz .
RUN mkdir -p $FFMPEG_DIR && \
    tar -xf ffmpeg-master-latest-linux64-lgpl.tar.xz -C $FFMPEG_DIR && \
    rm ffmpeg-master-latest-linux64-lgpl.tar.xz && \
    chmod +x $FFMPEG_DIR/ffmpeg-master-latest-linux64-lgpl/bin/ffmpeg && \
    chmod +x $FFMPEG_DIR/ffmpeg-master-latest-linux64-lgpl/bin/ffprobe && \
    ln -s $FFMPEG_DIR/ffmpeg-master-latest-linux64-lgpl/bin/ffmpeg /usr/local/bin/ffmpeg && \
    ln -s $FFMPEG_DIR/ffmpeg-master-latest-linux64-lgpl/bin/ffprobe /usr/local/bin/ffprobe

# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BlogService/VideoProcessing.Cli/VideoProcessing.Cli.csproj", "BlogService/VideoProcessing.Cli/"]
COPY ["Common/FFmpeg.Service/FFmpeg.Service.csproj", "Common/FFmpeg.Service/"]
COPY ["Common/FileStorage.Service/FileStorage.Service.csproj", "Common/FileStorage.Service/"]
COPY ["Common/Infrastructure/Infrastructure.csproj", "Common/Infrastructure/"]
COPY ["Common/Shared/Shared.csproj", "Common/Shared/"]
COPY ["Common/MessageBus/MessageBus.csproj", "Common/MessageBus/"]
COPY ["Common/MessageBus.Shared/MessageBus.Shared.csproj", "Common/MessageBus.Shared/"]
COPY ["BlogService/Blog.Domain/Blog.Domain.csproj", "BlogService/Blog.Domain/"]
COPY ["BlogService/Blog.Persistence/Blog.Persistence.csproj", "BlogService/Blog.Persistence/"]
RUN dotnet restore "./BlogService/VideoProcessing.Cli/VideoProcessing.Cli.csproj"
COPY . .
WORKDIR "/src/BlogService/VideoProcessing.Cli"
RUN dotnet build "./VideoProcessing.Cli.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./VideoProcessing.Cli.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VideoProcessing.Cli.dll"]
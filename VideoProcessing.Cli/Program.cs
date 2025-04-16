using Blog.Domain.Events;
using Blog.Persistence;
using FFmpeg.Service;
using FileStorage.Service;
using Infrastructure.Extensions;
using MessageBus;
using MessageBus.Shared.Configs;
using VideoProcessing.Cli;
using VideoProcessing.Cli.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProfilePersistence(builder.Configuration);
builder.Services.AddFileStorage();
builder.Services.AddHostedService<VideoConverterHostedService>();
builder.Services.AddFFMpeg(builder.Configuration);
builder.Services.AddRedisCache(builder.Configuration);

builder.Services.AddMessageBus(builder.Configuration)
    .AddSubscription<CombineFileChunksEvent, VideoChunksCombinerService>()
    .AddSubscription<VideoConvertEvent, ProcessVideoToHls>()
    .AddConnectionConfig(builder.Configuration.GetSection("RabbitMq:UploadVideoConfig").Get<RabbitMqUploadVideoConfig>()!);


var app = builder.Build();
app.Run();

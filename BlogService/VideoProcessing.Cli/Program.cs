using Blog.Domain.Events;
using FFmpeg.Service;
using FileStorage.Service;
using Infrastructure.Extensions;
using MassTransit;
using MessageBus;
using MessageBus.Configs;
using MessageBus.Shared.Configs;
using VideoProcessing.Cli;
using VideoProcessing.Cli.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFileStorage(builder.Configuration);
builder.Services.AddFFMpeg(builder.Configuration);
builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddMessageBus(builder.Configuration)
    .AddSubscription<CombineFileChunksCommand, VideoChunksCombinerService>()
    .AddSubscription<ConvertVideoCommand, ProcessVideoToHls>()
    .AddConnectionConfig(builder.Configuration.GetSection("RabbitMq:UploadVideoConfig").Get<RabbitMqUploadVideoConfig>()!);
builder.Services.AddHostedService<VideoConverterHostedService>();

//builder.Services.AddMassTransit(x =>
//{
//    x.AddConsumer<VideoChunksCombinerService>();
//    x.AddConsumer<ProcessVideoToHls>();

//    x.UsingRabbitMq((context, cfg) =>
//    {
//        var rabbit = builder.Configuration.GetSection("RabbitMQ:Connection").Get<RabbitMqConnection>()!;
//        cfg.Host($"rabbitmq://{rabbit.HostName}:{rabbit.Port}", h =>
//        {
//            h.Username(rabbit.UserName);
//            h.Password(rabbit.Password);
//        });

//        cfg.ReceiveEndpoint("chunk-combines", e =>
//        {
//            e.Durable = true;
//            e.Exclusive = false;
//            e.AutoDelete = false;

//            e.ConfigureConsumer<VideoChunksCombinerService>(context);
//        });
//        cfg.ReceiveEndpoint("video-processing", e =>
//        {
//            e.Durable = true;
//            e.Exclusive = false;
//            e.AutoDelete = false;
//            e.ConfigureConsumer<ProcessVideoToHls>(context);
//        });

//        cfg.ConfigureEndpoints(context);

//    });
//});

var app = builder.Build();
app.Run();

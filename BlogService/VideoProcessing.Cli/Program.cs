using Blog.Domain.Events;
using Blog.Persistence;
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

builder.Services.AddProfilePersistence(builder.Configuration);
builder.Services.AddFileStorage(builder.Configuration);
//builder.Services.AddHostedService<VideoConverterHostedService>();
builder.Services.AddFFMpeg(builder.Configuration);
builder.Services.AddRedisCache(builder.Configuration);

builder.Services.AddMessageBus(builder.Configuration);
//    .AddSubscription<CombineFileChunksCommand, VideoChunksCombinerService>()
//    .AddSubscription<VideoConvertEvent, ProcessVideoToHls>()
//    .AddConnectionConfig(builder.Configuration.GetSection("RabbitMq:UploadVideoConfig").Get<RabbitMqUploadVideoConfig>()!);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<VideoChunksCombinerService>();
    x.AddConsumer<ProcessVideoToHls>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbit = builder.Configuration.GetSection("RabbitMQ:Connection").Get<RabbitMqConnection>()!;
        cfg.Host($"rabbitmq://{rabbit.HostName}:{rabbit.Port}", h =>
        {
            h.Username(rabbit.UserName);
            h.Password(rabbit.Password);
        });
        // Очередь для обработки видео

        cfg.ReceiveEndpoint("chunk-combines", e =>
        {
            e.Durable = true;
            e.Exclusive = false;
            e.AutoDelete = false;
            e.Bind("video-events", s =>
            {
                s.RoutingKey = "chunks.combine";
                s.ExchangeType = "direct";
                s.AutoDelete = false;
                s.Durable = true;
            });
           
            e.ConfigureConsumer<VideoChunksCombinerService>(context);
        });
        cfg.ReceiveEndpoint("video-processing", e =>
        {
            e.Durable = true;
            e.Exclusive = false;
            e.AutoDelete = false;

            e.Bind("video-events", s =>
            {
                s.RoutingKey = "video.upload";
                s.ExchangeType = "direct";
                s.AutoDelete = false;
                s.Durable = true;
            });

            e.ConfigureConsumer<ProcessVideoToHls>(context);
        });
        cfg.ReceiveEndpoint("video-publish", e =>
        {
            e.ConfigureConsumeTopology = false;
            e.Durable = true;
            e.Exclusive = false;
            e.AutoDelete = false;

            e.Bind("video-events", s =>
            {
                s.RoutingKey = "video.publish";
                s.ExchangeType = "direct";
                s.AutoDelete = false;
                s.Durable = true;
            });
        });

        cfg.ReceiveEndpoint("video-processing-response", e =>
        {
            e.Durable = true;
            e.Exclusive = false;
            e.AutoDelete = false;
            e.Bind("video-events", s =>
            {
                s.RoutingKey = "response";
                s.ExchangeType = "direct";
                s.AutoDelete = false;
                s.Durable = true;
            });
        });

        cfg.Publish<ChunksCombinedResponse>(p =>
        {
            p.ExchangeType = "direct";
            p.Durable = true;
            p.AutoDelete = false;
            p.BindQueue("video-events", "video-processing-response", c => { c.RoutingKey = "response"; c.AutoDelete = false; c.Durable = true; c.ExchangeType = "direct"; });
        });
        cfg.Publish<VideoConvertedResponse>(p =>
        {
            p.ExchangeType = "direct";
            p.Durable = true;
            p.AutoDelete = false;
            p.BindQueue("video-events", "video-processing-response", c => { c.RoutingKey = "response"; c.AutoDelete = false; c.Durable = true; c.ExchangeType = "direct"; });
        });

        cfg.Publish<CombineFileChunksCommand>(p =>
        {
            p.ExchangeType = "direct";
            p.Durable = true;
            p.AutoDelete = false;
            p.BindQueue("video-events", "chunk-combines", c => { c.RoutingKey = "chunks.combine"; c.AutoDelete = false; c.Durable = true; c.ExchangeType = "direct"; });
        });

        cfg.Publish<ConvertVideoCommand>(p =>
        {
            p.ExchangeType = "direct";
            p.Durable = true;
            p.AutoDelete = false;
            p.BindQueue("video-events", "video-processing", c => { c.RoutingKey = "video.upload"; c.AutoDelete = false; c.Durable = true; c.ExchangeType = "direct"; });
        });
        cfg.Publish<VideoReadyToPublishEvent>(p =>
        {
            p.ExchangeType = "direct";
            p.Durable = true;
            p.AutoDelete = false;
            p.BindQueue("video-events", "video-publish", c => { c.RoutingKey = "video.publish"; c.AutoDelete = false; c.Durable = true; c.ExchangeType = "direct"; });
        });
        cfg.ConfigureEndpoints(context);

    });
});

var app = builder.Build();
app.Run();

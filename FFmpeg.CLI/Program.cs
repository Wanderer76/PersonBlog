using FFmpeg.Service;
using FileStorage.Service;
using MessageBus;
using MessageBus.Shared.Configs;
using Profile.Domain.Events;
using Profile.Persistence;
using VideoProcessing.Cli;
using VideoProcessing.Cli.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProfilePersistence(builder.Configuration);
builder.Services.AddFileStorage();
builder.Services.AddHostedService<VideoConverterHostedService>();
builder.Services.AddFFMpeg(builder.Configuration);


//builder.Services.AddMassTransit(x =>
//{
//    x.AddConsumer<VideoChunksCombinerService>();
//    x.AddConsumer<ConvertVideoFile>();

//    x.UsingRabbitMq((ctx, cfg) =>
//    {
//        var connection = builder.Configuration.GetSection("RabbitMQ:Connection").Get<RabbitMqConnection>()!;
//        var queue = builder.Configuration.GetSection("RabbitMQ:UploadVideoConfig").Get<RabbitMqUploadVideoConfig>()!;
//        cfg.Host(connection.HostName, (ushort)connection.Port, "/", h =>
//        {
//            h.Username(connection.UserName);
//            h.Password(connection.Password);
//        });
//        cfg.ReceiveEndpoint(queue.VideoProcessQueue, e =>
//        {
//            e.PrefetchCount = 20;
//            e.AutoDelete = false;
//            e.DiscardSkippedMessages();
//            e.Bind(queue.ExchangeName, x =>
//            {
//                x.Durable = true;
//                x.ExchangeType = "direct";
//                x.RoutingKey = queue.VideoConverterRoutingKey;
//                x.AutoDelete = false;
//            });
//            e.Bind(queue.ExchangeName, x =>
//            {
//                x.Durable = true;
//                x.ExchangeType = "direct";
//                x.RoutingKey = queue.FileChunksCombinerRoutingKey;
//                x.AutoDelete = false;
//            });

//            e.ConfigureConsumer<VideoChunksCombinerService>(ctx);
//            e.ConfigureConsumer<ConvertVideoFile>(ctx);

//        });
//    });

//});


builder.Services.AddMessageBus(builder.Configuration)
    .AddSubscription<CombineFileChunksEvent, VideoChunksCombinerService>()
    .AddSubscription<VideoConvertEvent, ConvertVideoFile>()
    .AddConnectionConfig(builder.Configuration.GetSection("RabbitMq:UploadVideoConfig").Get<RabbitMqUploadVideoConfig>()!);


var app = builder.Build();
app.Run();

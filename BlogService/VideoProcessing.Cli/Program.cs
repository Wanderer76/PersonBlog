using Blog.Contracts.Events;
using FFmpeg.Service;
using FileStorage.Service;
using Infrastructure.Extensions;
using MessageBus;
using MessageBus.Models;
using VideoProcessing.Cli;
using VideoProcessing.Cli.Service;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddFileStorage(builder.Configuration);
builder.Services.AddFFMpeg(builder.Configuration);
builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddMessageBus(builder.Configuration)
    .AddSubscription<CombineFileChunksCommand, VideoChunksCombinerService>(h=> 
    {
        h.QueueName = "combine-chunks";
        h.Exchange = new ExchangeParam
        {
            Name = "video-event",
            RoutingKey = "chunks.combine"
        };
    })
    .AddSubscription<ConvertVideoCommand, ProcessVideoToHls>(x =>
    {
        x.QueueName = "video-convert";
        x.Exchange = new ExchangeParam
        {
            Name = "video-event",
            RoutingKey = "video.convert"
        };
    });

var app = builder.Build();
app.MapDefaultEndpoints();
app.Run();

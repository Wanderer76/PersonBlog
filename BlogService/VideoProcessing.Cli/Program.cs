using Blog.Contracts.Events;
using FFmpeg.Service;
using FileStorage.Service;
using Infrastructure.Extensions;
using MessageBus;
using MessageBus.Configs;
using MessageBus.Models;
using VideoProcessing.Cli.Service;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddFileStorage(builder.Configuration);
builder.Services.AddFFMpeg(builder.Configuration);
builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddRabbitMqMessageBus(builder.Configuration.GetSection("RabbitMQ:Connection").Get<RabbitMqConnection>()!)
    .AddSubscription<ConvertVideoCommand, ProcessVideoToHls>(x =>
    {
        x.QueueName = "video-convert";
        x.Exchange = new ExchangeParam
        {
            Name = "video-event",
            RoutingKey = "video.convert"
        };
    });

builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

var app = builder.Build();

app.UseSwagger();
//app.UseCustomSwagger(app.Configuration);
app.UseSwaggerUI();
app.UseRouting();
app.MapControllers();
app.MapDefaultEndpoints();
app.Run();

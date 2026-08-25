using Blog.Contracts.Events;
using FFmpeg.Service;
using FFmpeg.Service.Models;
using FileStorage.Service;
using Infrastructure.Extensions;
using MessageBus;
using MessageBus.Configs;
using MessageBus.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using VideoProcessing.Cli.Hubs;
using VideoProcessing.Cli.Service;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Host.AddSerilogLogger(builder.Configuration);
builder.Services.AddFileStorage(builder.Configuration);
builder.Services.AddFFMpegVideoService(
    builder.Configuration.GetSection("FFMpegOptions:FFMpeg").Get<FFMpegOptions>()!,
    builder.Configuration.GetSection("FFMpegOptions:HlsVideoPresets").Get<HlsVideoPresets>()!
);

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

builder.Services.AddScoped<VideoConversionService>();
builder.Services.AddSingleton<IVideoProgressNotifier, SignalRVideoProgressNotifier>();
builder.Services.AddSignalR();
builder.Services.AddCors();
builder.Services.AddCustomJwtAuthentication();
builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.Events ??= new JwtBearerEvents();
    options.Events.OnMessageReceived = context =>
    {
        var accessToken = context.Request.Query["access_token"];
        if (!string.IsNullOrEmpty(accessToken) &&
            context.HttpContext.Request.Path.StartsWithSegments("/videohub"))
        {
            context.Token = accessToken;
        }

        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

var app = builder.Build();

app.UseSwagger();
//app.UseCustomSwagger(app.Configuration);
app.UseSwaggerUI();
app.UseRouting();
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<VideoProcessingHub>("/videohub");
app.MapDefaultEndpoints();
app.Run();

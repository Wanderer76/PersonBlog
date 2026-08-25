using Authentication.Contract;
using FFmpeg.Service;
using FileStorage.Service;
using Infrastructure.Extensions;
using Infrastructure.Interface;
using Infrastructure.Middleware;
using MessageBus;
using MessageBus.Configs;
using Music.API.HostedServices;
using Music.Contract.Events;
using Music.Domain.EventHandlers;
using Music.Persistence;
using Music.Service;
using MusicRecommendation.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Host.AddSerilogLogger(builder.Configuration);
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMusicPersistence(builder.Configuration);
builder.Services.AddMusicServices();
builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddCustomJwtAuthentication();
builder.Services.AddFileStorage(builder.Configuration);
builder.Services.AddUserSessionServices(s => { s.BaseUrl = builder.Configuration["AppUrls:Auth"]; });
builder.Services.AddFFMpegAudioExtractorService(builder.Configuration);
builder.Services.AddHostedService<OutboxPublisherService>();
builder.Services.AddMusicRecommendationServices(builder.Configuration);
builder.Services.AddRabbitMqMessageBus(builder.Configuration.GetSection("RabbitMQ:Connection").Get<RabbitMqConnection>()!);
//.AddSubscription<ListenHistoryEvent, ListenHistoryEventHandler>(cfg =>
//{
//    cfg.QueueName = "track-listened";
//});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var initializers = scope.ServiceProvider.GetServices<IDbInitializer>();
    foreach (var initializer in initializers)
    {
        initializer.Initialize();
    }
}
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
app.UseRouting();
app.UseAuthentication();
app.UseJwtMiddleware();
app.UseAuthorization();

app.MapControllers();

app.Run();

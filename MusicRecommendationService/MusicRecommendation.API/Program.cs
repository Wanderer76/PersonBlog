using Authentication.Contract;
using Infrastructure.Extensions;
using Infrastructure.Interface;
using MessageBus;
using MessageBus.Configs;
using Music.Contract.Events;
using MusicRecommendation.Domain.Handlers;
using MusicRecommendation.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Host.AddSerilogLogger(builder.Configuration);
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMusicRecommendationPersistence(builder.Configuration);
builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddUserSessionServices(s => { s.BaseUrl = builder.Configuration["AppUrls:Auth"]; });
builder.Services.AddCustomJwtAuthentication();
builder.Services.AddRabbitMqMessageBus(builder.Configuration.GetSection("RabbitMQ:Connection").Get<RabbitMqConnection>()!)
    .AddSubscription<TrackCreateEvent, TrackCreateHandler>(cfg =>
    {
        cfg.QueueName = "recommendations-track-create";
    }).AddSubscription<TrackDeleteEvent, TrackCreateHandler>(cfg =>
    {
        cfg.QueueName = "recommendations-track-delete";
    })
    .AddSubscription<ListenHistoryEvent, ListenHistoryEventHandler>(cfg =>
    {
        cfg.QueueName = "recommendations-track-listened";
    });
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

app.UseAuthorization();

app.MapControllers();

app.Run();

using Authentication.Contract;
using Blog.API.HostedServices;
using Blog.API.Saga;
using Blog.Contracts.Events;
using Blog.Domain.Events.Handlers;
using Blog.Persistence;
using Blog.Service.EventHandlers;
using Blog.Service.Extensions;
using FileStorage.Service;
using Infrastructure.Extensions;
using Infrastructure.Interface;
using Infrastructure.Middleware;
using MediaProcessing.Contract;
using MessageBus;
using MessageBus.Configs;
using MessageBus.Models;
using Profile.Domain.Events;
using Recommendation.Contracts;
using Recommendation.Contracts.Events;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Host.AddSerilogLogger(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddBlogServices();
builder.Services.AddUserSessionServices(s => { s.BaseUrl = builder.Configuration["AppUrls:Auth"]!; });
builder.Services.AddProfilePersistence(builder.Configuration);
builder.Services.AddCustomJwtAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddFileStorage(builder.Configuration);
builder.Services.AddCors();
builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddMediaProcessingContract(builder.Configuration);

builder.Services.AddRabbitMqMessageBus(builder.Configuration.GetSection("RabbitMQ:Connection").Get<RabbitMqConnection>()!)
    .AddVideoConvertSaga()
    .AddSubscription<SubscribeCreateEvent, SubscribeHandlers>(x =>
    {
        x.QueueName = "subscribers-sync";
        x.Exchange = new ExchangeParam
        {
            Name = "user-subscribe",
            RoutingKey = "created"
        };
    }).AddSubscription<SubscribeCancelEvent, SubscribeHandlers>(x =>
    {
        x.QueueName = "subscribers-sync";
        x.Exchange = new ExchangeParam
        {
            Name = "user-subscribe",
            RoutingKey = "canceled"
        };
    })
    .AddSubscription<PostBannedEvent, PostBannedEventHandler>(x =>
    {
        x.QueueName = "post-to-ban";
        x.Exchange = new ExchangeParam
        {
            Name = "blogs",
            RoutingKey = "post.banned"
        };
    })
    .AddSubscription<PostUnBannedEvent, PostUnBannedEventHandler>(x =>
    {
        x.QueueName = "post-to-unban";
        x.Exchange = new ExchangeParam
        {
            Name = "blogs",
            RoutingKey = "post.unbanned"
        };
    })
    .AddMessage<PostCatalogChangedV2>(x =>
    {
        x.Exchange = RecommendationExchange.Name;
        x.RoutingKey = RecommendationExchange.PostCatalogChangedV2RoutingKey;
    })
    .AddMessage<PostPublishedV1>(x =>
    {
        x.Exchange = BlogIntegrationEvents.Exchange;
        x.RoutingKey = BlogIntegrationEvents.PostPublishedV1RoutingKey;
    })
    .AddMessage<VideoProcessingCompletedV1>(x =>
    {
        x.Exchange = BlogIntegrationEvents.Exchange;
        x.RoutingKey = VideoProcessingIntegrationEvents.CompletedV1RoutingKey;
    })
    .AddMessage<VideoProcessingFailedV1>(x =>
    {
        x.Exchange = BlogIntegrationEvents.Exchange;
        x.RoutingKey = VideoProcessingIntegrationEvents.FailedV1RoutingKey;
    });

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = long.MaxValue;
});

builder.Services.AddHostedService<OutboxPublisherService>()
    .AddHostedService<PostRemoveHostedService>();

var app = builder.Build();

//if (app.Environment.IsDevelopment())
{

    app.UseCustomSwagger(app.Configuration);
    app.UseSwaggerUI();
}
using (var scope = app.Services.CreateScope())
{
    var initializers = scope.ServiceProvider.GetServices<IDbInitializer>();
    foreach (var initializer in initializers)
    {
        initializer.Initialize();
    }
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(policy => policy.WithOrigins("*").AllowAnyHeader().AllowAnyMethod());
app.UseAuthentication();
app.UseJwtMiddleware();
app.UseAuthorization();
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();


using Blog.API.Handlers;
using Blog.API.HostedServices;
using Blog.Contracts.Events;
using Blog.Domain.Entities;
using Blog.Domain.Events;
using Blog.Persistence;
using Blog.Service.Extensions;
using FileStorage.Service;
using Infrastructure.Extensions;
using Infrastructure.Interface;
using MassTransit;
using MessageBus;
using MessageBus.Configs;
using MessageBus.Models;
using MessageBus.Shared.Configs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddProfileServices();
builder.Services.AddUserSessionServices();
builder.Services.AddProfilePersistence(builder.Configuration);
builder.Services.AddCustomJwtAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddFileStorage(builder.Configuration);
builder.Services.AddCors();
builder.Services.AddRedisCache(builder.Configuration);

builder.Services.AddMessageBus(builder.Configuration)
    .AddSubscription<UserViewedSyncEvent, SyncProfileViewsHandler>(x =>
    {
        x.Name = "video-sync";
        x.Exchange = new ExchangeParam
        {
            Name = "view-reacting",
            RoutingKey = "video.sync"
        };
    })
    .AddSubscription<UserReactionSyncEvent, SyncProfileViewsHandler>(x =>
    {
        x.Name = "video-sync"; 
        x.Exchange = new ExchangeParam
        {
            Name = "view-reacting",
            RoutingKey = "video.sync"
        };
    })
    .AddSubscription<CombineFileChunksCommand, VideoProcessSagaHandler>(x =>
    {
        x.Name = "saga-queue";
        x.Exchange = new ExchangeParam
        {
            Name = "video-event",
            RoutingKey = "saga"
        };
    })
    .AddSubscription<ChunksCombinedResponse, VideoProcessSagaHandler>(x =>
    {
        x.Name = "saga-queue"; 
        x.Exchange = new ExchangeParam
        {
            Name = "video-event",
            RoutingKey = "saga"
        };
    })
    .AddSubscription<VideoConvertedResponse, VideoProcessSagaHandler>(x =>
    {
        x.Name = "saga-queue"; 
        x.Exchange = new ExchangeParam
        {
            Name = "video-event",
            RoutingKey = "saga"
        };
    })
    .AddSubscription<VideoPublishedResponse, VideoProcessSagaHandler>(x =>
    {
        x.Name = "saga-queue";
        x.Exchange = new ExchangeParam
        {
            Name = "video-event",
            RoutingKey = "saga"
        };
    })
    .AddSubscription<VideoReadyToPublishEvent, VideoReadyToPublishEventHandler>(x =>
    {
        x.Name = "saga-queue";
        x.Exchange = new ExchangeParam
        {
            Name = "video-event",
            RoutingKey = "saga"
        };
    })
    .AddConnectionConfig(builder.Configuration.GetSection("RabbitMq:UploadVideoConfig").Get<RabbitMqUploadVideoConfig>()!);


//builder.Services.AddMassTransit(x =>
//{
//    x.AddSagaStateMachine<VideoProcessingSaga, VideoProcessingSagaState>()
//        .EntityFrameworkRepository(r =>
//        {
//            r.ConcurrencyMode = ConcurrencyMode.Optimistic;
//            r.ExistingDbContext<ProfileDbContext>();
//            r.UsePostgres(builder.Configuration["ConnectionStrings:ProfileDbContext"]);
//        });

//    x.AddConsumer<VideoReadyToPublishEventHandler>();

//    x.UsingRabbitMq((ctx, cfg) =>
//    {
//        var rabbit = builder.Configuration.GetSection("RabbitMQ:Connection").Get<RabbitMqConnection>()!;
//        cfg.Host($"rabbitmq://{rabbit.HostName}:{rabbit.Port}", c =>
//        {
//            c.Username(rabbit.UserName);
//            c.Password(rabbit.Password);
//        });

//        cfg.ReceiveEndpoint("video-publish", e =>
//        {
//            e.Durable = true;
//            e.Exclusive = false;
//            e.AutoDelete = false;

//            e.ConfigureConsumer<VideoReadyToPublishEventHandler>(ctx);
//        });
//        cfg.ConfigureEndpoints(ctx);

//    });

//});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = long.MaxValue;
});

builder.Services.AddHostedService<OutboxPublisherService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var initializers = scope.ServiceProvider.GetServices<IDbInitializer>();
        foreach (var initializer in initializers)
        {
            initializer.Initialize();
        }
    }
    app.UseCustomSwagger(app.Configuration);
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(policy => policy.WithOrigins("http://localhost:3000").AllowCredentials().AllowAnyHeader().AllowAnyMethod());
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();


app.Run();


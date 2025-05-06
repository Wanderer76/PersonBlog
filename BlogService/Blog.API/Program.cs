using Blog.API;
using Blog.API.Handlers;
using Blog.API.HostedServices;
using Blog.API.Models;
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
using MessageBus.Shared.Configs;
using MessageBus.Shared.Events;

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

//builder.Services.AddMessageBus(builder.Configuration)
//    .AddSubscription<UserViewedSyncEvent, SyncProfileViewsHandler>()
//    .AddConnectionConfig(builder.Configuration.GetSection("RabbitMq:UploadVideoConfig").Get<RabbitMqUploadVideoConfig>()!);


builder.Services.AddMassTransit(x =>
{
    x.AddSagaStateMachine<VideoProcessingSaga, VideoProcessingSagaState>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Optimistic;
            r.ExistingDbContext<ProfileDbContext>();
            r.UsePostgres(builder.Configuration["ConnectionStrings:ProfileDbContext"]);
        });

    x.AddConsumer<VideoReadyToPublishEventHandler>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        var rabbit = builder.Configuration.GetSection("RabbitMQ:Connection").Get<RabbitMqConnection>()!;
        cfg.Host($"rabbitmq://{rabbit.HostName}:{rabbit.Port}", c =>
        {
            c.Username(rabbit.UserName);
            c.Password(rabbit.Password);
        });

        //cfg.MessageTopology.SetEntityNameFormatter(new DirectEntityNameFormatter(cfg.MessageTopology.EntityNameFormatter));
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
        });

        cfg.ReceiveEndpoint("video-publish", e =>
        {
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

            e.ConfigureConsumer<VideoReadyToPublishEventHandler>(ctx);
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
        cfg.ConfigureEndpoints(ctx);

    });

});

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


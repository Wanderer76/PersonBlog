using Authentication.Service;
using Blog.API.Handlers;
using Blog.API.HostedServices;
using Blog.Persistence;
using Blog.Service.Extensions;
using FileStorage.Service;
using Infrastructure.Cache;
using Infrastructure.Extensions;
using Infrastructure.Interface;
using MessageBus;
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
builder.Services.AddFileStorage();
builder.Services.AddCors();
builder.Services.AddRedisCache(builder.Configuration);

builder.Services.AddMessageBus(builder.Configuration)
    .AddSubscription<UserViewedSyncEvent, SyncProfileViewsHandler>()
    .AddConnectionConfig(builder.Configuration.GetSection("RabbitMq:UploadVideoConfig").Get<RabbitMqUploadVideoConfig>()!);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = long.MaxValue;
});

builder.Services.AddHostedService<OutboxPublisherService>()
    .AddHostedService<SyncProfileHostedService>();

//builder.Services.AddMassTransit(x =>
//{
//    //x.AddConsumers(typeof(Program).Assembly);
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

//        });
//    });
//});

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


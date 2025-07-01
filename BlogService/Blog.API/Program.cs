using Blog.API.Handlers;
using Blog.API.HostedServices;
using Blog.API.Saga;
using Blog.Contracts.Events;
using Blog.Domain.Events;
using Blog.Persistence;
using Blog.Service.Extensions;
using FileStorage.Service;
using Infrastructure.Extensions;
using Infrastructure.Interface;
using Infrastructure.Middleware;
using MessageBus;
using MessageBus.Models;
using ViewReacting.Domain.Events;

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
    .AddVideoConvertSaga()
    .AddSubscription<SubscribeCreateEvent, SubscribeHandlers>(x =>
    {
        x.Name = "subscribers-sync";
        x.Exchange = new ExchangeParam
        {
            Name = "user-subscribe",
            RoutingKey = "created"
        };
    }).AddSubscription<SubscribeCancelEvent, SubscribeHandlers>(x =>
    {
        x.Name = "subscribers-sync";
        x.Exchange = new ExchangeParam
        {
            Name = "user-subscribe",
            RoutingKey = "canceled"
        };
    });

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = long.MaxValue;
});

builder.Services.AddHostedService<OutboxPublisherService>()
    .AddHostedService<PostRemoveHostedService>();

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
app.UseCors(policy => policy.WithOrigins("*").AllowAnyHeader().AllowAnyMethod());
app.UseAuthentication();
app.UseAuthorization();
app.UseJwtMiddleware();
app.MapControllers();


app.Run();


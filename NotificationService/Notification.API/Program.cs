using Authentication.Contract;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MessageBus;
using MessageBus.Configs;
using Notification.Infrastructure;
using Notification.Infrastructure.Delivery;
using Notification.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Host.AddSerilogLogger(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddDataProtection();
builder.Services.AddSingleton<Notification.API.Services.NotificationCursorProtector>();
builder.Services.AddNotificationInfrastructure();
builder.Services.AddUserSessionServices(options => options.BaseUrl = builder.Configuration["AppUrls:Auth"]!);
builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddNotificationPersistence(builder.Configuration);
builder.Services.AddNotificationWorkers(options => builder.Configuration.GetSection("Notification:Worker").Bind(options));
builder.Services.AddCustomJwtAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddRabbitMqMessageBus(
        builder.Configuration.GetSection("RabbitMQ:Connection").Get<RabbitMqConnection>()
        ?? throw new InvalidOperationException("RabbitMQ:Connection is required."))
    .AddNotificationIntegrationEvents();
builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.Events ??= new JwtBearerEvents();
    options.Events.OnMessageReceived = context =>
    {
        var accessToken = context.Request.Query["access_token"];
        if (!string.IsNullOrEmpty(accessToken) &&
            context.HttpContext.Request.Path.StartsWithSegments("/hubs/notifications"))
        {
            context.Token = accessToken;
        }

        return Task.CompletedTask;
    };
});
// The legacy PostUpdateEvent lacks the business identifiers required by durable intake.
// Producers/consumers enqueue validated NotificationIngress through INotificationWorkStore.

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapDefaultEndpoints();

app.Run();

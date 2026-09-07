using Authentication.Contract;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Notification.Infrastructure;
using Notification.Infrastructure.Delivery;
using Notification.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Host.AddSerilogLogger(builder.Configuration);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddNotificationInfrastructure();
builder.Services.AddUserSessionServices(options => options.BaseUrl = builder.Configuration["AppUrls:Auth"]!);
builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddNotificationPersistence(builder.Configuration);
builder.Services.AddNotificationWorkers(options => builder.Configuration.GetSection("Notification:Worker").Bind(options));
builder.Services.AddCustomJwtAuthentication();
builder.Services.AddAuthorization();
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/weatherforecast", () =>
{
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.UseAuthentication();
app.UseAuthorization();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapDefaultEndpoints();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

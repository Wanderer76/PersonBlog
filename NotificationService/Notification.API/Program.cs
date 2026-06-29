using Blog.Contracts.Events;
using MessageBus;
using MessageBus.Configs;
using Notification.Domain.EventHandlers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("Blog", x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Blog"]);
    x.Timeout = TimeSpan.FromSeconds(1);
});
builder.Services.AddRabbitMqMessageBus(builder.Configuration.GetSection("RabbitMQ:Connection").Get<RabbitMqConnection>()!)
    .AddSubscription<PostUpdateEvent, PostCreateEventHandler>(cfg =>
    {
        cfg.QueueName = "post-create-notifications";
        cfg.Exchange = new MessageBus.Models.ExchangeParam
        {
            Name = "post-update",
            ExchangeType = "fanout"
        };
    });

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

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

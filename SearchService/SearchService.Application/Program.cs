using Blog.Contracts.Events;
using Infrastructure.Interface;
using MessageBus;
using MessageBus.Configs;
using Search.Persistence;
using Search.Service;
using SearchService.Application.Consumers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSearchService(builder.Configuration);
builder.Services.AddSearchPersistence(builder.Configuration);
builder.Services.AddRabbitMqMessageBus(builder.Configuration.GetSection("RabbitMQ:Connection").Get<RabbitMqConnection>()!)
    .AddSubscription<PostUpdateEvent, PostUpdateEventHandler>(cfg =>
    {
        cfg.QueueName = "post-search-sync";
        cfg.Durable = true;
        cfg.Exchange = new MessageBus.Models.ExchangeParam
        {
            Name = "post-update",
            ExchangeType = "fanout"
        };
    });
builder.Services.AddHttpClient("Tokenizer", cfg =>
{
    cfg.BaseAddress = new Uri(builder.Configuration["AppUrls:Tokenizer"]);
});

var app = builder.Build();
app.UseSearchService(app.Configuration);
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

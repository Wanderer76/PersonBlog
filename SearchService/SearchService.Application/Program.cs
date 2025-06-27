using Blog.Contracts.Events;
using MessageBus;
using Search.Service;
using SearchService.Application.Consumers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSearchService(builder.Configuration);
builder.Services.AddMessageBus(builder.Configuration)
    .AddSubscription<PostUpdateEvent, PostUpdateEventHandler>(cfg =>
    {
        cfg.Name = "post-search-sync";
        cfg.Durable = true;
        cfg.Exchange = new MessageBus.Models.ExchangeParam
        {
            Name = "post-update",
            ExchangeType = "fanout"
        };
    });
builder.Services.AddHttpClient("Tokenizer", cfg =>
{
    cfg.BaseAddress = new Uri("http://127.0.0.1:8000/");
});

var app = builder.Build();
app.UseSearchService(app.Configuration);
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

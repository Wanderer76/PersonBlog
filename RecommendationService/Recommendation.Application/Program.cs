using Blog.Persistence;
using FileStorage.Service;
using Infrastructure.Extensions;
using Infrastructure.Interface;
using MessageBus;
using MessageBus.Configs;
using Recommendation.Persistence;
using Recommendation.Service;
using Recommendation.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddBlogServices();
builder.Services.AddFileStorage(builder.Configuration);
builder.Services.AddProfilePersistence(builder.Configuration);
builder.Services.AddRecommendationEventServices();
builder.Services.AddRecommendationPersistence(builder.Configuration);
builder.Services
    .AddRabbitMqMessageBus(
        builder.Configuration.GetSection("RabbitMQ:Connection").Get<RabbitMqConnection>()
        ?? throw new InvalidOperationException("RabbitMQ connection is not configured."))
    .AddRecommendationEventSubscriptions();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializers = scope.ServiceProvider.GetServices<IDbInitializer>();
    foreach (var initializer in initializers)
    {
        initializer.Initialize();
    }
}

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.MapDefaultEndpoints();
app.MapControllers();

app.Run();

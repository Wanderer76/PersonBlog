using Infrastructure.Extensions;
using Infrastructure.Interface;
using MessageBus;
using MessageBus.Configs;
using Recommendation.Application.Services;
using Recommendation.Persistence;
using Recommendation.Services;
using Recommendation.Services.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCustomJwtAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddRecommendationEventServices();
builder.Services.AddRecommendationFeedServices(options =>
    builder.Configuration.GetSection(RecommendationFeedOptions.SectionName).Bind(options));
builder.Services.AddRecommendationPersistence(builder.Configuration);
builder.Services.AddScoped<IRecommendationSubjectResolver, RecommendationSubjectResolver>();
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

app.UseAuthentication();
app.UseAuthorization();
app.MapDefaultEndpoints();
app.MapControllers();

app.Run();

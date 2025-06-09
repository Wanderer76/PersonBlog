using Infrastructure.Extensions;
using Infrastructure.Interface;
using MessageBus;
using VideoReacting.API.HostedService;
using VideoReacting.Persistence;
using VideoReacting.Service;
using ViewReacting.Domain.Events;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddViewReactingPersistence(builder.Configuration);
builder.Services.AddVideoReactingService();
builder.Services.AddUserSessionServices();
builder.Services.AddCustomJwtAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddCors();
builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddMessageBus(builder.Configuration)
    .AddSubscription<VideoViewEvent, VideoViewEventHandler>(x =>
    {
        x.Name = QueueConstants.QueueName;
        x.Exchange = new MessageBus.Models.ExchangeParam { RoutingKey = QueueConstants.RoutingKey, Name = QueueConstants.Exchange };
    });

builder.Services.AddHttpClient("Blog", x =>
{
    x.BaseAddress = new Uri("http://localhost:5069/api/");
});

builder.Services.AddHostedService<ReactionOutbox>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    using (var scope = app.Services.CreateScope())
    {
        var initializers = scope.ServiceProvider.GetServices<IDbInitializer>();
        foreach (var initializer in initializers)
        {
            initializer.Initialize();
        }
    }
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(policy => policy.WithOrigins("http://localhost:3000", "http://localhost:5165").AllowCredentials().AllowAnyHeader().AllowAnyMethod());
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

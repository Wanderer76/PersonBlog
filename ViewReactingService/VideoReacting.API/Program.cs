using Infrastructure.Extensions;
using Infrastructure.Interface;
using MessageBus;
using VideoReacting.API.Consumer;
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
builder.Services.AddCustomJwtAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddMessageBus(builder.Configuration)
    .AddSubscription<VideoViewEvent, VideoViewEventHandler>();

builder.Services.AddHostedService<ViewHandlerHostedService>();
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

app.UseAuthorization();
app.UseAuthentication();

app.MapControllers();

app.Run();

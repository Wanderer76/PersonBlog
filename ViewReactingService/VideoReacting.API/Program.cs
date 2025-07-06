using Blog.Contracts.Events;
using Blog.Domain.Entities;
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
    }) .AddSubscription<PostUpdateEvent, PostUpdateEventHandler>(cfg =>
    {
        cfg.Name = "userReaction-post-sync";
        cfg.Durable = true;
        cfg.Exchange = new MessageBus.Models.ExchangeParam
        {
            Name = "post-update",
            ExchangeType = "fanout"
        };
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
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

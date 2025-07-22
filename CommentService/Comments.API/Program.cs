using Authentication.Contract.Events;
using Comments.Domain.Services;
using Comments.Persistence.Extensions;
using Comments.Service.Extensions;
using Infrastructure.Extensions;
using Infrastructure.Interface;
using MessageBus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCommentService();
builder.Services.AddCustomJwtAuthentication();
builder.Services.AddUserSessionServices();
builder.Services.AddCommentPersistence(builder.Configuration);
builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddMessageBus(builder.Configuration)
    .AddSubscription<UserCreateEvent, UserCreateEventHandler>(cfg =>
    {
        cfg.QueueName = "comment-userprofile-create";
    });
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    foreach (var initializer in scope.ServiceProvider.GetServices<IDbInitializer>())
    {
        initializer.Initialize();
    }
}

app.UseAuthorization();

app.MapControllers();

app.Run();

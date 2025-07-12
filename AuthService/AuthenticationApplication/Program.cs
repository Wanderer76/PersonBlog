using Authentication.Peristence;
using Authentication.Service;
using Authentication.Service.Service;
using Blog.Contracts.Events;
using Infrastructure.Extensions;
using Infrastructure.Interface;
using MessageBus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthenticationPersistence(builder.Configuration);
builder.Services.AddCustomJwtAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddAuthServices();
builder.Services.AddHttpClient("Blog", x =>
{
    x.BaseAddress = new Uri("http://localhost:5069/api/");
});
builder.Services.AddUserSessionServices();
builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddMessageBus(builder.Configuration)
    .AddSubscription<BlogCreateEvent, BlogCreateEventHandler>(cfg =>
    {
        cfg.QueueName = "auth-blog";
    });
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseCustomSwagger(app.Configuration);
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

app.UseRouting();
app.UseHttpsRedirection();
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
app.UseAuthentication();
app.UseAuthorization();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers(); // подключаем маршрутизацию на контроллеры
});
app.Run();

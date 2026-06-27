using Authentication.Contract;
using Authentication.Peristence;
using Authentication.Service;
using Authentication.Service.Models;
using Authentication.Service.Models.Options;
using Authentication.Service.Service;
using AuthenticationApplication.HostedServices;
using Blog.Contracts.Events;
using Infrastructure.Extensions;
using Infrastructure.Interface;
using Infrastructure.Services;
using MessageBus;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new ResultConverterFactory());

    });
builder.Services.AddSwaggerGen();
builder.Services.AddCors();
builder.Services.AddAuthenticationPersistence(builder.Configuration);
builder.Services.AddCustomJwtAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddAuthServices(builder.Configuration.GetSection("TokenOptions").Get<TokenOptions>()!);
builder.Services.AddHttpClient("Blog", x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Blog"]!);
});
builder.Services.AddUserSessionServices();
builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddRabbitMqMessageBus(builder.Configuration)
    .AddSubscription<BlogCreateEvent, BlogCreateEventHandler>(cfg =>
    {
        cfg.QueueName = "auth-blog";
    });

builder.Services.AddHostedService<EventPublishService>();
builder.Services.AddHostedService<TokenCleanerHostedService>();

var app = builder.Build();

//if (app.Environment.IsDevelopment())
{
    app.UseCustomSwagger(app.Configuration);
    app.UseSwaggerUI();

}
using (var scope = app.Services.CreateScope())
{
    var initializers = scope.ServiceProvider.GetServices<IDbInitializer>();
    foreach (var initializer in initializers)
    {
        initializer.Initialize();
    }
}
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapDefaultEndpoints();
app.Run();

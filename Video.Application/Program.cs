using Blog.Service;
using FileStorage.Service;
using Infrastructure.Cache;
using Infrastructure.Extensions;
using Infrastructure.Interface;
using MessageBus;
using Microsoft.AspNetCore.HttpOverrides;
using Profile.Persistence;
using Profile.Service.Extensions;
using Video.Persistence;
using Video.Service;
using VideoView.Application.HostedServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProfilePersistence(builder.Configuration);
builder.Services.AddVideoPersistence(builder.Configuration);
builder.Services.AddProfileServices();
builder.Services.AddFileStorage();
builder.Services.AddVideoService();
builder.Services.AddBlogServices();
builder.Services.AddHttpClient("Profile", x =>
{
    x.BaseAddress = new Uri("http://localhost:7892/profile/");
});
builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddCustomJwtAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddMessageBus(builder.Configuration);
builder.Services.AddHostedService<VideoReactionOutbox>();

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

app.UseCors(policy => policy.WithOrigins("http://localhost:3000").AllowCredentials().AllowAnyHeader().AllowAnyMethod());
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor |
    ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

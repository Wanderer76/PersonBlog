using AuthenticationApplication.Service;
using FileStorage.Service;
using Infrastructure.Extensions;
using Infrastructure.Middleware;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddFileStorage(builder.Configuration);
builder.Services.AddHttpClient("Auth", x =>
{
    x.BaseAddress = new Uri("http://localhost:5179/api/");
});

builder.Services.AddHttpClient("Profile", x =>
{
    x.BaseAddress = new Uri("http://localhost:7892/profile/");
});
builder.Services.AddHttpClient("Recommendation", x =>
{
    x.BaseAddress = new Uri("http://localhost:5209/api/");
});
builder.Services.AddHttpClient("Reacting", x =>
{
    x.BaseAddress = new Uri("http://localhost:5153/api/");
});
builder.Services.AddHttpClient("Search", x =>
{
    x.BaseAddress = new Uri("http://localhost:5250/api/");
});

builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddCustomJwtAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseCustomSwagger(app.Configuration);
    app.UseSwaggerUI();
}

app.UseCors(policy => policy.WithOrigins("http://localhost:3000", "http://localhost:5165").AllowCredentials().AllowAnyHeader().AllowAnyMethod());
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor |
    ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseJwtMiddleware();
app.MapControllers();

app.Run();

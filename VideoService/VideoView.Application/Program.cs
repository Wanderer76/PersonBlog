using AuthenticationApplication.Service;
using FileStorage.Service;
using Infrastructure.Extensions;
using Infrastructure.Middleware;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();
builder.Services.AddFileStorage(builder.Configuration);
builder.Services.AddHttpClient("Auth", x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Auth"]);
});

builder.Services.AddHttpClient("Profile", x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Profile"]);
});
builder.Services.AddHttpClient("Recommendation", x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Recommendation"]);
});
builder.Services.AddHttpClient("Reacting", x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Reacting"]);
});
builder.Services.AddHttpClient("Search", x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Search"]);
});
builder.Services.AddHttpClient("Conference", x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Conference"]);
});
builder.Services.AddHttpClient("Comments", x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Comments"]);
});


builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddCustomJwtAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
{
    app.UseCustomSwagger(app.Configuration);
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
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

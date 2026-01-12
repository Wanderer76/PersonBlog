using Authentication.Contract;
using Blog.Contracts;
using Blog.Service.Extensions;
using FileStorage.Service;
using Gateway.API;
using Gateway.API.Services;
using Infrastructure.Extensions;
using Infrastructure.Middleware;
using Microsoft.AspNetCore.HttpOverrides;
using PlayListService.Contract;
using Profile.Service;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();
builder.Host.AddSerilogLogger(builder.Configuration);
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<HeaderClientHandler>();
builder.Services.AddUserSessionServices(s => { s.BaseUrl = builder.Configuration["AppUrls:Auth"]; });
builder.Services.AddFileStorage(builder.Configuration);

builder.Services.AddPlayListContract(builder.Configuration);

builder.Services.AddHttpClient("Auth", x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Auth"]);
}).AddHttpMessageHandler<HeaderClientHandler>();
builder.Services.AddHttpClient("Profile", x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Profile"]);
}).AddHttpMessageHandler<HeaderClientHandler>();
builder.Services.AddHttpClient("Recommendation", x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Recommendation"]);
}).AddHttpMessageHandler<HeaderClientHandler>();
builder.Services.AddHttpClient("Reacting", x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Reacting"]);
}).AddHttpMessageHandler<HeaderClientHandler>();
builder.Services.AddHttpClient("Search", x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Search"]);
}).AddHttpMessageHandler<HeaderClientHandler>();
builder.Services.AddHttpClient("Conference", x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Conference"]);
}).AddHttpMessageHandler<HeaderClientHandler>();
builder.Services.AddHttpClient("Comments", x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Comments"]);
}).AddHttpMessageHandler<HeaderClientHandler>();


builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddCustomJwtAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddProfileHttpClient(builder.Configuration);
builder.Services.AddBlogContract(builder.Configuration);

var app = builder.Build();
app.UseSerilogRequestLogger();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
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

app.MapDefaultEndpoints();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseCorrelationMiddleware();
app.UseJwtMiddleware();
app.MapControllers();

app.Run();

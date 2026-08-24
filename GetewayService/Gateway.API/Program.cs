using Authentication.Contract;
using Blog.Contracts;
using Blog.Service.Extensions;
using FileStorage.Service;
using Gateway.API;
using Gateway.API.Api;
using Gateway.API.Services;
using Infrastructure.Extensions;
using Infrastructure.Middleware;
using Infrastructure.Services;
using Microsoft.AspNetCore.HttpOverrides;
using PlayListService.Contract;
using Profile.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();
builder.Host.AddSerilogLogger(builder.Configuration);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new ResultConverterFactory());
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:3000"];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<HeaderClientHandler>();
builder.Services.AddUserSessionServices(s => { s.BaseUrl = builder.Configuration["AppUrls:Auth"]; });
builder.Services.AddFileStorage(builder.Configuration);

builder.Services.AddPlayListContract(builder.Configuration);

builder.Services.AddHttpClient("Auth", x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Auth"]);
    x.Timeout = TimeSpan.FromSeconds(2);
}).AddHttpMessageHandler<HeaderClientHandler>();
builder.Services.AddHttpClient("Profile", x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Profile"]);
    x.Timeout = TimeSpan.FromSeconds(2);
}).AddHttpMessageHandler<HeaderClientHandler>();
builder.Services.AddHttpClient<RecommendationApiClient>(x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Recommendation"]!);
    x.Timeout = TimeSpan.FromSeconds(5);
}).AddHttpMessageHandler<HeaderClientHandler>();
builder.Services.AddHttpClient<BlogFeedApiClient>(x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Blog"]!);
    x.Timeout = TimeSpan.FromSeconds(5);
}).AddHttpMessageHandler<HeaderClientHandler>();
builder.Services.AddHttpClient<TextPostDetailApiClient>(x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Blog"]!);
    x.Timeout = TimeSpan.FromSeconds(5);
}).AddHttpMessageHandler<HeaderClientHandler>();
builder.Services.AddScoped<RecommendationFeedGateway>();
builder.Services.AddHttpClient("Reacting", x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Reacting"]);
    x.Timeout = TimeSpan.FromSeconds(2);
}).AddHttpMessageHandler<HeaderClientHandler>();
builder.Services.AddHttpClient("Search", x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Search"]);
    x.Timeout = TimeSpan.FromSeconds(2);
}).AddHttpMessageHandler<HeaderClientHandler>();
builder.Services.AddHttpClient("Conference", x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Conference"]);
    x.Timeout = TimeSpan.FromSeconds(2);
}).AddHttpMessageHandler<HeaderClientHandler>();
builder.Services.AddHttpClient("Comments", x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Comments"]);
    x.Timeout = TimeSpan.FromSeconds(2);
}).AddHttpMessageHandler<HeaderClientHandler>();

builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddCustomJwtAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddProfileHttpClient(builder.Configuration);
builder.Services.AddBlogContract(builder.Configuration);

var app = builder.Build();
app.UseSerilogRequestLogger();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
{
    app.UseCustomSwagger(app.Configuration);
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor |
    ForwardedHeaders.XForwardedProto
});

app.MapDefaultEndpoints();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseCorrelationMiddleware();
app.UseJwtMiddleware();
app.UseAuthorization();
app.MapControllers();

app.Run();

using Authentication.Contract;
using Infrastructure.Extensions;
using Infrastructure.Middleware;
using Infrastructure.Services;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new ResultConverterFactory());
}); ;
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<HeaderClientHandler>();
builder.Services.AddUserSessionServices(s => { s.BaseUrl = builder.Configuration["AppUrls:Auth"]; });
builder.Services.AddHttpClient("Auth", x =>
{
    x.BaseAddress = new Uri(builder.Configuration["AppUrls:Auth"]);
    x.Timeout = TimeSpan.FromSeconds(15);
}).AddHttpMessageHandler<HeaderClientHandler>();

builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddCustomJwtAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
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
app.MapControllers();

app.Run();

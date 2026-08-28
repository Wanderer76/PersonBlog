using Authentication.Contract;
using Conference.Persistence.Extensions;
using Conference.Service.Extensions;
using Conference.Service.Hubs;
using Infrastructure.Extensions;
using Infrastructure.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.AddServiceDefaults();
builder.Host.AddSerilogLogger(builder.Configuration);
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddConferencePersistence(builder.Configuration);
builder.Services.AddConferenceService();
builder.Services.AddUserSessionServices(s =>
{
    s.BaseUrl = builder.Configuration["AppUrls:Auth"]
        ?? throw new InvalidOperationException("Authentication service URL is not configured.");
});
builder.Services.AddCustomJwtAuthentication();
builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters.ValidateIssuer = true;
    options.TokenValidationParameters.ValidateAudience = true;
    options.TokenValidationParameters.ValidateIssuerSigningKey = true;
    options.Events ??= new JwtBearerEvents();
    options.Events.OnMessageReceived = context =>
    {
        var accessToken = context.Request.Query["access_token"];
        if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/conference"))
        {
            context.Token = accessToken;
        }

        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization();
builder.Services.AddCors();
builder.Services.AddSignalR();
builder.Services.AddRedisCache(builder.Configuration);
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    foreach (var initializer in scope.ServiceProvider.GetServices<IDbInitializer>())
    {
        initializer.Initialize();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor |
    ForwardedHeaders.XForwardedProto
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<ConferenceHub>("/conference");
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();

using Conference.Persistence.Extensions;
using Conference.Service.Extensions;
using Conference.Service.Hubs;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddConferencePersistence(builder.Configuration);
builder.Services.AddConferenceService();
builder.Services.AddUserSessionServices();
builder.Services.AddCustomJwtAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddCors();
builder.Services.AddSignalR();
builder.Services.AddRedisCache(builder.Configuration);
var app = builder.Build();

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

app.MapHub<ConferenceHub>("/conference");

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

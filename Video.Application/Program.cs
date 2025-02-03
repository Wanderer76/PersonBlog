using Blog.Service;
using FileStorage.Service;
using Infrastructure.Extensions;
using MessageBus;
using Profile.Persistence;
using Profile.Service.Extensions;
using Video.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProfilePersistence(builder.Configuration);
builder.Services.AddProfileServices();
builder.Services.AddFileStorage();
builder.Services.AddVideoService();
builder.Services.AddBlogServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseCustomSwagger(app.Configuration);
    app.UseSwaggerUI();
    app.UseCors(cfg=>cfg.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

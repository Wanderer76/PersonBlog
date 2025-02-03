using FileStorage.Service;
using MessageBus;
using MessageBus.Configs;
using Profile.Persistence;
using VideoProcessing.Cli;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProfilePersistence(builder.Configuration);
builder.Services.AddMessageBus();
builder.Services.AddFileStorage();
builder.Services.AddHostedService<VideoConverterHostedService>();
builder.Services.AddHostedService<FileChunksCombinerHostedService>();
builder.Services.AddSingleton<RabbitMqConfig>(sp => sp.GetRequiredService<IConfiguration>().GetSection("RabbitMQ").Get<RabbitMqConfig>());

var app = builder.Build();
app.Run();

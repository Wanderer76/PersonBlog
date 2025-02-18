using FFmpeg.Service;
using FileStorage.Service;
using MessageBus;
using MessageBus.EventHandler;
using Profile.Domain.Events;
using Profile.Persistence;
using VideoProcessing.Cli;
using VideoProcessing.Cli.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProfilePersistence(builder.Configuration);
builder.Services.AddFileStorage();
builder.Services.AddHostedService<VideoConverterHostedService>();
builder.Services.AddFFMpeg(builder.Configuration);
builder.Services.AddKeyedScoped<IEventHandler, VideoChunksCombinerService>(nameof(CombineFileChunksEvent));
builder.Services.AddKeyedScoped<IEventHandler, ConvertVideoFile>(nameof(VideoUploadEvent));
builder.Services.AddMessageBus(builder.Configuration, new Dictionary<string, Type>
{
    { nameof(CombineFileChunksEvent), typeof(CombineFileChunksEvent) },
    { nameof(VideoUploadEvent), typeof(VideoUploadEvent) }
});


var app = builder.Build();
app.Run();

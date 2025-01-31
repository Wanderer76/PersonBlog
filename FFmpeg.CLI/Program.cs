

using FFmpeg.Service;
using FFMpeg.Cli;
using FileStorage.Service;
using MessageBus;
using Profile.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProfilePersistence(builder.Configuration);
builder.Services.AddMessageBus();
//builder.Services.AddFFMpeg(new FFMpegCore.FFOptions
//{
//    BinaryFolder = "../ffmpeg",
//});
builder.Services.AddFileStorage();
builder.Services.AddHostedService<VideoConverterHostedService>();
builder.Services.AddHostedService<FileChunksCombinerHostedService>();

var app = builder.Build();
//using (var db = app.Services.CreateScope().ServiceProvider.GetRequiredService<ProfileDbContext>())
//{
//    db.Database.EnsureCreated();
//}

app.Run();

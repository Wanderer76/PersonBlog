using MessageBus;
using MessageBus.Shared.Events;
using ReactionProcessing.Cli;
using ReactionProcessing.Cli.HostedServices;
using Video.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//builder.Services.AddControllers();
//// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<VideoReactionProcessingHostedService>();
builder.Services.AddVideoPersistence(builder.Configuration);
builder.Services.AddMessageBus(builder.Configuration)
    .AddSubscription<UserViewedPostEvent, VideoViewEventHandler>();

builder.Services.AddHttpClient("ProfileClient", config =>
{
    config.BaseAddress = new Uri("http://localhost:7892/");
    config.Timeout = new TimeSpan(0, 0, 20);
});

var app = builder.Build();
//app.UseCors(x => x.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());
// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseAuthorization();

//app.MapControllers();

app.Run();

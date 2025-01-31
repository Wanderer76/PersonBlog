using FileStorage.Service;
using Infrastructure.Extensions;
using Infrastructure.Interface;
using MessageBus;
using Profile.Persistence;
using Profile.Service.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddProfileServices();
builder.Services.AddProfilePersistence(builder.Configuration);
builder.Services.AddCustomJwtAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddFileStorage();
builder.Services.AddMessageBus();

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = long.MaxValue;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var initializers = scope.ServiceProvider.GetServices<IDbInitializer>();
        foreach (var initializer in initializers)
        {
            initializer.Initialize();
        }
    }
    app.UseCustomSwagger(app.Configuration);
    app.UseSwaggerUI();
}

//app.UseWhen(context => context.Request.Path.StartsWithSegments("/video"), appBuilder =>
//{
//    var provider = new FileExtensionContentTypeProvider();
//    // Add new mappings
//    provider.Mappings[".m3u8"] = "application/x-mpegURL";
//    provider.Mappings[".ts"] = "video/MP2T";

//    appBuilder.UseStaticFiles(new StaticFileOptions
//    {
//        ContentTypeProvider = provider,
//        RequestPath = "/uploads"
//    });
//});

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();


app.Run();


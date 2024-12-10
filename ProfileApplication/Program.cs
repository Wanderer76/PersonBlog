using FileStorage.Service;
using Infrastructure.Extensions;
using Profile.Persistence;
using Profile.Service.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddProfileServices();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddCustomJwtAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddFileStorage();

builder.Services.AddCors(x => x.AddDefaultPolicy(b => b.AllowAnyMethod().AllowAnyOrigin()));

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = long.MaxValue;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using (var db = app.Services.CreateScope().ServiceProvider.GetRequiredService<ProfileDbContext>())
    {
        db.Database.EnsureCreated();
    }
    app.UseCustomSwagger(app.Configuration);
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.MapControllers();


app.Run();


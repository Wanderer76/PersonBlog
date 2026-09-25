using Authentication.Contract;
using Blog.Contracts;
using FileStorage.Service;
using Infrastructure.Extensions;
using Infrastructure.Interface;
using Infrastructure.Middleware;
using PlayListService.Persistence;
using PlayListService.Services;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.AddServiceDefaults();
        builder.Host.AddSerilogLogger(builder.Configuration);

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddBlogContract(builder.Configuration);
        builder.Services.AddRedisCache(builder.Configuration);
        builder.Services.AddFileStorage(builder.Configuration);
        builder.Services.AddPlayListPersistence(builder.Configuration);
        builder.Services.AddCors();

        builder.Services.AddPlayListService();
        builder.Services.AddUserSessionServices(options =>
        {
            options.BaseUrl = builder.Configuration["AppUrls:Auth"]
                ?? throw new InvalidOperationException("Auth service URL is not configured.");
        });
        builder.Services.AddCustomJwtAuthentication();
        builder.Services.AddAuthorization();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        //if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();

        }
        using (var scope = app.Services.CreateScope())
        {
            var initializers = scope.ServiceProvider.GetServices<IDbInitializer>();
            foreach (var initializer in initializers)
            {
                initializer.Initialize();
            }
        }

        app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
        app.UseRouting();
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseJwtMiddleware();
        app.MapControllers();
        app.MapDefaultEndpoints();
        app.Run();
    }
}

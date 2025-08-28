using Microsoft.Extensions.DependencyInjection;
using Music.Domain.Services;
using Music.Service.Services;

namespace Music.Service;

public static class MusicServiceExtensions
{
    public static void AddMusicServices(this IServiceCollection services)
    {
        services.AddScoped<ITrackService, DefaultTrackService>();
        services.AddScoped<IGenreService, DefaultGenreService>();
        services.AddScoped<ITrackSearchService, DefaultTrackSearchService>();
        services.AddScoped<IAvatarService, DefaultAvatarService>();
        services.AddScoped<IArtistService, DefaultArtistService>();
        services.AddScoped<IArtistSearchService, DefaultArtistSearchService>();
    }
}

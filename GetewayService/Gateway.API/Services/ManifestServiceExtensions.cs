using Infrastructure.Services;

namespace Gateway.API.Services
{
    public static class ManifestServiceExtensions
    {
        public static async Task<string> ReadHlsManifestAsync(
            this IFileStorage storage,
            Guid blogId,
            string file,
            CancellationToken cancellationToken = default)
        {
            using var manifestStream = new MemoryStream();
            await storage.ReadFileAsync(blogId, file, manifestStream, cancellationToken);
            manifestStream.Position = 0;
            using var reader = new StreamReader(manifestStream);
            return await reader.ReadToEndAsync(cancellationToken);
        }
    }
}

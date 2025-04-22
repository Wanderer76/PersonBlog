using FileStorage.Service.Service;
using Infrastructure.Services;

namespace VideoView.Application.Services
{
    public static class ManifestServiceExtensions
    {
        public static async Task<string> ProcessManifestAsync(this IFileStorage storage, Guid postId, string file)
        {
            var manifestStream = new MemoryStream();
            await storage.ReadFileAsync(postId, file, manifestStream);
            manifestStream.Position = 0;
            var manifestContent = await new StreamReader(manifestStream).ReadToEndAsync();
            var prefixPath = Path.GetDirectoryName(file).Replace("\\", "/");
            var modifiedContent = new List<string>(manifestContent.Length);
            foreach (var line in manifestContent.Split('\n'))
            {
                if (line.EndsWith(".m3u8") || line.EndsWith(".ts"))
                {
                    var nestedPath = $"{prefixPath}/{line}";

                    var url = await storage.GetFileUrlAsync(
                        postId,
                        nestedPath);

                    modifiedContent.Add(url);
                }
                else
                {
                    modifiedContent.Add(line);
                }
            }
            return string.Join("\n", modifiedContent);
        }
    }
}

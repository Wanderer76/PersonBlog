using Infrastructure.Services;

namespace Gateway.API.Services
{
    public static class ManifestServiceExtensions
    {
        public static async Task<string> ProcessManifestAsync(this IFileStorage storage, Guid blogId, string file)
        {
            var manifestStream = new MemoryStream();
            await storage.ReadFileAsync(blogId, $"{file}", manifestStream);
            manifestStream.Position = 0;
            var manifestContent = await new StreamReader(manifestStream).ReadToEndAsync();
            var prefixPath = Path.GetDirectoryName(file)!.Replace("\\", "/");
            var lines = manifestContent.Split('\n');
            var modifiedContent = new List<string>(lines.Length);
            foreach (var line in lines)
            {
                if (line.EndsWith(".m3u8") || line.EndsWith(".ts"))
                {
                    var nestedPath = $"{prefixPath}/{line}";

                    var url = await storage.GetFileUrlAsync(
                        blogId,
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

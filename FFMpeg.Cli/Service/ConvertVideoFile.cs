using FFmpeg.Service;
using FFmpeg.Service.Models;
using FileStorage.Service.Service;
using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Profile.Domain.Events;
using Shared.Persistence;
using Shared.Services;
using Xabe.FFmpeg;

namespace VideoProcessing.Cli.Service
{
    public class ConvertVideoFile
    {
        public static async Task ProcessFile(
            IServiceScope scope,
            IFileStorage storage,
            IEnumerable<VideoPreset> videoPresets,
            string tempPath,
            VideoUploadEvent @event)
        {
            var context = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IProfileEntity>>();
            var ffmpegService = scope.ServiceProvider.GetRequiredService<IFFMpegService>();

            var fileMetadata = await context.Get<VideoMetadata>()
                .FirstOrDefaultAsync(x => x.ObjectName == @event.ObjectName);

            if (fileMetadata == null)
            {

                throw new ArgumentException("Не удалось найти данные");
            }

            var url = await storage.GetFileUrlAsync(fileMetadata.PostId, @event.ObjectName);

            var videoSizes = new List<VideoSize>() { VideoSize.Hd1080, VideoSize.Hd720, VideoSize.Vga, VideoSize.Nhd };
            var dir = Path.Combine(tempPath, fileMetadata.Id.ToString());
            var fileId = GuidService.GetNewGuid();
            var inputUrl = new Uri(url).AbsoluteUri;
            var videoStream = await ffmpegService.GetVideoMediaInfo(inputUrl) ?? throw new ArgumentException("Не удалось найти видеопоток");
            try
            {
                Directory.CreateDirectory(dir);

                var presets = (videoStream == null
                    ? videoPresets
                    : videoPresets.Where(x => x.Width <= videoStream.Width))
                    .ToList();
                
                var hlsOptions = new HlsOptions
                {
                    Resolutions = presets.Select(x => x.GetResolution()).ToArray(),
                    Bitrates = presets.Select(x => x.VideoBitrate).ToArray(),
                    AudioBitrates = presets.Select(x => x.AudioBitrate).ToArray(),
                    SegmentFileName = fileId.ToString(),
                    MasterName = fileMetadata.Id.ToString()
                };

                await ffmpegService.CreateHls(inputUrl, dir, hlsOptions);

                foreach (var file in Directory.GetFiles(dir))
                {
                    using var fileStream = new FileStream(file, FileMode.Open);
                    using var copyStream = new MemoryStream();
                    await fileStream.CopyToAsync(copyStream);
                    copyStream.Position = 0;
                    var objectName = await storage.PutFileAsync(fileMetadata.PostId, Path.GetFileName(file), copyStream);
                }

                foreach (string folder in Directory.EnumerateDirectories(dir))
                {
                    foreach (var file in Directory.EnumerateFiles(folder))
                    {
                        using var fileStream = new FileStream(file, FileMode.Open);
                        using var copyStream = new MemoryStream();
                        await fileStream.CopyToAsync(copyStream);
                        copyStream.Position = 0;
                        var objectName = await storage.PutFileAsync(fileMetadata.PostId, GetRelativePath(file).Replace(Path.DirectorySeparatorChar, '/'), copyStream);
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
            finally
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }

            var post = await context.Get<Post>()
                       .FirstAsync(x => x.Id == fileMetadata.PostId);

            if (post.Type == PostType.Video && string.IsNullOrWhiteSpace(post.PreviewId))
            {
                var snapshotFileId = GuidService.GetNewGuid();
                var snapshotFileName = Path.Combine(tempPath, snapshotFileId.ToString() + ".png");

                try
                {
                    await ffmpegService.GeneratePreview(new Uri(url).AbsoluteUri, snapshotFileName);
                    using var fileStream = new FileStream(snapshotFileName, FileMode.Open);
                    using var copyStream = new MemoryStream();
                    await fileStream.CopyToAsync(copyStream);
                    copyStream.Position = 0;
                    var objectName = await storage.PutFileAsync(fileMetadata.PostId, snapshotFileId, copyStream);

                    context.Attach(post);
                    post.PreviewId = objectName;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    if (File.Exists(snapshotFileName))
                    {
                        File.Delete(snapshotFileName);
                    }
                }
            }

            context.Attach(fileMetadata);
            fileMetadata.IsProcessed = false;
            fileMetadata.ObjectName = $"{fileMetadata.Id}.m3u8";
            fileMetadata.Duration = videoStream.Duration;

            await context.SaveChangesAsync();
        }
        public static string GetRelativePath(string filePath)
        {
            var directoryName = Path.GetDirectoryName(filePath);

            if (directoryName != null)
            {
                string[] pathComponents = directoryName.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

                if (pathComponents.Length > 1)
                {
                    string relativePath = string.Join(Path.DirectorySeparatorChar, pathComponents.Skip(pathComponents.Length - 1));
                    string fileName = Path.GetFileName(filePath);
                    return Path.Combine(relativePath, fileName);
                }
                else
                {
                    return Path.GetFileName(filePath);
                }
            }
            else
            {
                return Path.GetFileName(filePath);
            }
        }
    }
}

using Blog.Domain.Entities;
using Blog.Domain.Events;
using FFmpeg.Service;
using FFmpeg.Service.Models;
using FileStorage.Service.Service;
using Infrastructure.Services;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;

namespace VideoProcessing.Cli.Service
{
    public class ProcessVideoToHls : IEventHandler<VideoConvertEvent>
    {
        private readonly IReadWriteRepository<IBlogEntity> _context;
        private readonly IFFMpegService _ffmpegService;
        private readonly IFileStorage _storage;
        private readonly ICacheService _cacheService;
        private readonly string _tempPath;
        private readonly HlsVideoPresets _videoPresets;

        public ProcessVideoToHls(IReadWriteRepository<IBlogEntity> context, IFFMpegService ffmpegService, IFileStorageFactory storage, IConfiguration configuration, HlsVideoPresets videoPresets, ICacheService cacheService)
        {
            _context = context;
            _ffmpegService = ffmpegService;
            _storage = storage.CreateFileStorage();
            _tempPath = Path.GetFullPath(configuration["TempDir"]!);
            _videoPresets = videoPresets;
            _cacheService = cacheService;
        }

        public async Task Handle(VideoConvertEvent @event)
        {
            var fileMetadata = await _context.Get<VideoMetadata>()
                .FirstAsync(x => x.ObjectName == @event.ObjectName);

            _context.Attach(fileMetadata);

            try
            {
                var url = await _storage.GetFileUrlAsync(fileMetadata.PostId, @event.ObjectName);
                var dir = Path.Combine(_tempPath, fileMetadata.Id.ToString());
                var fileId = GuidService.GetNewGuid();

                var inputUrl = new Uri(url).AbsoluteUri;

                var videoStream = await _ffmpegService.GetVideoMediaInfo(inputUrl) ?? throw new ArgumentException("Не удалось найти видеопоток");

                await ProcessHls(fileMetadata, dir, fileId, inputUrl, videoStream);

                var post = await _context.Get<Post>()
                           .FirstAsync(x => x.Id == fileMetadata.PostId);
                _context.Attach(post);

                if (post.Type == PostType.Video && string.IsNullOrWhiteSpace(post.PreviewId))
                {
                    var snapshotFileId = GuidService.GetNewGuid();
                    var snapshotFileName = Path.Combine(_tempPath, snapshotFileId.ToString() + ".png");

                    try
                    {
                        await _ffmpegService.GeneratePreview(new Uri(url).AbsoluteUri, snapshotFileName);
                        using var fileStream = new FileStream(snapshotFileName, FileMode.Open);
                        using var copyStream = new MemoryStream();
                        await fileStream.CopyToAsync(copyStream);
                        copyStream.Position = 0;
                        var objectName = await _storage.PutFileAsync(fileMetadata.PostId, snapshotFileId, copyStream);

                        post.PreviewId = objectName;
                        fileMetadata.IsProcessed = false;
                        fileMetadata.ObjectName = $"{fileMetadata.Id}.m3u8";
                        fileMetadata.Duration = videoStream.Duration;
                        fileMetadata.ProcessState = ProcessState.Complete;
                        post.VideoFileId = fileMetadata.Id;
                        await _context.SaveChangesAsync();
                        await _cacheService.RemoveCachedDataAsync($"PostModel:{post.Id}");
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
            }
            catch (Exception e)
            {
                fileMetadata.ProcessState = ProcessState.Error;
                fileMetadata.ErrorMessage = "Не удалось собрать файл";

                var processedEvent = await _context.Get<VideoProcessEvent>()
                    .FirstAsync(x => x.Id == @event.EventId);

                _context.Attach(processedEvent);
                processedEvent.SetErrorMessage(e.Message);
                await _context.SaveChangesAsync();
                throw;
            }
        }

        private async Task ProcessHls(VideoMetadata fileMetadata, string dir, Guid fileId, string inputUrl, FFProbeStream videoStream)
        {
            try
            {
                Directory.CreateDirectory(dir);

                var presets = (videoStream == null
                    ? _videoPresets.VideoPresets
                    : _videoPresets.VideoPresets.Where(x => x.Width <= videoStream.Width))
                    .ToList();

                var hlsOptions = new HlsOptions
                {
                    Resolutions = presets.Select(x => x.GetResolution()).ToArray(),
                    Bitrates = presets.Select(x => x.VideoBitrate).ToArray(),
                    AudioBitrates = presets.Select(x => x.AudioBitrate).ToArray(),
                    SegmentFileName = fileId.ToString(),
                    MasterName = fileMetadata.Id.ToString()
                };

                var progressCallBack = new AsyncProgress<double>((currentTime) =>
                {
                    var percent = Math.Min(100, currentTime / fileMetadata.Duration * 100);
                    Console.WriteLine($"Percent : {percent}");
                    return Task.CompletedTask;
                });

                await _ffmpegService.CreateHls(inputUrl, dir, hlsOptions, progressCallBack);

                foreach (var file in Directory.GetFiles(dir))
                {
                    using var fileStream = new FileStream(file, FileMode.Open);
                    using var copyStream = new MemoryStream();
                    await fileStream.CopyToAsync(copyStream);
                    copyStream.Position = 0;
                    var objectName = await _storage.PutFileAsync(fileMetadata.PostId, Path.GetFileName(file), copyStream);
                }

                foreach (string folder in Directory.EnumerateDirectories(dir))
                {
                    foreach (var file in Directory.EnumerateFiles(folder))
                    {
                        using var fileStream = new FileStream(file, FileMode.Open);
                        using var copyStream = new MemoryStream();
                        await fileStream.CopyToAsync(copyStream);
                        copyStream.Position = 0;
                        var objectName = await _storage.PutFileAsync(fileMetadata.PostId, GetRelativePath(file).Replace(Path.DirectorySeparatorChar, '/'), copyStream);
                    }
                }

            }
            catch (Exception e)
            {
                throw;
            }
            finally
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
        }

        private static string GetRelativePath(string filePath)
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

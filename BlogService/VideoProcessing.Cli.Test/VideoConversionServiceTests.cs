using Blog.Contracts.Events;
using Blog.Domain.Entities;
using FFmpeg.Service.Models;
using FileStorage.Service;
using FileStorage.Service.Models;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Shared.Models;
using Shared.Services;
using Shared.Utils;
using VideoProcessing.Cli.Service;

namespace VideoProcessing.Cli.Test;

public class VideoConversionServiceTests : IDisposable
{
    private readonly Mock<IVideoConvertService> _mockFfmpegService;
    private readonly Mock<IFileStorage> _mockStorage;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IVideoProgressNotifier> _mockProgressNotifier;
    private readonly HlsVideoPresets _videoPresets;
    private readonly VideoConversionService _service;
    private readonly string _tempPath;

    public VideoConversionServiceTests()
    {
        _mockFfmpegService = new Mock<IVideoConvertService>();
        _mockStorage = new Mock<IFileStorage>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockProgressNotifier = new Mock<IVideoProgressNotifier>();

        _tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempPath);

        _mockConfiguration.Setup(c => c["TempDir"]).Returns(_tempPath);
        _videoPresets = new HlsVideoPresets
        {
            VideoPresets =
            [
                new VideoPreset { Width = 1920, Height = 1080, VideoBitrate = "5000k", AudioBitrate = "192k" },
                new VideoPreset { Width = 1280, Height = 720, VideoBitrate = "3000k", AudioBitrate = "128k" },
                new VideoPreset { Width = 854, Height = 480, VideoBitrate = "1500k", AudioBitrate = "96k" }
            ],
            EncodePreset = "ultrafast"
        };

        _service = new VideoConversionService(
            _mockFfmpegService.Object,
            _mockStorage.Object,
            _mockConfiguration.Object,
            _videoPresets,
            _mockProgressNotifier.Object,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<VideoConversionService>.Instance);
    }

    #region ProcessConversionAsync Tests

    [Fact]
    public async Task ProcessConversionAsync_WithEmptyUrl_ThrowsArgumentException()
    {
        // Arrange
        var command = CreateTestCommand();
        var postId = Guid.NewGuid();
        var hasPreviewId = false;

        _mockStorage.Setup(s => s.GetFileUrlAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        // Act
        var result = await _service.ProcessConversionAsync(command, postId, hasPreviewId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProcessState.Error, result.ProcessState);
        Assert.Contains("URL не может быть пустым", result.Error);
    }

    [Fact]
    public async Task ProcessConversionAsync_WithNullUrl_ThrowsArgumentException()
    {
        // Arrange
        var command = CreateTestCommand();
        var postId = Guid.NewGuid();
        var hasPreviewId = false;

        _mockStorage.Setup(s => s.GetFileUrlAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string)null!);

        // Act
        var result = await _service.ProcessConversionAsync(command, postId, hasPreviewId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProcessState.Error, result.ProcessState);
        Assert.Contains("URL не может быть пустым", result.Error);
    }

    [Fact]
    public async Task ProcessConversionAsync_WhenGetVideoMediaInfoReturnsNull_ThrowsArgumentException()
    {
        // Arrange
        var command = CreateTestCommand();
        var postId = Guid.NewGuid();
        var hasPreviewId = false;
        var testUrl = "https://test.com/video.mp4";

        _mockStorage.Setup(s => s.GetFileUrlAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testUrl);

        _mockFfmpegService.Setup(f => f.GetVideoMediaInfoAsync(testUrl))
            .ReturnsAsync((VideoMediaInfo?)null);

        // Act
        var result = await _service.ProcessConversionAsync(command, postId, hasPreviewId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProcessState.Error, result.ProcessState);
        Assert.Contains("Не удалось найти видеопоток в видеофайле", result.Error);
    }

    [Fact]
    public async Task ProcessConversionAsync_WithValidInput_NoPreview_CompletesSuccessfully()
    {
        // Arrange
        var command = CreateTestCommand();
        var postId = Guid.NewGuid();
        var hasPreviewId = true; // Превью уже существует
        var testUrl = "https://test.com/video.mp4";
        var videoStream = new VideoMediaInfo(
            Width: 1920,
            Height: 1080,
            Duration: 120.5,
            CodecType: "video",
            CodecName: "h264",
            BitRate: 32132132
            );

        _mockStorage.Setup(s => s.GetFileUrlAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testUrl);

        _mockFfmpegService.Setup(f => f.GetVideoMediaInfoAsync(testUrl))
            .ReturnsAsync(videoStream);

        _mockFfmpegService.Setup(f => f.CreateHlsAsync(
                It.Is<string>(url => url == testUrl),
                It.IsAny<string>(),
                It.IsAny<HlsOptions>(),
                It.IsAny<AsyncProgress<double>>()))
            .Callback<string, string, HlsOptions, AsyncProgress<double>?>((_, output, options, _) =>
            {
                File.WriteAllText(Path.Combine(output, $"{options.MasterName}.m3u8"), "#EXTM3U");
            })
            .Returns(Task.CompletedTask);

        // Setup for file upload
        _mockStorage.Setup(s => s.PutFileAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid bucketId, string objectName, Stream stream, CancellationToken cancellationToken) => objectName);

        // Act
        var result = await _service.ProcessConversionAsync(command, postId, hasPreviewId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProcessState.Complete, result.ProcessState);
        Assert.False(result.IsProcessing);
        Assert.Equal(postId, result.PostId);
        Assert.Equal(command.VideoMetadataId, result.VideoMetadataId);
        Assert.Equal(videoStream.Duration, result.Duration);
        Assert.EndsWith(".m3u8", result.ObjectName);
        Assert.Null(result.PreviewId);
    }

    [Fact]
    public async Task ProcessConversionAsync_WithValidInput_WithoutPreview_GeneratesPreviewAndCompletes()
    {
        // Arrange
        var command = CreateTestCommand();
        var postId = Guid.NewGuid();
        var hasPreviewId = false;
        var testUrl = "https://test.com/video.mp4";
        var videoStream = new VideoMediaInfo(
            Width: 1920,
            Height: 1080,
            Duration: 120.5,
            CodecType: "video",
            CodecName: "h264",
            BitRate: 32132132
            );

        _mockStorage.Setup(s => s.GetFileUrlAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testUrl);

        _mockFfmpegService.Setup(f => f.GetVideoMediaInfoAsync(testUrl))
            .ReturnsAsync(videoStream);

        // Setup preview generation - create a temp file
        _mockFfmpegService.Setup(f => f.GeneratePreviewAsync(testUrl, It.IsAny<string>()))
            .Callback<string, string>((input, output) =>
            {
                Assert.Equal(".png", Path.GetExtension(output));
                Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                File.WriteAllText(output, "fake image content");
            })
            .Returns(Task.CompletedTask);

        _mockFfmpegService.Setup(f => f.CreateHlsAsync(
                It.Is<string>(url => url == testUrl),
                It.IsAny<string>(),
                It.IsAny<HlsOptions>(),
                It.IsAny<AsyncProgress<double>>()))
            .Callback<string, string, HlsOptions, AsyncProgress<double>?>((_, output, options, _) =>
            {
                File.WriteAllText(Path.Combine(output, $"{options.MasterName}.m3u8"), "#EXTM3U");
            })
            .Returns(Task.CompletedTask);

        _mockStorage.Setup(s => s.PutFileAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid bucketId, string objectName, Stream stream, CancellationToken cancellationToken) => objectName);

        // Act
        var result = await _service.ProcessConversionAsync(command, postId, hasPreviewId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProcessState.Complete, result.ProcessState);
        Assert.False(result.IsProcessing);
        Assert.NotNull(result.PreviewId);
        Assert.Equal(".png", result.PreviewId.FileExtension);
        Assert.Equal("image/png", result.PreviewId.ContentType);
        Assert.False(Path.IsPathRooted(result.PreviewId.Name));
    }

    [Fact]
    public async Task ProcessConversionAsync_WhenFfmpegException_Occurs_ReturnsErrorState()
    {
        // Arrange
        var command = CreateTestCommand();
        var postId = Guid.NewGuid();
        var hasPreviewId = true;
        var testUrl = "https://test.com/video.mp4";
        var expectedError = "FFmpeg processing failed";

        _mockStorage.Setup(s => s.GetFileUrlAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testUrl);

        _mockFfmpegService.Setup(f => f.GetVideoMediaInfoAsync(testUrl))
            .ThrowsAsync(new InvalidOperationException(expectedError));

        // Act
        var result = await _service.ProcessConversionAsync(command, postId, hasPreviewId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProcessState.Error, result.ProcessState);
        Assert.Contains(expectedError, result.Error);
    }

    [Fact]
    public async Task ProcessConversionAsync_WhenFfmpegProducesNoMasterPlaylist_ReturnsErrorState()
    {
        var command = CreateTestCommand();
        var testUrl = "https://test.com/video.mp4";
        var videoStream = new VideoMediaInfo("h264", 1080, 1920, "video", 120.5, 3_000_000);

        _mockStorage.Setup(s => s.GetFileUrlAsync(command.BlogId, command.ObjectName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testUrl);
        _mockFfmpegService.Setup(f => f.GetVideoMediaInfoAsync(testUrl))
            .ReturnsAsync(videoStream);
        _mockFfmpegService.Setup(f => f.CreateHlsAsync(
                testUrl,
                It.IsAny<string>(),
                It.IsAny<HlsOptions>(),
                It.IsAny<AsyncProgress<double>>()))
            .Returns(Task.CompletedTask);

        var result = await _service.ProcessConversionAsync(command, command.PostId, hasPreviewId: true);

        Assert.Equal(ProcessState.Error, result.ProcessState);
        Assert.Contains("master HLS playlist", result.Error);
        _mockStorage.Verify(
            x => x.PutFileAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessConversionAsync_WhenStorageException_Occurs_ReturnsErrorState()
    {
        // Arrange
        var command = CreateTestCommand();
        var postId = Guid.NewGuid();
        var hasPreviewId = true;
        var expectedError = "Storage connection failed";

        _mockStorage.Setup(s => s.GetFileUrlAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(expectedError));

        // Act
        var result = await _service.ProcessConversionAsync(command, postId, hasPreviewId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProcessState.Error, result.ProcessState);
        Assert.Contains(expectedError, result.Error);
    }

    #endregion

    #region Helper Methods

    private static ConvertVideoCommand CreateTestCommand()
    {
        return new ConvertVideoCommand
        {
            BlogId = Guid.NewGuid(),
            PostId = Guid.NewGuid(),
            VideoMetadataId = Guid.NewGuid(),
            ObjectName = "test-video.mp4",
            HasPreviewId = false,
            VideoMetadata = new VideoFile
            {
                Id = Guid.NewGuid(),
                Name = "test-video.mp4",
                FileExtension = ".mp4",
                Length = 1024000,
                ContentType = "video/mp4",
                CreatedAt = DateTimeOffset.Now,
                ObjectName = "test-video.mp4",
                Duration = 120.5,
                PostId = Guid.NewGuid()
            }
        };
    }

    #endregion

    #region Dispose

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempPath))
            {
                Directory.Delete(_tempPath, true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    #endregion
}

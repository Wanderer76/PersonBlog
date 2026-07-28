using Blog.Contracts.Events;
using Blog.Domain.Entities;
using FFmpeg.Service.Models;
using Moq;
using VideoProcessing.Cli.Service;

namespace VideoProcessing.Cli.Tests.Service
{
    /// <summary>
    /// Тесты для обработки конвертации видео в HLS
    /// </summary>
    public class ProcessVideoToHlsTests
    {
        private readonly Mock<IVideoConvertService> _ffmpegMock = new();
        private readonly Mock<IFileStorage> _storageMock = new();
        private readonly Mock<Infrastructure.Services.ICacheService> _cacheMock = new();
        private readonly Mock<Infrastructure.Services.ICurrentUserService> _userMock = new();

        [Fact]
        public async Task Handle_ConvertsVideo_Successfully()
        {
            // Arrange
            var videoMetadataId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            var blogId = Guid.NewGuid();

            var videoFile = new VideoFile
            {
                Id = videoMetadataId,
                PostId = postId,
                Resolution = VideoResolution.FullHD,
                Duration = 120.5
            };

            var command = new ConvertVideoCommand
            {
                BlogId = blogId,
                PostId = postId,
                VideoMetadataId = videoMetadataId,
                ObjectName = "videos/source_video.mp4",
                HasPreviewId = false,
                VideoMetadata = videoFile
            };

            var eventContext = new Mock<IMessageContext<ConvertVideoCommand>>();
            eventContext.Setup(x => x.Message).Returns(command);

            _cacheMock.Setup(x => x.SetCachedDataAsync(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);

            var service = new ProcessVideoToHls(
                _ffmpegMock.Object,
                _storageMock.CreateFileStorage(),
                new Mock<IConfiguration>().Object,
                new HlsVideoPresets { EncodePreset = "ultrafast" });

            // Act
            var result = await service.Handle(eventContext.Object);

            // Assert
            Assert.False(result.IsProcessing);
            Assert.Equal(ProcessState.Complete, result.ProcessState);
            Assert.Equal(videoMetadataId, result.VideoMetadataId);
            Assert.Equal(postId, result.PostId);
        }

        [Fact]
        public async Task Handle_SkipsPreview_WhenAlreadyExists()
        {
            // Arrange
            var videoMetadataId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            var blogId = Guid.NewGuid();

            var videoFile = new VideoFile
            {
                Id = videoMetadataId,
                PostId = postId,
                Resolution = VideoResolution.FullHD,
                Duration = 120.5
            };

            var command = new ConvertVideoCommand
            {
                BlogId = blogId,
                PostId = postId,
                VideoMetadataId = videoMetadataId,
                ObjectName = "videos/source_video.mp4",
                HasPreviewId = true,  // Превью уже существует
                VideoMetadata = videoFile
            };

            var eventContext = new Mock<IMessageContext<ConvertVideoCommand>>();
            eventContext.Setup(x => x.Message).Returns(command);

            var service = new ProcessVideoToHls(
                _ffmpegMock.Object,
                _storageMock.CreateFileStorage(),
                new Mock<IConfiguration>().Object,
                new HlsVideoPresets { EncodePreset = "ultrafast" });

            // Act
            var result = await service.Handle(eventContext.Object);

            // Assert
            Assert.False(result.IsProcessing);
            Assert.Null(result.PreviewId);  // Превью не создается, так как уже существует
        }

        [Fact]
        public async Task Handle_ProcessesPreview_WhenNotExists()
        {
            // Arrange
            var videoMetadataId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            var blogId = Guid.NewGuid();
            var previewId = new Shared.Models.BaseFileMetadataEntity();

            var videoFile = new VideoFile
            {
                Id = videoMetadataId,
                PostId = postId,
                Resolution = VideoResolution.FullHD,
                Duration = 120.5
            };

            var command = new ConvertVideoCommand
            {
                BlogId = blogId,
                PostId = postId,
                VideoMetadataId = videoMetadataId,
                ObjectName = "videos/source_video.mp4",
                HasPreviewId = false,  // Превью не существует
                VideoMetadata = videoFile
            };

            var eventContext = new Mock<IMessageContext<ConvertVideoCommand>>();
            eventContext.Setup(x => x.Message).Returns(command);

            _cacheMock.Setup(x => x.SetCachedDataAsync(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);

            var service = new ProcessVideoToHls(
                _ffmpegMock.Object,
                _storageMock.CreateFileStorage(),
                new Mock<IConfiguration>().Object,
                new HlsVideoPresets { EncodePreset = "ultrafast" });

            // Act
            var result = await service.Handle(eventContext.Object);

            // Assert
            Assert.False(result.IsProcessing);
            Assert.NotNull(result.PreviewId);
        }

        [Fact]
        public async Task Handle_Fails_WhenVideoStreamNotFound()
        {
            // Arrange
            var videoMetadataId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            var blogId = Guid.NewGuid();

            var videoFile = new VideoFile
            {
                Id = videoMetadataId,
                PostId = postId,
                Resolution = VideoResolution.FullHD,
                Duration = 120.5
            };

            var command = new ConvertVideoCommand
            {
                BlogId = blogId,
                PostId = postId,
                VideoMetadataId = videoMetadataId,
                ObjectName = "videos/source_video.mp4",
                HasPreviewId = false,
                VideoMetadata = videoFile
            };

            var eventContext = new Mock<IMessageContext<ConvertVideoCommand>>();
            eventContext.Setup(x => x.Message).Returns(command);

            // Simulate video stream not found
            _ffmpegMock.Setup(x => x.GetVideoMediaAsync(It.IsAny<string>()))
                .ReturnsAsync((FFmpeg.Service.Models.FFProbeStream?)null);

            var service = new ProcessVideoToHls(
                _ffmpegMock.Object,
                _storageMock.CreateFileStorage(),
                new Mock<IConfiguration>().Object,
                new HlsVideoPresets { EncodePreset = "ultrafast" });

            // Act
            var result = await service.Handle(eventContext.Object);

            // Assert
            Assert.True(result.IsProcessing);
            Assert.Equal(ProcessState.Error, result.ProcessState);
            Assert.Contains("Не удалось найти видеопоток", result.Error);
        }

        [Fact]
        public async Task Handle_Fails_WithErrorMessage()
        {
            // Arrange
            var videoMetadataId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            var blogId = Guid.NewGuid();

            var videoFile = new VideoFile
            {
                Id = videoMetadataId,
                PostId = postId,
                Resolution = VideoResolution.FullHD,
                Duration = 120.5
            };

            var command = new ConvertVideoCommand
            {
                BlogId = blogId,
                PostId = postId,
                VideoMetadataId = videoMetadataId,
                ObjectName = "videos/source_video.mp4",
                HasPreviewId = false,
                VideoMetadata = videoFile
            };

            var eventContext = new Mock<IMessageContext<ConvertVideoCommand>>();
            eventContext.Setup(x => x.Message).Returns(command);

            _ffmpegMock.Setup(x => x.GetVideoMediaAsync(It.IsAny<string>()))
                .ReturnsAsync(new FFProbeStream { Width = 1920, Height = 1080 });

            // Simulate storage error
            _storageMock.Setup(x => x.GetFileUrlAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Storage unavailable"));

            var service = new ProcessVideoToHls(
                _ffmpegMock.Object,
                _storageMock.CreateFileStorage(),
                new Mock<IConfiguration>().Object,
                new HlsVideoPresets { EncodePreset = "ultrafast" });

            // Act
            var result = await service.Handle(eventContext.Object);

            // Assert
            Assert.True(result.IsProcessing);
            Assert.Equal(ProcessState.Error, result.ProcessState);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public async Task Handle_PublishesResponseEvent()
        {
            // Arrange
            var videoMetadataId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            var blogId = Guid.NewGuid();

            var response = new VideoConvertedResponse
            {
                PostId = postId,
                VideoMetadataId = videoMetadataId,
                ObjectName = "videos/output.m3u8",
                Duration = 120.5,
                ProcessState = ProcessState.Complete,
                IsProcessing = false
            };

            var eventContext = new Mock<IMessageContext<ConvertVideoCommand>>();
            eventContext.Setup(x => x.Message).Returns(new ConvertVideoCommand { VideoMetadataId = videoMetadataId });
            eventContext.Setup(x => x.PublishAsync(It.IsAny<BaseEvent<VideoConvertedResponse>>(), It.IsAny<Dictionary<string, object>>()))
                .Returns(Task.CompletedTask);

            var service = new ProcessVideoToHls(
                _ffmpegMock.Object,
                _storageMock.CreateFileStorage(),
                new Mock<IConfiguration>().Object,
                new HlsVideoPresets { EncodePreset = "ultrafast" });

            // Act
            await service.Handle(eventContext.Object);

            // Assert
            eventContext.Verify(x => x.PublishAsync(It.IsAny<BaseEvent<VideoConvertedResponse>>(), It.IsAny<Dictionary<string, object>>()), Times.Once);
        }
    }
}

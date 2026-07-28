using FFmpeg.Service.Models;

namespace VideoProcessing.Cli.Tests.Integration
{
    /// <summary>
    /// Интеграционные тесты для HLS presets конфигурации
    /// </summary>
    public class HlsPresetsTests
    {
        [Fact]
        public void VideoPreset_CreatesResolution_String_Correctly()
        {
            // Arrange
            var preset = new VideoPreset
            {
                Width = 1920,
                Height = 1080,
                VideoBitrate = "5M",
                AudioBitrate = "96k"
            };

            // Act
            var resolution = preset.GetResolution();

            // Assert
            Assert.Equal("1920x1080", resolution);
        }

        [Fact]
        public void VideoPreset_CreatesResolution_String_Different_Resolution()
        {
            // Arrange
            var preset = new VideoPreset
            {
                Width = 854,
                Height = 480,
                VideoBitrate = "1.5M",
                AudioBitrate = "64k"
            };

            // Act
            var resolution = preset.GetResolution();

            // Assert
            Assert.Equal("854x480", resolution);
        }

        [Fact]
        public void HlsOptions_CreatesValidConfiguration()
        {
            // Arrange
            var presets = new List<VideoPreset>
            {
                new VideoPreset { Width = 1920, Height = 1080, VideoBitrate = "5M", AudioBitrate = "96k" },
                new VideoPreset { Width = 1280, Height = 720, VideoBitrate = "3M", AudioBitrate = "96k" }
            };

            // Act
            var hlsOptions = new HlsOptions
            {
                Resolutions = [.. presets.Select(x => x.GetResolution())],
                Bitrates = [.. presets.Select(x => x.VideoBitrate)],
                AudioBitrates = [.. presets.Select(x => x.AudioBitrate)]
            };

            // Assert
            Assert.NotNull(hlsOptions);
            Assert.Equal(2, hlsOptions.Resolutions.Count);
            Assert.Equal("1920x1080", hlsOptions.Resolutions[0]);
            Assert.Equal("5M", hlsOptions.Bitrates[0]);
            Assert.Equal("96k", hlsOptions.AudioBitrates[0]);
        }

        [Fact]
        public void HlsVideoPresets_WithMultipleResolutions_CreatesValidConfiguration()
        {
            // Arrange - Standard HLS presets
            var videoPresets = new List<VideoPreset>
            {
                new VideoPreset { Width = 1920, Height = 1080, VideoBitrate = "5M", AudioBitrate = "96k" },   // Full HD
                new VideoPreset { Width = 1280, Height = 720, VideoBitrate = "3M", AudioBitrate = "96k" },   // SD
                new VideoPreset { Width = 854,  Height = 480,  VideoBitrate = "1.5M", AudioBitrate = "64k" }, // Mobile HD
                new VideoPreset { Width = 640,  Height = 360,  VideoBitrate = "1M",   AudioBitrate = "48k" }  // Low quality
            };

            // Act
            var presets = new HlsVideoPresets
            {
                VideoPresets = videoPresets,
                EncodePreset = "medium"
            };

            // Assert
            Assert.NotNull(presets);
            Assert.Equal(4, presets.VideoPresets.Count);
            Assert.Equal("medium", presets.EncodePreset);
        }

        [Fact]
        public void HlsOptions_WithSingleResolution_CreatesValidConfiguration()
        {
            // Arrange - Single resolution preset
            var singlePreset = new VideoPreset
            {
                Width = 1920,
                Height = 1080,
                VideoBitrate = "5M",
                AudioBitrate = "96k"
            };

            // Act
            var hlsOptions = new HlsOptions
            {
                Resolutions = [singlePreset.GetResolution()],
                Bitrates = [singlePreset.VideoBitrate],
                AudioBitrates = [singlePreset.AudioBitrate]
            };

            // Assert
            Assert.Single(hlsOptions.Resolutions);
            Assert.Single(hlsOptions.Bitrates);
            Assert.Equal("1920x1080", hlsOptions.Resolutions[0]);
        }

        [Fact]
        public void HlsOptions_MapsVideoBitrate_ToBitrates_Accurately()
        {
            // Arrange
            var preset = new VideoPreset
            {
                Width = 1280,
                Height = 720,
                VideoBitrate = "3M",
                AudioBitrate = "96k"
            };

            // Act
            var hlsOptions = new HlsOptions
            {
                Resolutions = [preset.GetResolution()],
                Bitrates = [preset.VideoBitrate],
                AudioBitrates = [preset.AudioBitrate]
            };

            // Assert
            Assert.Equal("3M", hlsOptions.Bitrates[0]);
            Assert.Equal("96k", hlsOptions.AudioBitrates[0]);
        }

        [Theory]
        [InlineData(1920, 1080)]
        [InlineData(1280, 720)]
        [InlineData(854, 480)]
        [InlineData(640, 360)]
        [InlineData(256, 144)]
        public void VideoPreset_WithStandardResolutions_CreatesValidFormat(int width, int height)
        {
            // Arrange
            var preset = new VideoPreset
            {
                Width = width,
                Height = height,
                VideoBitrate = $"{width / 200}M",
                AudioBitrate = "96k"
            };

            // Act
            var resolution = preset.GetResolution();

            // Assert
            Assert.Equal($"{width}x{height}", resolution);
        }

        [Fact]
        public void VideoPreset_WithCustomBitrates_CreatesValidConfiguration()
        {
            // Arrange
            var preset = new VideoPreset
            {
                Width = 1920,
                Height = 1080,
                VideoBitrate = "4M",
                AudioBitrate = "128k"
            };

            // Act
            var hlsOptions = new HlsOptions
            {
                Resolutions = [preset.GetResolution()],
                Bitrates = [preset.VideoBitrate],
                AudioBitrates = [preset.AudioBitrate]
            };

            // Assert
            Assert.Equal("4M", hlsOptions.Bitrates[0]);
            Assert.Equal("128k", hlsOptions.AudioBitrates[0]);
        }

        [Fact]
        public void HlsOptions_WithMultiplePreset_CreatesFullHlsConfiguration()
        {
            // Arrange - Multiple quality levels
            var presets = new List<VideoPreset>
            {
                new VideoPreset { Width = 1920, Height = 1080, VideoBitrate = "5M", AudioBitrate = "96k" },
                new VideoPreset { Width = 1280, Height = 720, VideoBitrate = "3M", AudioBitrate = "96k" },
                new VideoPreset { Width = 640,  Height = 360,  VideoBitrate = "1M", AudioBitrate = "48k" }
            };

            // Act
            var hlsOptions = new HlsOptions
            {
                Resolutions = [.. presets.Select(x => x.GetResolution())],
                Bitrates = [.. presets.Select(x => x.VideoBitrate)],
                AudioBitrates = [.. presets.Select(x => x.AudioBitrate)]
            };

            // Assert
            Assert.NotNull(hlsOptions);
            Assert.Equal(3, hlsOptions.Resolutions.Count);
            Assert.Equal(3, hlsOptions.Bitrates.Count);
            Assert.Equal(3, hlsOptions.AudioBitrates.Count);
        }

        [Fact]
        public void HlsVideoPresets_EmptyVideoPresets_CreatesValidConfiguration()
        {
            // Arrange
            // Act
            var presets = new HlsVideoPresets { EncodePreset = "ultrafast" };

            // Assert
            Assert.NotNull(presets);
            Assert.Equal(0, presets.VideoPresets.Count);
            Assert.Equal("ultrafast", presets.EncodePreset);
        }

        [Fact]
        public void VideoPreset_WithVeryHighResolution_CreatesValidFormat()
        {
            // Arrange - 4K resolution
            var preset = new VideoPreset
            {
                Width = 3840,
                Height = 2160,
                VideoBitrate = "25M",
                AudioBitrate = "192k"
            };

            // Act
            var resolution = preset.GetResolution();

            // Assert
            Assert.Equal("3840x2160", resolution);
        }
    }
}

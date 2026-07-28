using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using VideoProcessing.Cli.Service;

namespace VideoProcessing.Cli.Tests.Service
{
    /// <summary>
    /// Тесты для метода валидации URL (SSRF защита)
    /// </summary>
    public class ValidateUriTests
    {
        private readonly object _lock = new();

        [Fact]
        public void ValidateUri_ForEmptyString_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => ValidateUriForHls(string.Empty));
            Assert.Throws<ArgumentException>(() => ValidateUriForHls(null!));
        }

        [Fact]
        public void ValidateUri_ForValidHttpsUrl_ReturnsValidUri()
        {
            // Arrange
            var testUrl = "https://example.com/video.mp4";

            // Act
            var result = ValidateUriForHls(testUrl);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("https", result.Scheme);
            Assert.Equal("example.com", result.Host);
        }

        [Fact]
        public void ValidateUri_ForValidHttpUrl_ReturnsValidUri()
        {
            // Arrange
            var testUrl = "http://storage.example.com/bucket/video.mp4";

            // Act
            var result = ValidateUriForHls(testUrl);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("http", result.Scheme);
        }

        [Fact]
        public void ValidateUri_ForFileScheme_ReturnsValidUri()
        {
            // Arrange
            var testUrl = "file:///path/to/video.mp4";

            // Act
            var result = ValidateUriForHls(testUrl);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("file", result.Scheme);
        }

        [Fact]
        public void ValidateUri_ForInvalidScheme_ThrowsSecurityException()
        {
            // Arrange
            var testUrl = "ftp://example.com/video.mp4";

            // Act & Assert
            Assert.Throws<SecurityException>(() => ValidateUriForHls(testUrl));
        }

        [Fact]
        public void ValidateUri_ForLanIp_ThrowsSecurityException()
        {
            // Arrange
            var testUrls = new[]
            {
                "http://192.168.1.100/video.mp4",
                "https://10.0.0.50/storage/file.mp4",
                "http://172.16.0.1/bucket/data.mp4"
            };

            // Act & Assert
            foreach (var url in testUrls)
            {
                Assert.Throws<SecurityException>(() => ValidateUriForHls(url));
            }
        }

        [Fact]
        public void ValidateUri_ForLocalhost_ThrowsSecurityException()
        {
            // Arrange
            var testUrls = new[]
            {
                "http://localhost/video.mp4",
                "https://127.0.0.1/storage/file.mp4",
                "http://[::1]/bucket/data.mp4"
            };

            // Act & Assert
            foreach (var url in testUrls)
            {
                Assert.Throws<SecurityException>(() => ValidateUriForHls(url));
            }
        }

        [Fact]
        public void ValidateUri_ForCidrRange_ThrowsSecurityException()
        {
            // Arrange - CDRanges должны быть заблокированы
            var testUrls = new[]
            {
                "http://10.255.255.255/video.mp4",
                "http://192.168.255.255/file.mp4",
                "https://172.31.255.255/data.mp4"
            };

            // Act & Assert
            foreach (var url in testUrls)
            {
                var exception = Assert.Throws<SecurityException>(() => ValidateUriForHls(url));
                Assert.Contains("приватным", exception.Message);
            }
        }

        [Fact]
        public void ValidateUri_ForPrivateIpv6_ThrowsSecurityException()
        {
            // Arrange
            var testUrls = new[]
            {
                "http://[fc00:db8::1]/video.mp4",  // Unique local (FC00::/7)
                "http://[fe80::1]/file.mp4",       // Link-local (FE80::/10)
                "http://[fd12:3456:789a:bcde::1]/data.mp4"  // Unique local
            };

            // Act & Assert
            foreach (var url in testUrls)
            {
                Assert.Throws<SecurityException>(() => ValidateUriForHls(url));
            }
        }

        [Fact]
        public void ValidateUri_ForMulticastRange_ThrowsSecurityException()
        {
            // Arrange - Multicast range 224.0.0.0/4 должен быть заблокирован
            var testUrls = new[]
            {
                "http://224.0.0.1/video.mp4",
                "https://239.255.255.255/file.mp4"
            };

            // Act & Assert
            foreach (var url in testUrls)
            {
                Assert.Throws<SecurityException>(() => ValidateUriForHls(url));
            }
        }

        [Fact]
        public void ValidateUri_ForValidPublicDomain_ReturnsUri()
        {
            // Arrange
            var testUrls = new[]
            {
                "http://www.example.com/video.mp4",
                "https://cdn.example.org/storage/file.mp4"
            };

            // Act & Assert
            foreach (var url in testUrls)
            {
                Assert.DoesNotThrow(() => ValidateUriForHls(url));
            }
        }

        [Fact]
        public void ValidateUri_ForUrlWithPort_ThrowsSecurityException()
        {
            // Arrange - URL с портом должны быть заблокированы как потенциальный SSRF вектор
            var testUrls = new[]
            {
                "http://localhost:8080/video.mp4",
                "https://192.168.1.1:5000/file.mp4"
            };

            // Act & Assert
            foreach (var url in testUrls)
            {
                var exception = Assert.Throws<SecurityException>(() => ValidateUriForHls(url));
                Assert.Contains("приватным", exception.Message);
            }
        }

        [Fact]
        public void ValidateUri_ForValidDomainWithPort_ReturnsUri()
        {
            // Arrange - Публичные домены с портом должны быть разрешены (если это CDN)
            var testUrl = "http://cdn.example.com:443/video.mp4";

            // Act & Assert
            var result = ValidateUriForHls(testUrl);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("http", result.Scheme);
            Assert.Equal("cdn.example.com", result.Host);
        }

        [Fact]
        public void ValidateUri_ForLongHost_ReturnsUri_IfLengthValid()
        {
            // Arrange - Максимальная длина DNS хоста 253 символа
            var longDomain = string.Concat(Enumerable.Repeat("a", 250));
            var testUrl = $"http://{longDomain}/video.mp4";

            // Act & Assert
            var result = ValidateUriForHls(testUrl);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(longDomain, result.Host);
        }

        [Fact]
        public void ValidateUri_ForTooLongHost_ThrowsArgumentException()
        {
            // Arrange - URL с хостом более 253 символов
            var longDomain = string.Concat(Enumerable.Repeat("a", 260));
            var testUrl = $"http://{longDomain}/video.mp4";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => ValidateUriForHls(testUrl));
        }

        [Fact]
        public void ValidateUri_ForIpAddressWithPort_ThrowsSecurityException()
        {
            // Arrange
            var testUrls = new[]
            {
                "http://192.168.1.100:3000/video.mp4",
                "https://10.0.0.50:8080/file.mp4"
            };

            // Act & Assert
            foreach (var url in testUrls)
            {
                var exception = Assert.Throws<SecurityException>(() => ValidateUriForHls(url));
                Assert.Contains("приватным", exception.Message);
            }
        }
    }
}

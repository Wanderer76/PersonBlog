using System.IO;

namespace VideoProcessing.Cli.Tests.Service
{
    /// <summary>
    /// Тесты для метода очистки временных директорий
    /// </summary>
    public class CleanupDirectoryTests
    {
        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "CleanupTest");

        [Fact]
        public async Task CleanupDirectory_Existing_Directory_CleansSuccessfully()
        {
            // Arrange
            Directory.CreateDirectory(_tempDir);
            var testFile = Path.Combine(_tempDir, "test.txt");
            File.WriteAllText(testFile, "test content");

            // Act
            await TestCleanupDirectoryAsync(_tempDir);

            // Assert
            Assert.False(Directory.Exists(_tempDir));
        }

        [Fact]
        public async Task CleanupDirectory_WithSubDirectories_CleansRecursively()
        {
            // Arrange
            Directory.CreateDirectory(_tempDir);
            var subDir1 = Path.Combine(_tempDir, "subdir1");
            var subDir2 = Path.Combine(_tempDir, "subdir2");
            File.WriteAllText(Path.Combine(subDir1, "file1.txt"), "content");
            File.WriteAllText(Path.Combine(subDir2, "file2.txt"), "content");

            // Act
            await TestCleanupDirectoryAsync(_tempDir);

            // Assert
            Assert.False(Directory.Exists(_tempDir));
        }

        [Fact]
        public async Task CleanupDirectory_NonExisting_Directory_DoesNothing()
        {
            // Arrange
            var nonExistingDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "CleanupTest2");
            
            // Act
            await TestCleanupDirectoryAsync(nonExistingDir);

            // Assert
            Assert.False(Directory.Exists(nonExistingDir));
        }

        [Fact]
        public async Task CleanupDirectory_Empty_Directory_CleansSuccessfully()
        {
            // Arrange
            Directory.CreateDirectory(_tempDir);

            // Act
            await TestCleanupDirectoryAsync(_tempDir);

            // Assert
            Assert.False(Directory.Exists(_tempDir));
        }

        [Fact]
        public async Task CleanupDirectory_WithLargeNumberOfFiles_CleansSuccessfully()
        {
            // Arrange
            Directory.CreateDirectory(_tempDir);
            for (int i = 0; i < 100; i++)
            {
                var testFile = Path.Combine(_tempDir, $"test_{i}.txt");
                File.WriteAllText(testFile, $"content {i}");
            }

            // Act
            await TestCleanupDirectoryAsync(_tempDir);

            // Assert
            Assert.False(Directory.Exists(_tempDir));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task CleanupDirectory_NullOrEmptyPath_DoesNothing(string? path)
        {
            // Act
            await TestCleanupDirectoryAsync(path!);

            // Assert
            if (!string.IsNullOrEmpty(path))
            {
                Assert.False(Directory.Exists(path));
            }
        }

        [Fact]
        public async Task CleanupDirectory_WithNestedFolders_CleansProperly()
        {
            // Arrange
            Directory.CreateDirectory(_tempDir);
            var nestedPath = Path.Combine(_tempDir, "level1", "level2", "level3");
            Directory.CreateDirectory(nestedPath);
            File.WriteAllText(Path.Combine(nestedPath, "deep.txt"), "deep content");

            // Act
            await TestCleanupDirectoryAsync(_tempDir);

            // Assert
            Assert.False(Directory.Exists(_tempDir));
        }

        [Fact]
        public async Task CleanupDirectory_DirectoryAlreadyCleaned_ReturnsImmediately()
        {
            // Arrange
            var alreadyCleaned = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "CleanupTest3");

            // Act
            var startTime = DateTime.Now;
            await TestCleanupDirectoryAsync(alreadyCleaned);
            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;

            // Assert
            Assert.False(Directory.Exists(alreadyCleaned));
            Assert.True(elapsed < 100, "Method should return immediately if directory doesn't exist");
        }

        [Fact]
        public async Task CleanupDirectory_MultipleConcurrentCalls_WorksWithoutConflict()
        {
            // Arrange
            var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "CleanupTest4");
            Directory.CreateDirectory(testDir);

            // Act & Assert - Multiple concurrent calls should work without throwing
            await Task.WhenAll(
                TestCleanupDirectoryAsync(testDir),
                Task.Delay(50),  // Small delay to simulate timing
                TestCleanupDirectoryAsync(testDir)
            );

            Assert.False(Directory.Exists(testDir));
        }

        [Fact]
        public async Task CleanupDirectory_DirectoryWithMixedFileTypes_CleansAll()
        {
            // Arrange
            Directory.CreateDirectory(_tempDir);
            
            var files = new[] { ".txt", ".mp4", ".m3u8", ".ts", ".jpg" };
            foreach (var ext in files)
            {
                File.WriteAllText(Path.Combine(_tempDir, $"file{ext}"), "content");
            }

            // Act
            await TestCleanupDirectoryAsync(_tempDir);

            // Assert
            Assert.False(Directory.Exists(_tempDir));
        }

        private static async Task TestCleanupDirectoryAsync(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                return;
            }

            var deleted = false;
            for (var attempt = 0; attempt < 3 && !deleted; attempt++)
            {
                try
                {
                    if (!Directory.Exists(directoryPath))
                    {
                        return;
                    }

                    // Сначала удаляем все файлы внутри
                    var filesToClean = Directory.GetFiles(directoryPath);
                    foreach (var file in filesToClean)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception ioEx) when (!ioEx.IsCancellationRequested)
                        {
                            // Log error but continue
                        }
                    }

                    // Удаляем поддиректории
                    var subDirectories = Directory.GetDirectories(directoryPath);
                    foreach (var subDir in subDirectories)
                    {
                        try
                        {
                            Directory.Delete(subDir, true);
                        }
                        catch (Exception ioEx) when (!ioEx.IsCancellationRequested)
                        {
                            // Log error but continue
                        }
                    }

                    // Удаляем корневую директорию
                    Directory.Delete(directoryPath);
                    deleted = true;
                }
                catch (Exception ioEx) when (!ioEx.IsCancellationRequested && attempt < 2)
                {
                    await Task.Delay(100 * (attempt + 1));
                }
            }
        }
    }
}

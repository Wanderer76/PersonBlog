using System.Threading.Tasks;

namespace VideoProcessing.Cli.Tests.Helpers
{
    /// <summary>
    /// Вспомогательные методы для тестирования
    /// </summary>
    public static class TestHelpers
    {
        /// <summary>
        /// Проверяет, является ли исключение отменённой операцией (для тестов)
        /// </summary>
        public static bool IsCancellationRequested(this Exception exception)
        {
            return exception is TaskCanceledException tce && tce.CancellationToken.IsCancellationRequested;
        }

        /// <summary>
        /// Создаёт временную директорию для тестирования
        /// </summary>
        public static string CreateTempDirectory(string? prefix = null, int? count = null)
        {
            var dirName = prefix ?? "temp_test";
            if (count.HasValue)
            {
                dirName += $"_{count.Value}";
            }

            var tempPath = Path.Combine(Path.GetTempPath(), dirName);
            Directory.CreateDirectory(tempPath);
            return tempPath;
        }

        /// <summary>
        /// Очищает временную директорию после тестирования
        /// </summary>
        public static async Task CleanupAsync(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.Delete(directoryPath, true);
                }
                catch { /* Ignore cleanup errors */ }
            }
        }

        /// <summary>
        /// Создаёт тестовый файл в директории
        /// </summary>
        public static void CreateTempFile(string directoryPath, string fileName, string content = "")
        {
            var filePath = Path.Combine(directoryPath, fileName);
            File.WriteAllText(filePath, content);
        }
    }
}

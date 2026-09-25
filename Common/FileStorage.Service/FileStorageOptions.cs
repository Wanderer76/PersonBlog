namespace FileStorage.Service
{
    public class FileStorageOptions
    {
        public const int DefaultPresignedUrlExpirySeconds = 900;

        public string Endpoint { get; set; } = null!;
        public string AccessKey { get; set; } = null!;
        public string SecretKey { get; set; } = null!;
        public int PresignedUrlExpirySeconds { get; set; } = DefaultPresignedUrlExpirySeconds;
    }
}

namespace Infrastructure.Services;

public interface IMultipartFileUpload
{
    Task<MultipartUploadSession> InitiateUploadAsync(string bucketId, string objectName, long size);
    Task<PreSignedUrl> GenerateUploadPartUrlAsync(string bucketId, string uploadId, int partNumber, TimeSpan expiry = default);
    Task<PreSignedUrl> GenerateDownloadPartUrlAsync(string bucketId, string uploadId, int partNumber, TimeSpan expiry = default);
    Task<MultipartUploadPart> UploadPartAsync(string bucketId, string uploadId, int partNumber, Stream input);
    Task<string> CompleteUploadAsync(string bucketId, string uploadId, List<MultipartUploadPart> parts);
    Task AbortUploadAsync(string bucketId, string uploadId);
    Task<MultipartUploadSession?> GetUploadSessionAsync(string bucketId, string uploadId);
    Task<IReadOnlyList<MultipartUploadPart>> ListPartsAsync(string bucketId, string uploadId);
}

public sealed class PreSignedUrl
{
    public string Url { get; set; } = null!;
    public string UploadId { get; set; } = null!;
    public int PartNumber { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string HttpMethod { get; set; } = "PUT";
}

public sealed class MultipartUploadSession
{
    public string UploadId { get; set; } 
    public string BucketId { get; set; }
    public string ObjectName { get; set; }
    public string ContentType { get; set; } = "application/octet-stream";
    public DateTime CreatedAt { get; set; }
    public UploadStatus Status { get; set; }
    public long TotalSize { get; set; }
    public int TotalParts { get; set; }
    public string? CompletedETag { get; set; }
}

public sealed class MultipartUploadPart
{
    public int PartNumber { get; set; }
    public string eTag { get; set; } = null!;
    public long Size { get; set; }
    public DateTime UploadedAt { get; set; }
}
public enum UploadStatus
{
    InProgress,
    Completed,
    Aborted,
    Failed
}

using Infrastructure.Services;

namespace Blog.Contracts.Models.Upload;

public sealed class GenerateUrlRequest
{
    public string UploadId { get; set; } = null!;
    public int PartNumber { get; set; }
    public int ExpiryMinutes { get; set; } = 5;
}

public sealed class CompleteUploadRequest
{
    public string UploadId { get; set; } = null!;
    public List<MultipartUploadPart> Parts { get; set; } = [];
}

public sealed class AbortUploadRequest
{
    public string UploadId { get; set; } = null!;
}

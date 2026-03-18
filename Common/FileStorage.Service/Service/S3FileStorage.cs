using Infrastructure.Services;
using Shared.Services;
using Amazon.S3;
using Amazon.S3.Model;
using System.Net;
using System.Text.Json;

namespace FileStorage.Service.Service;

internal class S3FileStorage : IMultipartFileUpload
{
    private readonly IAmazonS3 _client;
    private const string SESSIONS_PREFIX = "__uploads__";
    private const int _minPartSize = 5 * 1024 * 1024; // 5MB minimum part size

    public S3FileStorage(IAmazonS3 client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<MultipartUploadSession> InitiateUploadAsync(string bucketId, string objectName, long size)
    {
        await CreateBucketIfNotExistsAsync(bucketId);
        var initiateRequest = new InitiateMultipartUploadRequest
        {
            BucketName = bucketId,
            Key = objectName,
            ContentType = "application/octet-stream",
        };

        var initiateResponse = await _client.InitiateMultipartUploadAsync(initiateRequest);
        var totalParts = CalculateParts(size);

        var session = new MultipartUploadSession
        {
            UploadId = initiateResponse.UploadId,
            BucketId = bucketId,
            ObjectName = objectName,
            CreatedAt = DateTimeService.Now().DateTime,
            Status = UploadStatus.InProgress,
            TotalSize = 0,
            TotalParts = 0
        };

        await SaveSessionAsync(bucketId, session);
        return session;
    }
    private static int CalculateParts(long totalSize)
    {
        const int maxParts = 10000;
        var partSize = Math.Max(_minPartSize, totalSize / maxParts);
        return (int)Math.Ceiling((double)totalSize / partSize);
    }

    public async Task<MultipartUploadPart> UploadPartAsync(string bucketId, string uploadId, int partNumber, Stream input)
    {
        var session = await GetSessionAsync(bucketId, uploadId);

        if (session == null)
            throw new InvalidOperationException($"Upload session {uploadId} not found");

        if (session.Status != UploadStatus.InProgress)
            throw new InvalidOperationException($"Upload session {uploadId} is not in progress");

        if (input.CanSeek)
            input.Position = 0;

        var partSize = input.Length;

        // Загружаем часть через нативный API S3
        var uploadPartRequest = new UploadPartRequest
        {
            BucketName = bucketId,
            Key = session.ObjectName,
            UploadId = uploadId,
            PartNumber = partNumber,
            InputStream = input,
            PartSize = partSize
        };

        var uploadPartResponse = await _client.UploadPartAsync(uploadPartRequest);

        var part = new MultipartUploadPart
        {
            PartNumber = partNumber,
            eTag = uploadPartResponse.ETag.Trim('"'),
            Size = partSize,
            UploadedAt = DateTime.UtcNow
        };

        // Обновляем метаданные сессии
        session.TotalSize += partSize;
        session.TotalParts = Math.Max(session.TotalParts, partNumber);
        await SaveSessionAsync(bucketId, session);

        return part;
    }

    public async Task<string> CompleteUploadAsync(string bucketId, string uploadId, List<MultipartUploadPart> parts)
    {
        var session = await GetSessionAsync(bucketId, uploadId);

        if (session == null)
            throw new InvalidOperationException($"Upload session {uploadId} not found");

        if (parts == null || parts.Count == 0)
        {
            parts = (await ListPartsAsync(bucketId, uploadId)).ToList();
        }

        if (parts.Count == 0)
            throw new InvalidOperationException("No parts uploaded for this upload session");

        // Формируем список частей для завершения
        var partETags = parts.OrderBy(p => p.PartNumber).Select(p => new PartETag(p.PartNumber, $"\"{p.eTag}\"")).ToList();

        // Завершаем multipart upload
        var completeRequest = new CompleteMultipartUploadRequest
        {
            BucketName = bucketId,
            Key = session.ObjectName,
            UploadId = uploadId,
            PartETags = partETags
        };

        var completeResponse = await _client.CompleteMultipartUploadAsync(completeRequest);

        session.Status = UploadStatus.Completed;
        await SaveSessionAsync(bucketId, session);
        return completeResponse.ETag.Trim('"');
    }

    public async Task AbortUploadAsync(string bucketId, string uploadId)
    {
        var session = await GetSessionAsync(bucketId, uploadId);

        if (session == null)
            return; // Сессия уже удалена или не существует

        // Отменяем multipart upload через нативный API
        var abortRequest = new AbortMultipartUploadRequest
        {
            BucketName = bucketId,
            Key = session.ObjectName,
            UploadId = uploadId
        };

        await _client.AbortMultipartUploadAsync(abortRequest);

        // Обновляем статус сессии
        session.Status = UploadStatus.Aborted;
        await SaveSessionAsync(bucketId, session);
    }

    public async Task<MultipartUploadSession?> GetUploadSessionAsync(string bucketId, string uploadId)
    {
        return await GetSessionAsync(bucketId, uploadId);
    }

    public async Task<IReadOnlyList<MultipartUploadPart>> ListPartsAsync(string bucketId, string uploadId)
    {
        var session = await GetSessionAsync(bucketId, uploadId) ?? throw new InvalidOperationException($"Upload session {uploadId} not found");
        var parts = new List<MultipartUploadPart>();
        string? nextPartNumberMarker = null;
        bool isTruncated = true;

        while (isTruncated)
        {
            var listRequest = new ListPartsRequest
            {
                BucketName = bucketId,
                Key = session.ObjectName,
                UploadId = uploadId,
                PartNumberMarker = nextPartNumberMarker
            };

            var listResponse = await _client.ListPartsAsync(listRequest);

            foreach (var part in listResponse.Parts)
            {
                parts.Add(new MultipartUploadPart
                {
                    PartNumber = part.PartNumber!.Value,
                    eTag = part.ETag.Trim('"'),
                    Size = part.Size!.Value,
                    UploadedAt = part.LastModified!.Value
                });
            }

            isTruncated = listResponse.IsTruncated!.Value;
            nextPartNumberMarker = listResponse.NextPartNumberMarker?.ToString();
        }

        return parts.OrderBy(p => p.PartNumber).ToList();
    }

    private async Task<MultipartUploadSession?> GetSessionAsync(string bucketName, string uploadId)
    {
        try
        {
            var key = $"{SESSIONS_PREFIX}/{uploadId}.json";
            var request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = key
            };

            using var response = await _client.GetObjectAsync(request);
            using var reader = new StreamReader(response.ResponseStream);
            var json = await reader.ReadToEndAsync();

            return JsonSerializer.Deserialize<MultipartUploadSession>(json);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private async Task SaveSessionAsync(string bucketName, MultipartUploadSession session)
    {
        var key = $"{SESSIONS_PREFIX}/{session.UploadId}.json";
        var json = JsonSerializer.Serialize(session);

        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = key,
            ContentBody = json,
            ContentType = "application/json",
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.None
        };

        await _client.PutObjectAsync(request);
    }

    public async Task<PreSignedUrl> GenerateUploadPartUrlAsync(string bucketId, string uploadId, int partNumber, TimeSpan expiry = default)
    {
        var session = await GetSessionAsync(bucketId, uploadId);

        if (session == null)
            throw new InvalidOperationException($"Upload session {uploadId} not found");

        if (expiry == default)
            expiry = TimeSpan.FromMinutes(5); // 5 минут на загрузку части

        // Создаем временный объект для части (MinIO не поддерживает pre-signed для частей напрямую)
        // Поэтому используем временный ключ
        //var tempKey = $"__temp__/{uploadId}/{partNumber:D8}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketId,
            Key = session.ObjectName,
            UploadId = uploadId,
            PartNumber = partNumber,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(expiry),
            ContentType = "application/octet-stream",
            Protocol = Protocol.HTTP
        };

        var url = _client.GetPreSignedURL(request);

        return new PreSignedUrl
        {
            Url = url,
            UploadId = uploadId,
            PartNumber = partNumber,
            ExpiresAt = DateTime.UtcNow.Add(expiry),
            HttpMethod = "PUT"
        };
    }

    public async Task<PreSignedUrl> GenerateDownloadPartUrlAsync(string bucketId, string uploadId, int partNumber, TimeSpan expiry = default)
    {
        var session = await GetSessionAsync(bucketId, uploadId);

        if (session == null)
            throw new InvalidOperationException($"Upload session {uploadId} not found");

        if (expiry == default)
            expiry = TimeSpan.FromMinutes(5);

        var tempKey = $"__temp__/{uploadId}/{partNumber:D8}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketId,
            Key = tempKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry),
            Protocol = Protocol.HTTP
        };

        var url = _client.GetPreSignedURL(request);

        return new PreSignedUrl
        {
            Url = url,
            UploadId = uploadId,
            PartNumber = partNumber,
            ExpiresAt = DateTime.UtcNow.Add(expiry),
            HttpMethod = "GET"
        };
    }

    private async Task CreateBucketIfNotExistsAsync(string bucketName)
    {
        if (!await BucketExistsAsync(bucketName))
        {
            await _client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = bucketName
            });
        }
    }

    private async Task<bool> BucketExistsAsync(string bucketName)
    {
        try
        {
            await _client.GetBucketLocationAsync(bucketName);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}



/*
internal class S3FileStorage : IFileStorage
{
    private readonly IAmazonS3 _clinet;
    private readonly IOptions<FileStorageOptions> _options;
    private const int Expiry = 604800;

    public Task CreateTempBucketAsync(Guid bucketId)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        _clinet?.Dispose();
    }

    public IAsyncEnumerable<(string Objectname, IReadOnlyDictionary<string, string> Headers)> GetAllBucketObjects(Guid bucketId, ChunkUploadingInfo options)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetFileUrlAsync(Guid bucketId, string objectName)
    {
        return _clinet.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = bucketId.ToString(),
            Key = objectName
        });
    }

    public async Task<string> PutFileAsync(Guid bucketId, string objectName, Stream input)
    {
        await CreateBucketIfNotExistsAsync(bucketId.ToString());
        if (input.CanSeek)
            input.Position = 0;

        var result = await _clinet.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucketId.ToString(),
            Key = objectName,
            InputStream = input,
        });
        var url = _clinet.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = bucketId.ToString(),
            Key = objectName,
            Verb = HttpVerb.GET,
            Expires = DateTimeService.Now().AddMinutes(Expiry).DateTime,
            Protocol = Protocol.HTTP, 
        });
        return url;
    }

    public Task<string> PutFileChunkAsync(Guid bucketId, Guid id, Stream input, ChunkUploadingInfo options)
    {
        throw new NotImplementedException();
    }

    public Task ReadFileAsync(Guid bucketId, string objectName, Stream output)
    {
        throw new NotImplementedException();
    }

    public Task<long> ReadFileByChunksAsync(Guid bucketId, string objectName, long offset, long length, Stream output)
    {
        throw new NotImplementedException();
    }

    public Task RemoveBucketAsync(string bucketId)
    {
        throw new NotImplementedException();
    }

    public Task RemoveFileAsync(Guid bucketId, string objectName)
    {
        throw new NotImplementedException();
    }

    private async Task CreateBucketIfNotExistsAsync(string bucketName)
    {
        if (!await BucketExistsAsync(bucketName))
        {
            await _clinet.PutBucketAsync(new PutBucketRequest
            {
                BucketName = bucketName
            });
        }
    }

    private async Task<bool> BucketExistsAsync(string bucketName)
    {
        try
        {
            await _clinet.GetBucketLocationAsync(bucketName);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
*/

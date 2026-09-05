using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using FileStorage.Service.Service;
using Infrastructure.Services;
using System.Text;
using System.Text.Json;

namespace BlogServiceTests;

public sealed class S3MultipartFileUploadServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(null)]
    public async Task ListPartsReturnsEmptyForUnpopulatedSdkResponse(bool? isTruncated)
    {
        using var client = new StubS3Client(new ListPartsResponse { Parts = null, IsTruncated = isTruncated });
        var service = new S3MultipartFileUploadService(client);

        var parts = await service.ListPartsAsync("bucket", "upload");

        Assert.Empty(parts);
        Assert.Single(client.Requests);
    }

    [Fact]
    public async Task ListPartsContinuesPaginationAfterEmptyPage()
    {
        using var client = new StubS3Client(
            new ListPartsResponse { Parts = null, IsTruncated = true, NextPartNumberMarker = 1 },
            new ListPartsResponse { Parts = [Part(2)], IsTruncated = false });
        var service = new S3MultipartFileUploadService(client);

        var parts = await service.ListPartsAsync("bucket", "upload");

        Assert.Equal(2, Assert.Single(parts).PartNumber);
        Assert.Equal("1", client.Requests[1].PartNumberMarker);
    }

    [Fact]
    public async Task ListPartsMapsAndSortsAllPages()
    {
        using var client = new StubS3Client(
            new ListPartsResponse { Parts = [Part(2), Part(1)], IsTruncated = true, NextPartNumberMarker = 2 },
            new ListPartsResponse { Parts = [Part(3)], IsTruncated = false });
        var service = new S3MultipartFileUploadService(client);

        var parts = await service.ListPartsAsync("bucket", "upload");

        Assert.Equal(new[] { 1, 2, 3 }, parts.Select(part => part.PartNumber));
        Assert.All(parts, part =>
        {
            Assert.Equal($"etag-{part.PartNumber}", part.eTag);
            Assert.Equal(1024L, part.Size);
            Assert.Equal(DateTime.UnixEpoch, part.UploadedAt);
        });
        Assert.Equal("2", client.Requests[1].PartNumberMarker);
        Assert.All(client.Requests, request =>
        {
            Assert.Equal("bucket", request.BucketName);
            Assert.Equal("post/video.mp4", request.Key);
            Assert.Equal("upload", request.UploadId);
        });
    }

    private static PartDetail Part(int number) => new()
    {
        PartNumber = number,
        ETag = $"\"etag-{number}\"",
        Size = 1024,
        LastModified = DateTime.UnixEpoch
    };

    private sealed class StubS3Client(params ListPartsResponse[] responses)
        : AmazonS3Client(new AnonymousAWSCredentials(), new AmazonS3Config { ServiceURL = "http://localhost" })
    {
        public List<ListPartsRequest> Requests { get; } = [];

        public override Task<GetObjectResponse> GetObjectAsync(GetObjectRequest request, CancellationToken cancellationToken = default)
        {
            var session = new MultipartUploadSession
            {
                UploadId = "upload", BucketId = "bucket", ObjectName = "post/video.mp4", Status = UploadStatus.InProgress
            };
            return Task.FromResult(new GetObjectResponse
            {
                ResponseStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(session)))
            });
        }

        public override Task<ListPartsResponse> ListPartsAsync(ListPartsRequest request, CancellationToken cancellationToken = default)
        {
            var response = responses[Requests.Count];
            Requests.Add(request);
            return Task.FromResult(response);
        }
    }
}

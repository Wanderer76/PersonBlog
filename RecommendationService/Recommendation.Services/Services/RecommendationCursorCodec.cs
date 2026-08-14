using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Recommendation.Services.Models;
using Recommendation.Services.Options;

namespace Recommendation.Services.Services;

internal sealed class RecommendationCursorCodec(RecommendationFeedOptions options)
{
    private const int Version = 1;
    private readonly byte[] _key = GetKey(options.CursorSigningKey);

    public string Encode(
        Guid requestId,
        int offset,
        DateTimeOffset generatedAt,
        string subjectFingerprint,
        Guid? currentPostId,
        string algorithmVersion)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(new CursorPayload(
            Version,
            requestId,
            offset,
            generatedAt,
            subjectFingerprint,
            currentPostId,
            algorithmVersion));
        var signature = HMACSHA256.HashData(_key, payload);
        return $"{Base64UrlEncode(payload)}.{Base64UrlEncode(signature)}";
    }

    public DecodedCursor Decode(
        string cursor,
        string subjectFingerprint,
        Guid? currentPostId,
        string algorithmVersion)
    {
        try
        {
            var parts = cursor.Split('.');
            if (parts.Length != 2) throw new FormatException();
            var payloadBytes = Base64UrlDecode(parts[0]);
            var suppliedSignature = Base64UrlDecode(parts[1]);
            var expectedSignature = HMACSHA256.HashData(_key, payloadBytes);
            if (!CryptographicOperations.FixedTimeEquals(suppliedSignature, expectedSignature))
                throw new InvalidRecommendationCursorException("Cursor signature is invalid.");

            var payload = JsonSerializer.Deserialize<CursorPayload>(payloadBytes)
                ?? throw new FormatException();
            if (payload.Version != Version || payload.Offset < 0)
                throw new InvalidRecommendationCursorException("Cursor version or offset is invalid.");
            if (payload.SubjectFingerprint != subjectFingerprint
                || payload.CurrentPostId != currentPostId
                || payload.AlgorithmVersion != algorithmVersion)
                throw new InvalidRecommendationCursorException("Cursor does not belong to this feed.");

            return new DecodedCursor(payload.RequestId, payload.Offset, payload.GeneratedAt);
        }
        catch (InvalidRecommendationCursorException)
        {
            throw;
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            throw new InvalidRecommendationCursorException("Cursor format is invalid.");
        }
    }

    public static string CreateSubjectFingerprint(Guid? userId, string? anonymousSessionId)
    {
        var subject = userId.HasValue ? $"user:{userId.Value:N}" : $"anonymous:{anonymousSessionId}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(subject)));
    }

    private static byte[] GetKey(string signingKey)
    {
        if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
            throw new InvalidOperationException("Recommendation cursor signing key must contain at least 32 bytes.");
        return Encoding.UTF8.GetBytes(signingKey);
    }

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        base64 += new string('=', (4 - base64.Length % 4) % 4);
        return Convert.FromBase64String(base64);
    }

    private sealed record CursorPayload(
        int Version,
        Guid RequestId,
        int Offset,
        DateTimeOffset GeneratedAt,
        string SubjectFingerprint,
        Guid? CurrentPostId,
        string AlgorithmVersion);

    public sealed record DecodedCursor(Guid RequestId, int Offset, DateTimeOffset GeneratedAt);
}

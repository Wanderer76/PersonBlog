namespace Infrastructure.Services;

/// <summary>
/// Backward-compatible registration name for applications that already add this factory globally.
/// Serialization behavior is implemented by the shared result converter factory.
/// </summary>
public sealed class ResultConverterFactory : ResultJsonConverterFactory
{
}

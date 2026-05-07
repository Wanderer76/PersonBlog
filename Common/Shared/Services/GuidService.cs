namespace Shared.Services;

public interface IGuidManager
{
    Guid GetNewGuid() => GuidService.GetNewGuid();
}

public static class GuidService
{
    public static Guid GetNewGuid() => Guid.NewGuid();
}

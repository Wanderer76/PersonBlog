namespace Shared.Services;

public interface IGuidManager
{
    Guid GetNewGuid() => Guid.NewGuid();
}

public class GuidService : IGuidManager
{
    public static Guid GetNewGuid() => Guid.NewGuid();
}

//public static class GuidService
//{
//    public static Guid GetNewGuid() => Guid.NewGuid();
//}

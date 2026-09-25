namespace Shared;

public interface ISoftDelete
{
    public bool IsDelete { get; }
    public DateTimeOffset? DeleteDateTime { get; }
}

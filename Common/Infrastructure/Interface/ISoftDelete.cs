namespace Infrastructure.Interface;

public interface ISoftDelete
{
    public bool IsDelete { get; }
    public DateTimeOffset? DeleteDateTime { get; }
}

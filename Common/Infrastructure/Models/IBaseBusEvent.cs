namespace Infrastructure.Models
{
    public interface IBaseBusEvent
    {
        byte[] Serialize();
        T Deserialize<T>();
    }
}

namespace Authentication.Domain.Entities;
public class Client : IAuthEntity
{
    public string ClientId { get; private set; } = null!;
    public string RedirectUri { get; private set; } = null!;

    private Client()
    {
        
    }
}

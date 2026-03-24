namespace Authentication.Service.Models;

public sealed class RedirectResponse
{
    public string RedirectUrl { get; private set; } = null!;
    
    public RedirectResponse()
    {

    }

    public RedirectResponse(string redirectUrl)
    {
        RedirectUrl = redirectUrl;
    }
}

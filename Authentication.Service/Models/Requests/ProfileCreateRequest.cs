namespace AuthenticationApplication.Models.Requests;

public class ProfileCreateRequest
{
    public string FirstName { get; set; } = null!;
    public string SurName { get; set; } = null!;
    public string? LastName { get; set; }
    public DateTimeOffset Birthdate{ get; set; }
    public Guid UserId{ get; set; }
}
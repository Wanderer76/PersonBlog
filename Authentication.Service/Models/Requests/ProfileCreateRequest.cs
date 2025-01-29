namespace AuthenticationApplication.Models.Requests;

public class ProfileCreateRequest
{
    public string? FirstName { get; set; }
    public string? SurName { get; set; }
    public string? LastName { get; set; }
    public DateTimeOffset? Birthdate { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; }
}
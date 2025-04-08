namespace Authentication.Domain.Interfaces.Models.Profile
{
    public class ProfileEditModel
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string SurName { get; set; }
        public string Email { get; set; }
        public string? LastName { get; set; }
        public DateTimeOffset Birthdate { get; set; }
        public string? PhotoUrl { get; set; }
    }
}

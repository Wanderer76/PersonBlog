namespace Authentication.Domain.Interfaces.Models.Profile
{
    public class ProfileUpdateModel : ProfileCreateModel
    {
        public Guid Id { get; set; }
    }
}

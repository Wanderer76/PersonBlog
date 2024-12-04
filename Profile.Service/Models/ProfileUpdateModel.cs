namespace Profile.Service.Models
{
    public class ProfileUpdateModel : ProfileCreateModel
    {
        public Guid ProfileId { get; set; }
    }
}

namespace Profile.Service.Models
{
    public class UserViewInfo
    {
        public bool IsViewed { get; set; }
        public bool? IsLike { get; set; }

        public UserViewInfo()
        {
        }
        
        public UserViewInfo(bool isViewed, bool? isLike)
        {
            IsViewed = isViewed;
            IsLike = isLike;
        }

        public static UserViewInfo CreateEmpty => new(false, null);
    }
}
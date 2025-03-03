namespace Profile.Service.Models
{
    public class UserViewInfo
    {
        public bool IsViewed { get; set; }
        public bool? IsLike { get; set; }
        public bool IsSubscribe { get; set; }


        public UserViewInfo(bool isViewed, bool? isLike, bool isSubscribe)
        {
            IsViewed = isViewed;
            IsLike = isLike;
            IsSubscribe = isSubscribe;
        }

        public static UserViewInfo CreateEmpty => new(false, null, false);
    }
}
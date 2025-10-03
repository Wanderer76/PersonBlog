namespace Authentication.Contract.Constants
{
    public static class Roles
    {
        public const string Admin = "57a2b99b-b6ee-4c98-a1f0-b18fe96dae60";
        public const string SuperAdmin = "accbc12f-6ff1-4343-a26f-13b99e64abb6";
        public const string User = "d95ca3d6-0f63-4b48-a54f-1202f3d6bf2c";
        public const string Blogger = "68816c1b-fdfb-45d0-bfea-dceb04277a57";
        public const string Artist = "72c4bbed-5375-47fc-864f-1490ca82aced";

        public static readonly Guid AdminRoleId = Guid.Parse(Admin);
        public static readonly Guid SuperAdminRoleId = Guid.Parse(SuperAdmin);
        public static readonly Guid UserRoleId = Guid.Parse(User);
        public static readonly Guid BloggerRoleId = Guid.Parse(Blogger);
        public static readonly Guid ArtistRoleId = Guid.Parse(Artist);
    }
}

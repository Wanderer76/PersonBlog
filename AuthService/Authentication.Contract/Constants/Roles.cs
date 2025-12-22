namespace Authentication.Contract.Constants
{
    public static class Roles
    {
        public const string Admin = "57a2b99b-b6ee-4c98-a1f0-b18fe96dae60";
        public const string SuperAdmin = "accbc12f-6ff1-4343-a26f-13b99e64abb6";
        public const string User = "d95ca3d6-0f63-4b48-a54f-1202f3d6bf2c";
        public const string Blogger = "c2ff298c-dd14-436c-a28b-e2036866ef41";
        public const string Artist = "72c4bbed-5375-47fc-864f-1490ca82aced";

        public static readonly Guid AdminRoleId = Guid.Parse(Admin);
        public static readonly Guid SuperAdminRoleId = Guid.Parse(SuperAdmin);
        public static readonly Guid UserRoleId = Guid.Parse(User);
        public static readonly Guid BloggerRoleId = Guid.Parse(Blogger);
        public static readonly Guid ArtistRoleId = Guid.Parse(Artist);
    }
}

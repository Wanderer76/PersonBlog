namespace Blog.Domain.Entities
{
    public enum PostVisibility
    {
        Public,
        ByUrl,
        Private
    }

    public static class PostVisibilityExtensions
    {
        public static string FormatName(this PostVisibility visibility)
        {
            switch (visibility)
            {
                case PostVisibility.Public:
                    return "Публичный";
                case PostVisibility.ByUrl:
                    return "Ссылочный";
                case PostVisibility.Private:
                    return "Приватный";
                default:
                    throw new NotImplementedException();
            }
        }
    }
}

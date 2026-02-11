namespace Blog.Contracts.Models.Category
{
    public class CategoryModel
    {
        public int Id { get; }
        public string Title { get; }

        public CategoryModel(int id, string title)
        {
            Id = id;
            Title = title;
        }
    }
}

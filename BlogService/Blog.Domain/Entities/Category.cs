using System.ComponentModel.DataAnnotations;

namespace Blog.Domain.Entities
{
    public class Category : IBlogEntity
    {
        [Key]
        public int Id { get; private set; }

        [StringLength(100)]
        public string Title { get; private set; }

        public Category(int id, string title)
        {
            Id = id;
            Title = title;
        }
    }
}

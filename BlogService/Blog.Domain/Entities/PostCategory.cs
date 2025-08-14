using System.ComponentModel.DataAnnotations.Schema;

namespace Blog.Domain.Entities
{
    public class PostCategory : IBlogEntity
    {
        public Guid PostId { get; private set; }
        public int CategoryId { get; private set; }

        [ForeignKey(nameof(PostId))]
        public Post Post { get; private set; }

        [ForeignKey(nameof(CategoryId))]
        public Category Category { get; private set; }

        public PostCategory(Guid postId, int categoryId)
        {
            PostId = postId;
            CategoryId = categoryId;
        }
    }
}

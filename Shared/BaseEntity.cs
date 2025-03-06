using System.ComponentModel.DataAnnotations;

namespace Shared
{
    public abstract class BaseEntity<TId> where TId : notnull
    {
        [Key]
        public TId Id { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public bool IsDeleted { get; set; } = false;
    }
}

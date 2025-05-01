using System.ComponentModel.DataAnnotations;

namespace Shared
{
    public abstract class BaseEntity
    {
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public bool IsDeleted { get; set; } = false;
    }
}

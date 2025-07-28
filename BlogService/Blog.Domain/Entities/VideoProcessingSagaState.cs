using System.ComponentModel.DataAnnotations;

namespace Blog.Domain.Entities
{
    public class VideoProcessingSagaState :  IBlogEntity
    {
        [Key]
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = default!;
        public Guid VideoMetadataId { get; set; }
        public Guid PostId { get; set; }
        public string? ObjectName { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profile.Domain.Entities
{
    public class VideoMetadata : IProfileEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid FileId { get; set; }
        public long Length { get; set; }
        public string ContentType { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}

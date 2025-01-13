using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profile.Service.Models.File
{
    public class UploadVideoChunkDto
    {
        public required Guid UserId { get; set; }
        public required Guid PostId { get; set; }
        public required long ChunkNumber { get; set; }
        public required long TotalChunkCount { get; set; }
        public required Stream ChunkData { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blog.Service.Models.File
{
    public class PostFileMetadataModel : FileMetadataModel
    {
        public string PreviewUrl { get; }
        public Guid PostId { get; }

        public PostFileMetadataModel(string contentType, long length, string name, DateTimeOffset createdAt, Guid id, string objectName, string previewUrl, Guid postId)
            : base(contentType, length, name, createdAt, id, objectName)
        {
            PreviewUrl = previewUrl;
            PostId = postId;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blog.Contracts.Events
{
    public class PostBannedEvent
    {
        public Guid PostId {  get; set; }
        public string Message {  get; set; }
        public DateTimeOffset CreatedAt {  get; set; }
    }

    public class PostUnBannedEvent
    {
        public Guid PostId { get; set; }
    }
}

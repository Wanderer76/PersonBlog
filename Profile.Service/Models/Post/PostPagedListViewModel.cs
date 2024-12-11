using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profile.Service.Models.Post
{
    public class PostPagedListViewModel
    {
        public required int TotalPageCount { get; init; }
        public required IEnumerable<PostModel> Posts { get; init; }
    }
}

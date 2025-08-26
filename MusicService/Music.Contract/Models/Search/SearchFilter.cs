using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Music.Contract.Models.Search
{
    public class SearchFilter
    {
        public SortOrder? Order { get; set; }
        public string? Title { get; set; }
        public List<Guid>? Genres { get; set; }
    }

    public enum SortOrder
    {
        New,
        Popular,
    }
}

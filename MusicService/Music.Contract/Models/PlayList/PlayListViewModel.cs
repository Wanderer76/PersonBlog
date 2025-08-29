using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Music.Contract.Models.PlayList
{
    public class PlayListViewModel
    {
        public Guid Id { get; }
        public string Title { get; }
        public int TrackCount { get; }
        public string? ThumbnailUrl { get; }
        public string Type { get; }

        public PlayListViewModel()
        {

        }
        public PlayListViewModel(Guid id, string title, int trackCount, string? thumbnailUrl, string type)
        {
            Id = id;
            Title = title;
            TrackCount = trackCount;
            ThumbnailUrl = thumbnailUrl;
            Type = type;
        }
    }
}

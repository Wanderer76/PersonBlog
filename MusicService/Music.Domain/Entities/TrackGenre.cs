using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Music.Domain.Entities
{
    public class TrackGenre : IMusicEntity
    {
        public Guid TrackId { get; private set; }
        public Guid GenreId { get; private set; }

        [ForeignKey(nameof(GenreId))]
        public Genre Genre { get; private set; }

        [ForeignKey(nameof(TrackId))]
        public Track Track { get; private set; }

        public TrackGenre(Guid trackId, Guid genreId)
        {
            TrackId = trackId;
            GenreId = genreId;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicRecommendation.Domain.Domain
{
    public class Track : IRecommendation
    {
        [Key]
        public Guid Id { get; private set; }
        public Guid? AlbumId { get; private set; }
        public Guid ArtistId { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public List<TrackGenre> Genres {  get; private set; }
        private Track()
        {
            Genres = [];
        }

        public Track(Guid id, Guid? albumId, Guid artistId, DateTimeOffset createdAt, List<TrackGenre> genres)
        {
            Id = id;
            AlbumId = albumId;
            ArtistId = artistId;
            CreatedAt = createdAt;
            Genres = genres;
        }
    }

    public class TrackGenre : IRecommendation
    {
        [Key]
        public Guid Id { get; private set; }

        public Guid TrackId {  get; private set; }

        [ForeignKey(nameof(TrackId))]
        public Track Track { get; private set; }

        private TrackGenre()
        {
            
        }

        public TrackGenre(Guid id, Guid trackId)
        {
            Id = id;
            TrackId = trackId;
        }
    }
}

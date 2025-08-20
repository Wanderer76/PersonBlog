using Shared.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Music.Domain.Entities
{
    public class TrackMetadata : FileMetadata, IMusicEntity
    {
        public bool IsProcessed { get; set; }
        public Guid TrackId { get; private set; }
        public double Duration { get; private set; }

        [ForeignKey(nameof(TrackId))]
        public Track Track { get; private set; }
    }
}

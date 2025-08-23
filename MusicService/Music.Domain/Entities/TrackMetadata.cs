using Shared.Models;
using Shared.Services;
using System.ComponentModel.DataAnnotations.Schema;

namespace Music.Domain.Entities
{
    public class TrackMetadata : FileMetadata, IMusicEntity
    {
        public FileProcessState ProcessState { get; set; }
        public Guid TrackId { get; private set; }
        public double Duration { get; private set; }

        [ForeignKey(nameof(TrackId))]
        public Track Track { get; private set; }

        public TrackMetadata(Guid id, string name, string extension, long length, string contentType, string objectName, Guid trackId, double duration)

        {
            Id = id;
            Name = name;
            FileExtension = extension;
            ProcessState = FileProcessState.None;
            TrackId = trackId;
            Duration = duration;
            Length = length;
            ContentType = contentType;
            ObjectName = objectName;
            CreatedAt = DateTimeService.Now();
        }
    }

    public enum FileProcessState
    {
        None,
        Init,
        Complete,
    }
}

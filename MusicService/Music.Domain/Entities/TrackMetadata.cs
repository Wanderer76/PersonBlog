using Shared.Models;
using Shared.Services;
using System.ComponentModel.DataAnnotations.Schema;

namespace Music.Domain.Entities
{
    public class TrackMetadata : BaseFileMetadataEntity, IMusicEntity
    {
        public FileProcessState ProcessState { get; set; }
        public Guid TrackId { get;  set; }
        public long Duration { get;  set; }

        public TrackMetadata()
        {
            
        }

        public TrackMetadata(Guid id, string name, string extension, long length, string contentType, string objectName, Guid trackId, long duration)

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

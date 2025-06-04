using Blog.Domain.Entities;
using Blog.Domain.Services.Models;
using MassTransit;
using MessageBus;

namespace Blog.Domain.Events
{
    //public class CombineFileChunksEvent
    //{
    //    public required Guid EventId { get; set; }
    //    public required Guid VideoMetadataId { get; set; }
    //    public required Guid PostId {  get; set; }
    //    public required bool IsCompleted { get; set; }
    //    public required DateTimeOffset CreatedAt { get; set; }
    //}

    //[EntityName("video-events")]
    [EventPublish]
    public class CombineFileChunksCommand
    {
        public Guid VideoMetadataId { get; set; }
        public Guid PostId { get; set; }
    }

    //[EntityName("video-events")]
    [EventPublish]
    public class ChunksCombinedResponse
    {
        public Guid VideoMetadataId { get; set; }
        public Guid PostId { get; set; }
        public string ObjectName { get; set; }
        public string? ErrorMessage { get; set; }
    }

    //[EntityName("video-events")]
    public class ConvertVideoCommand
    {
        public Guid PostId { get; set; }
        public Guid VideoMetadataId { get; set; }
        public string ObjectName { get; set; }
        public bool HasPreviewId {  get; set; }
        public required VideoMetadata VideoMetadata { get; set; }

    }

    //[EntityName("video-events")]
    public class VideoConvertedResponse
    {
        public Guid VideoMetadataId { get; set; }
        public Guid PostId { get; set; }
        public string? PreviewId { get; set; }
        public bool IsProcessed { get; set; }
        public string ObjectName { get; set; }
        public double Duration { get; set; }
        public ProcessState ProcessState { get; set; }
        public string? Error {  get; set; }

    }
}

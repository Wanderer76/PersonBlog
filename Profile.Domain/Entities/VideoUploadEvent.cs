namespace Profile.Domain.Entities
{
    public class VideoUploadEvent : IProfileEntity
    {
        public required Guid Id { get; set; }
        public required string FileUrl { get; set; }
        public required bool IsCompleted { get; set; }
        public Guid UserProfileId { get; set; }
        public required string ObjectName { get; set; }
        public Guid FileId{ get; set; }
    }
}

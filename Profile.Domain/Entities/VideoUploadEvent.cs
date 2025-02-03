namespace Profile.Domain.Entities
{
    public class VideoUploadEvent 
    {
        public required Guid Id { get; set; }
        public required string FileUrl { get; set; }
        public Guid UserProfileId { get; set; }
        public required string ObjectName { get; set; }
        public Guid FileId { get; set; }
    }
}

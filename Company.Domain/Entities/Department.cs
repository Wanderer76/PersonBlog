using System.ComponentModel.DataAnnotations.Schema;

namespace Company.Domain.Entities
{
    public class Department : ICompanyEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? CloseAt { get; set; }

        public Guid? LeaderUserId { get; set; }

        public Guid OrganizationId { get; set; }

        [ForeignKey(nameof(OrganizationId))]
        public Organization Organization { get; set; }

        public List<Position> Positions { get; set; }
    }
}

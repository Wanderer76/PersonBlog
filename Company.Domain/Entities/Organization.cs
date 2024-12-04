using System.ComponentModel.DataAnnotations;

namespace Company.Domain.Entities
{
    public class Organization : ICompanyEntity
    {
        [Key]
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty!;

        public Guid DirectorId { get; set; }
        public string? Address { get; set; }

        public virtual List<Department> Departments { get; set; }
    }
}

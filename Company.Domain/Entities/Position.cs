using System.ComponentModel.DataAnnotations.Schema;

namespace Company.Domain.Entities
{
    public class Position : ICompanyEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid DepartmentId { get; set; }


        [ForeignKey(nameof(DepartmentId))]
        public Department Department { get; set; }
    }
}

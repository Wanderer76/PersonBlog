using Company.Domain.Entities;

namespace Company.Service.Models
{
    public sealed class DepartmentViewModel
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset? CloseAt { get; private set; }
        public string Manager { get; private set; }

        public DepartmentViewModel(Guid id, string name, string description, DateTimeOffset createdAt, DateTimeOffset? closeAt, string manager)
        {
            Id = id;
            Name = name;
            Description = description;
            CreatedAt = createdAt;
            CloseAt = closeAt;
            Manager = manager;
        }
    }

}

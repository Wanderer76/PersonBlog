using Shared.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewReacting.Domain.Entities
{
    public class SubscribedChanel : IUserEntity
    {
        [Key]
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public Guid BlogId { get; private set; }
        public DateTimeOffset CreatedAt { get; set; }

        public SubscribedChanel(Guid userId, Guid blogId)
        {
            Id = GuidService.GetNewGuid();
            UserId = userId;
            BlogId = blogId;
            CreatedAt = DateTimeService.Now();
        }
    }
}

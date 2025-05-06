using MassTransit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blog.Domain.Entities
{
    public class VideoProcessingSagaState : SagaStateMachineInstance
    {
        [Key]
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = default!;
        public Guid VideoMetadataId { get; set; }
        public Guid PostId { get; set; }
        public string? ObjectName { get; set; }
    }
}

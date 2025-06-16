using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewReacting.Domain.Models
{
    public class HasSubscriptionModel
    {
        public Guid BlogId { get; }
        public bool HasSubscription { get; }

        public HasSubscriptionModel(Guid blogId, bool hasSubscription)
        {
            BlogId = blogId;
            HasSubscription = hasSubscription;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Video.Service.Interface.Default
{
    internal class DefaultReactionService : IReactionService
    {
        public Task RemoveReactionToPost(Guid postId)
        {
            return Task.CompletedTask;
        }

        public Task SetReactionToPost(Guid postId)
        {
            return Task.CompletedTask;
        }

        public Task SetViewToPost(Guid postId, Guid? userId, string segmentNumber)
        {
            return Task.CompletedTask;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Interface
{
    public interface ISoftDelete
    {
        public bool IsDelete { get; }
        public DateTimeOffset? DeleteDateTime { get; }
    }
}

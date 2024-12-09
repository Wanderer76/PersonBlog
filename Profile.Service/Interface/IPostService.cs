using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profile.Service.Interface
{
    public interface IPostService
    {
        Task<Guid> CreatePost();
    }
}

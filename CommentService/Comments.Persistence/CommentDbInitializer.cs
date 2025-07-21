using Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comments.Persistence
{
    internal class CommentDbInitializer : IDbInitializer
    {
        private readonly CommentDbContext _dbContext;

        public CommentDbInitializer(CommentDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void Initialize()
        {
            _dbContext.Database.Migrate();
        }
    }
}

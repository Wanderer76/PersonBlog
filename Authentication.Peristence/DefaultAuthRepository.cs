using Authentication.Domain.Entities;
using Shared.Persistence;

namespace Authentication.Peristence;
//
// public class DefaultAuthRepository : IReadWriteRepository<AuthenticationDbContext>
// {
//     private readonly AuthenticationDbContext _context;
//
//     public DefaultAuthRepository(AuthenticationDbContext context)
//     {
//         _context = context;
//     }
//
//     public IQueryable<TEntity> Get<TEntity>() where TEntity : class, IAuthEntity
//     {
//         _context.Set<TEntity>()
//     }
//
//     public void Attach<TEntity>(TEntity entity) where TEntity : class, IAuthEntity
//     {
//         throw new NotImplementedException();
//     }
//
//     public int SaveChanges()
//     {
//         throw new NotImplementedException();
//     }
//
//     public Task<int> SaveChangesAsync()
//     {
//         throw new NotImplementedException();
//     }
// }
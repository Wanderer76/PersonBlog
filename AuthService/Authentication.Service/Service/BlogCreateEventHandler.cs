using Authentication.Contract.Constants;
using Authentication.Domain.Entities;
using Blog.Contracts.Events;
using Infrastructure.Models;
using Infrastructure.Services;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;

namespace Authentication.Service.Service
{
    public class BlogCreateEventHandler : IEventHandler<BlogCreateEvent>
    {
        private readonly IReadWriteRepository<IAuthEntity> _repository;
        private readonly ICacheService _cacheService;

        public BlogCreateEventHandler(IReadWriteRepository<IAuthEntity> repository, ICacheService cacheService)
        {
            _repository = repository;
            _cacheService = cacheService;
        }

        public async Task Handle(IMessageContext<BlogCreateEvent> @event)
        {
            var user = await _repository.Get<AppUser>()
                .Include(x => x.AppUserRoles)
                .Include(x => x.UserContexts)
                .FirstAsync(x => x.Id == @event.Message.UserId);

            if (user.UserContexts.Any(x => x.ContextType == UserContextType.Blog))
            {
                return;
            }

            if (!user.AppUserRoles.Any(x => x.UserRoleId == Roles.BloggerRoleId))
            {
                var blogRole = new AppUserRole
                {
                    UserRoleId = Roles.BloggerRoleId,
                    AppUserId = @event.Message.UserId
                };
                user.AppUserRoles.Add(blogRole);
                _repository.Add(blogRole);
            }

            var blogUserContext = new UserContext(user.Id, UserContextType.Blog, @event.Message.BlogId);
            user.UserContexts.Add(blogUserContext);
            _repository.Add(blogUserContext);

            await _repository.SaveChangesAsync();

            var token = await _repository.Get<Token>()
                .Where(x => x.AppUserId == @event.Message.UserId)
                .Where(x => x.TokenType == TokenTypes.Access)
                .FirstOrDefaultAsync();

            if (token != null)
            {
                await _cacheService.SetCachedDataAsync(new BlacklistTokenCacheKey(token.Id), token.ToTokenModel(user), (token.ExpiredAt - token.CreatedAt));
            }
        }
    }
}

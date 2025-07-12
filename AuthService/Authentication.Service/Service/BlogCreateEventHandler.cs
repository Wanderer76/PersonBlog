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
            var profile = await _repository.Get<AppProfile>()
                .FirstAsync(x => x.UserId == @event.Message.UserId);

            _repository.Attach(profile);
            profile.BlogId = @event.Message.BlogId;
            await _repository.SaveChangesAsync();

            var token = await _repository.Get<Token>()
                .Where(x => x.AppUserId == @event.Message.UserId)
                .Where(x => x.TokenType == TokenTypes.Access)
                .FirstOrDefaultAsync();

            if (token != null)
            {
                await _cacheService.SetCachedDataAsync(new BlacklistToken(token.Id), token.ToTokenModel(profile.BlogId), (token.ExpiredAt - token.CreatedAt));
            }
        }
    }
}

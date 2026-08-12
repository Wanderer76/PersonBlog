using Blog.Contracts.Events;
using Blog.Domain.Entities;
using Blog.Service.Events;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;

namespace Blog.Domain.Events.Handlers
{
    public class PostBannedEventHandler : IEventHandler<PostBannedEvent>
    {
        private readonly IReadWriteRepository<IBlogEntity> _repository;

        public PostBannedEventHandler(IReadWriteRepository<IBlogEntity> repository)
        {
            _repository = repository;
        }

        public async Task Handle(IMessageContext<PostBannedEvent> @event)
        {
            var post = await _repository.Get<Post>()
                .Include(x => x.VideoPostInfo).ThenInclude(x => x.PostCategories)
                .Include(x => x.VideoPostInfo).ThenInclude(x => x.PreviewFile)
                .Include(x => x.VideoPostInfo).ThenInclude(x => x.VideoFile)
                .Include(x => x.TextPostInfo)
                .FirstAsync(x => x.Id == @event.Message.PostId);

            if (!post.BanMessageId.HasValue)
            {
                _repository.Attach(post);
                var banMessage = new BanMessage(GuidService.GetNewGuid(), post.Id, @event.Message.CreatedAt, @event.Message.Message);
                _repository.Add(banMessage);
                post.SetPostBanned(banMessage);
                post.MarkRecommendationChanged();
                _repository.Add(VideoProcessEvent.Create(PostCatalogChangedV2Factory.Create(post, @event.Message.CreatedAt)));
                await _repository.SaveChangesAsync();
            }
        }

     
    }

    public class PostUnBannedEventHandler : IEventHandler<PostUnBannedEvent>
    {
        private readonly IReadWriteRepository<IBlogEntity> _repository;

        public PostUnBannedEventHandler(IReadWriteRepository<IBlogEntity> repository)
        {
            _repository = repository;
        }

        public async Task Handle(IMessageContext<PostUnBannedEvent> @event)
        {
            var post = await _repository.Get<Post>()
                .Include(x => x.BanMessage)
                .Include(x => x.VideoPostInfo).ThenInclude(x => x.PostCategories)
                .Include(x => x.VideoPostInfo).ThenInclude(x => x.PreviewFile)
                .Include(x => x.VideoPostInfo).ThenInclude(x => x.VideoFile)
                .Include(x => x.TextPostInfo)
                .FirstAsync(x => x.Id == @event.Message.PostId);

            if (post.BanMessageId.HasValue)
            {
                _repository.Attach(post);
                post.RestorePostFromBan();
                post.MarkRecommendationChanged();
                _repository.Add(VideoProcessEvent.Create(PostCatalogChangedV2Factory.Create(post)));
                await _repository.SaveChangesAsync();
            }
        }

    }
}

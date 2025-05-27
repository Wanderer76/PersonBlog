using MessageBus;
using MessageBus.Shared.Configs;
using MessageBus.Shared.Events;
using Shared.Persistence;
using Shared.Services;
using System.Text.Json;
using ViewReacting.Domain.Entities;
using ViewReacting.Domain.Events;
using ViewReacting.Domain.Services;

namespace VideoReacting.Service.Implementation
{
    internal class DefaultReactionService : IReactionService
    {
        private readonly IReadWriteRepository<IVideoReactEntity> _context;
        private readonly RabbitMqMessageBus _messageBus;
        private readonly RabbitMqVideoReactionConfig _reactionConfig = new();

        public DefaultReactionService(IReadWriteRepository<IVideoReactEntity> context, RabbitMqMessageBus messageBus)
        {
            _context = context;
            _messageBus = messageBus;
        }

        public Task RemoveReactionToPost(Guid postId)
        {
            return Task.CompletedTask;
        }

        public async Task SetReactionToPost(ReactionCreateModel reaction)
        {
            //await using var connection = await _messageBus.GetConnectionAsync();
            //await using var channel = await connection.CreateChannelAsync();
            //await channel.ExchangeDeclareAsync(_reactionConfig.ExchangeName, ExchangeType.Direct, durable: true);
            //await channel.QueueDeclareAsync(_reactionConfig.QueueName, durable: true, exclusive: false, autoDelete: false);
            //await channel.QueueBindAsync(_reactionConfig.QueueName, _reactionConfig.ExchangeName, _reactionConfig.ViewRoutingKey);
            var now = DateTimeOffset.UtcNow;
            var eventData = new UserViewedPostEvent(GuidService.GetNewGuid(), reaction.UserId, reaction.PostId, now, reaction.RemoteIp, reaction.IsLike, true);
            var videoEvent = new ReactingEvent
            {
                Id = eventData.EventId,
                EventType = nameof(UserViewedPostEvent),
                EventData = JsonSerializer.Serialize(eventData),
            };
            _context.Add(videoEvent);
            await _context.SaveChangesAsync();
        }

        public async Task SetViewToPost(VideoViewEvent videoView)
        {
            //await using var connection = await _messageBus.GetConnectionAsync();
            //await using var channel = await connection.CreateChannelAsync();
            //await channel.ExchangeDeclareAsync(_reactionConfig.ExchangeName, ExchangeType.Direct, durable: true);
            //await channel.QueueDeclareAsync(_reactionConfig.QueueName, durable: true, exclusive: false, autoDelete: false);
            //await channel.QueueBindAsync(_reactionConfig.QueueName, _reactionConfig.ExchangeName, _reactionConfig.ViewRoutingKey);
                var videoEvent = new ReactingEvent
                {
                    Id = GuidService.GetNewGuid(),
                    EventType = nameof(VideoViewEvent),
                    EventData = JsonSerializer.Serialize(videoView),
                };
                _context.Add(videoEvent);
                await _context.SaveChangesAsync();
            }
        }
    }


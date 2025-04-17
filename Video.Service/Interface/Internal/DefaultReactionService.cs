using MessageBus;
using MessageBus.Shared.Configs;
using MessageBus.Shared.Events;
using RabbitMQ.Client;
using Shared.Persistence;
using Shared.Services;
using System.Text.Json;
using Video.Domain.Entities;
using Video.Domain.Events;

namespace Video.Service.Interface.Default
{
    internal class DefaultReactionService : IReactionService
    {
        private readonly IReadWriteRepository<IVideoViewEntity> _context;
        private readonly RabbitMqMessageBus _messageBus;
        private readonly RabbitMqVideoReactionConfig _reactionConfig = new();

        public DefaultReactionService(IReadWriteRepository<IVideoViewEntity> context, RabbitMqMessageBus messageBus)
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
            await using var connection = await _messageBus.GetConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();
            await channel.ExchangeDeclareAsync(_reactionConfig.ExchangeName, ExchangeType.Direct, durable: true);
            await channel.QueueDeclareAsync(_reactionConfig.QueueName, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync(_reactionConfig.QueueName, _reactionConfig.ExchangeName, _reactionConfig.ViewRoutingKey);
            var now = DateTimeOffset.UtcNow;
            var eventData = new UserViewedPostEvent(GuidService.GetNewGuid(), reaction.UserId, reaction.PostId, now, reaction.RemoteIp, reaction.IsLike, true);
            var videoEvent = new VideoEvent
            {
                Id = eventData.EventId,
                EventType = nameof(UserViewedPostEvent),
                EventData = JsonSerializer.Serialize(eventData),
            };
            _context.Add(videoEvent);
            await _context.SaveChangesAsync();
        }

        public async Task SetViewToPost(Guid postId, Guid? userId, string? remoteIp)
        {
            await using var connection = await _messageBus.GetConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();
            await channel.ExchangeDeclareAsync(_reactionConfig.ExchangeName, ExchangeType.Direct, durable: true);
            await channel.QueueDeclareAsync(_reactionConfig.QueueName, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync(_reactionConfig.QueueName, _reactionConfig.ExchangeName, _reactionConfig.ViewRoutingKey);
            var now = DateTimeService.Now();
            var eventData = new UserViewedPostEvent(GuidService.GetNewGuid(), userId, postId, now, remoteIp);
            var videoEvent = new VideoEvent
            {
                Id = eventData.EventId,
                EventType = nameof(UserViewedPostEvent),
                EventData = JsonSerializer.Serialize(eventData),
            };
            _context.Add(videoEvent);
            await _context.SaveChangesAsync();
        }
    }
}

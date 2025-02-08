using RabbitMQ.Client;
using System.Text;
using MessageBus.Configs;
using Shared.Persistence;
using Profile.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MessageBus;
using System.Collections.Concurrent;
using Profile.Domain.Events;
using Microsoft.OpenApi.Writers;
using Infrastructure.Models;
using Shared.Services;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace ProfileApplication.HostedServices
{
    public class OutboxPublisherService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly RabbitMqConfig _settings;
        private readonly RabbitMqMessageBus _messageBus;
        private readonly ConcurrentDictionary<ulong, ProfileEventMessages> sendMessages = new();


        public OutboxPublisherService(
            IServiceProvider serviceProvider,
            RabbitMqConfig config,
            ILogger<OutboxPublisherService> logger,
            RabbitMqMessageBus messageBus)
        {
            _serviceProvider = serviceProvider;
            _settings = config;
            _messageBus = messageBus;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var connection = await _messageBus.GetConnectionAsync();
            var channelOpts = new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true,
                outstandingPublisherConfirmationsRateLimiter: new ThrottlingRateLimiter(50));
            using var channel = await connection.CreateChannelAsync(channelOpts);

            await channel.ExchangeDeclareAsync(_settings.ExchangeName, ExchangeType.Direct, durable: true);


            channel.BasicAcksAsync += async (sender, ea) =>
            {
                if (sendMessages.TryRemove(ea.DeliveryTag, out var value))
                {
                    await ProcessSuccess(value);
                }
            };

            channel.BasicReturnAsync += async (sender, ea) =>
            {
                Console.WriteLine(Encoding.UTF8.GetString(ea.Body.Span));
                //dbContext.Remove(message);
                //await dbContext.SaveChangesAsync();
            };

            channel.BasicNacksAsync += async (sender, ea) =>
            {
                if (sendMessages.TryGetValue(ea.DeliveryTag, out var value))
                {
                    await ProcessError(value);
                }
            };

            while (!stoppingToken.IsCancellationRequested)
            {
                //{
                //    var messagesCount = await channel.MessageCountAsync(_settings.VideoProcessQueue);
                //    using var localScope = _serviceProvider.CreateScope();
                //    var writeContext = localScope.ServiceProvider.GetRequiredService<IReadWriteRepository<IProfileEntity>>();
                //    if (messagesCount == 0 && await writeContext.Get<ProfileEventMessages>().AnyAsync(x => x.State == Infrastructure.Models.EventState.Processed))
                //    {
                //        var lostMessages = await writeContext.Get<ProfileEventMessages>()
                //            .Where(x => x.State == Infrastructure.Models.EventState.Processed)
                //            .ToListAsync();

                //        foreach (var i in lostMessages)
                //        {
                //            writeContext.Attach(i);
                //            i.State = Infrastructure.Models.EventState.Pending;
                //        }
                //        await writeContext.SaveChangesAsync();
                //    }
                //}
                using var scope = _serviceProvider.CreateScope();

                var dbContext = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IProfileEntity>>();

                var messages = await dbContext.Get<ProfileEventMessages>()
                    .Where(m => m.State == Infrastructure.Models.EventState.Pending && m.RetryCount < 3)
                    .OrderBy(m => m.CreatedAt)
                    .Take(100)
                    .ToListAsync(stoppingToken);

                foreach (var message in messages)
                {
                    var nextNumber = await channel.GetNextPublishSequenceNumberAsync();
                    try
                    {
                        var body = Encoding.UTF8.GetBytes(message.EventData);
                        sendMessages.TryAdd(nextNumber, message);
                        dbContext.Attach(message);
                        message.State = Infrastructure.Models.EventState.Processed;
                        await dbContext.SaveChangesAsync();

                        await channel.BasicPublishAsync(
                           exchange: _settings.ExchangeName,
                           routingKey: GetRoutingKey(message),
                           body: body,
                           mandatory: true
                       );
                    }
                    catch (Exception ex)
                    {
                        dbContext.Attach(message);
                        sendMessages.Remove(nextNumber, out _);
                        if (message.RetryCount == 3)
                        {
                            message.SetErrorMessage(ex.Message);
                        }
                        else
                        {
                            message.RetryCount++;
                            message.State = Infrastructure.Models.EventState.Pending;
                        }
                        await dbContext.SaveChangesAsync();
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Интервал опроса
            }
        }

        private async Task ProcessError(ProfileEventMessages message)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IProfileEntity>>();
            dbContext.Attach(message);
            message.RetryCount++;
            if (message.RetryCount > 3)
            {
                message.SetErrorMessage("Не удалось обработать событие");
                message.State = Infrastructure.Models.EventState.Error;
            }
            else
            {
                message.State = Infrastructure.Models.EventState.Pending;
            }
            await dbContext.SaveChangesAsync();
        }

        private async Task ProcessSuccess(ProfileEventMessages message)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IProfileEntity>>();
            //if (message.EventType == nameof(CombineFileChunksEvent))
            //{
            //    var @event = JsonSerializer.Deserialize<CombineFileChunksEvent>(message.EventData);
            //    var videoMetadata = await dbContext.Get<VideoMetadata>()
            //        .Where(x => x.Id == @event.VideoMetadataId)
            //        .Select(x => new
            //        {
            //            x.ObjectName,
            //            x.Id,
            //            x.Post.Blog.ProfileId
            //        })
            //        .FirstAsync();
            //    var videoEvent = new ProfileEventMessages
            //    {
            //        Id = GuidService.GetNewGuid(),
            //        EventData = JsonSerializer.Serialize(new VideoUploadEvent
            //        {
            //            Id = GuidService.GetNewGuid(),
            //            ObjectName = videoMetadata.ObjectName,
            //            UserProfileId = videoMetadata.ProfileId,
            //            FileId = videoMetadata.Id
            //        }),
            //        EventType = nameof(VideoUploadEvent),
            //        State = EventState.Pending,
            //    };
            //    dbContext.Add(videoEvent);
            //}

            dbContext.Attach(message);
            message.State = EventState.Complete;
            await dbContext.SaveChangesAsync();
        }

        private string GetRoutingKey(ProfileEventMessages message)
        {
            switch (message.EventType)
            {
                case nameof(VideoUploadEvent):
                    return _settings.VideoConverterRoutingKey;
                case nameof(CombineFileChunksEvent):
                    return _settings.FileChunksCombinerRoutingKey;
                //case nameof(VideoViewEvent):
                //return _settings.FileChunksCombinerRoutingKey;
                default:
                    throw new ArgumentException("Неизвестное событие");
            }
        }
    }
}

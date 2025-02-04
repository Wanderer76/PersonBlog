using RabbitMQ.Client;
using System.Text;
using MessageBus.Configs;
using Shared.Persistence;
using Profile.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MessageBus;
using System.Collections.Concurrent;

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
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IProfileEntity>>();


            channel.BasicAcksAsync += async (sender, ea) =>
            {
                if (sendMessages.TryGetValue(ea.DeliveryTag, out var value))
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

                        dbContext.Attach(message);
                        message.State = Infrastructure.Models.EventState.Processed;
                        await dbContext.SaveChangesAsync();

                        sendMessages.TryAdd(nextNumber, message);
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
            dbContext.Attach(message);
            message.State = Infrastructure.Models.EventState.Complete;
            await dbContext.SaveChangesAsync();
        }

        private string GetRoutingKey(ProfileEventMessages message)
        {
            return message.EventType == nameof(VideoUploadEvent) ? _settings.VideoConverterRoutingKey : _settings.FileChunksCombinerRoutingKey;
        }
    }
}

using FileStorage.Service.Service;
using MessageBus;
using MessageBus.Configs;
using Profile.Domain.Entities;
using RabbitMQ.Client;
using Shared.Persistence;
using VideoProcessing.Cli.Models;
using VideoProcessing.Cli.Service;

namespace VideoProcessing.Cli
{
    internal class VideoConverterHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScope;
        private readonly IFileStorageFactory _fileStorageFactory;
        private readonly IEnumerable<VideoPreset> _videoPresets;
        private readonly string _tempPath;
        private readonly RabbitMqMessageBus _messageBus;
        private readonly RabbitMqConfig _config;
        public VideoConverterHostedService(IServiceScopeFactory serviceScope,
            IFileStorageFactory fileStorageFactory, IConfiguration configuration, RabbitMqMessageBus messageBus, RabbitMqConfig config)
        {
            _serviceScope = serviceScope;
            _fileStorageFactory = fileStorageFactory;
            _tempPath = Path.GetFullPath(configuration["TempDir"]!);
            _videoPresets = configuration.GetSection("VideoPresets").Get<List<VideoPreset>>()!;
            _messageBus = messageBus;
            _config = config;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _ = ProcessUploadVideoEvents();
            return Task.CompletedTask;
        }

        private async Task ProcessUploadVideoEvents()
        {
            var connection = await _messageBus.GetConnectionAsync();
            var channelOpts = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true,
            outstandingPublisherConfirmationsRateLimiter: new ThrottlingRateLimiter(50));
            var videoConverterChannel = await connection.CreateChannelAsync(channelOpts);
            var fileChunkChannel = await connection.CreateChannelAsync(channelOpts);
            
            await videoConverterChannel.ExchangeDeclareAsync(_config.ExchangeName, ExchangeType.Direct, durable: true);
            await videoConverterChannel.QueueDeclareAsync(_config.VideoConverterQueue, durable: true, exclusive: false, autoDelete: false);
            await videoConverterChannel.QueueBindAsync(_config.VideoConverterQueue, _config.ExchangeName, _config.VideoConverterRoutingKey);
            await fileChunkChannel.ExchangeDeclareAsync(_config.ExchangeName, ExchangeType.Direct, durable: true);
            await fileChunkChannel.QueueDeclareAsync(_config.FileChunksCombinerQueue, durable: true, exclusive: false, autoDelete: false);
            await fileChunkChannel.QueueBindAsync(_config.FileChunksCombinerQueue, _config.ExchangeName, _config.FileChunksCombinerRoutingKey);

            try
            {
                using var scope = _serviceScope.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IProfileEntity>>();

                await _messageBus.SubscribeAsync<VideoUploadEvent>(videoConverterChannel,
                    _config.VideoConverterQueue,
                    _config.ExchangeName,
                    _config.VideoConverterRoutingKey,
                    async (e) =>
                    {
                        using var storage = _fileStorageFactory.CreateFileStorage();
                        using var scope = _serviceScope.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IProfileEntity>>();
                        await ConvertVideoFile.ProcessFile(dbContext, storage, _videoPresets, _tempPath, e);
                    });

                await _messageBus.SubscribeAsync<CombineFileChunksEvent>(fileChunkChannel,
                   _config.FileChunksCombinerQueue,
                   _config.ExchangeName,
                   _config.FileChunksCombinerRoutingKey,
                   async (e) =>
                   {
                       using var storage = _fileStorageFactory.CreateFileStorage();
                       using var scope = _serviceScope.CreateScope();
                       var dbContext = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IProfileEntity>>();
                       await VideoChunksCombinerService.ProcessChunks(dbContext, storage, e);
                   });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

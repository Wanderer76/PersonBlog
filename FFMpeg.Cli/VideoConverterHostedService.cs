using FFmpeg.Service.Models;
using FileStorage.Service.Service;
using MessageBus;
using MessageBus.Configs;
using Profile.Domain.Entities;
using Profile.Domain.Events;
using RabbitMQ.Client;
using Shared.Persistence;
using System.Text.Json;
using VideoProcessing.Cli.Service;

namespace VideoProcessing.Cli
{
    internal class VideoConverterHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScope;
        private readonly IFileStorageFactory _fileStorageFactory;
        private readonly HlsVideoPresets _videoPresets;
        private readonly string _tempPath;
        private readonly RabbitMqMessageBus _messageBus;
        private readonly RabbitMqConfig _config;
        public VideoConverterHostedService(IServiceScopeFactory serviceScope,
            IFileStorageFactory fileStorageFactory, IConfiguration configuration, RabbitMqMessageBus messageBus, RabbitMqConfig config, HlsVideoPresets videoPresets)
        {
            _serviceScope = serviceScope;
            _fileStorageFactory = fileStorageFactory;
            _tempPath = Path.GetFullPath(configuration["TempDir"]!);
            _messageBus = messageBus;
            _config = config;
            _videoPresets = videoPresets;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _ = ProcessUploadVideoEvents();
            return Task.CompletedTask;
        }

        private async Task ProcessUploadVideoEvents()
        {
            var connection = await _messageBus.GetConnectionAsync();
            var videoConverterChannel = await connection.CreateChannelAsync();

            await videoConverterChannel.ExchangeDeclareAsync(_config.ExchangeName, ExchangeType.Direct, durable: true);
            await videoConverterChannel.QueueDeclareAsync(_config.VideoProcessQueue, durable: true, exclusive: false, autoDelete: false);

            await videoConverterChannel.QueueBindAsync(_config.VideoProcessQueue, _config.ExchangeName, _config.VideoConverterRoutingKey);
            await videoConverterChannel.QueueBindAsync(_config.VideoProcessQueue, _config.ExchangeName, _config.FileChunksCombinerRoutingKey);

            try
            {
                using var scope = _serviceScope.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IProfileEntity>>();

                await _messageBus.SubscribeAsync(videoConverterChannel,
                    _config.VideoProcessQueue,
                    async (e) =>
                    {
                        var routingKey = e.RoutingKey;
                        var body = e.Body;
                        using var storage = _fileStorageFactory.CreateFileStorage();
                        using var scope = _serviceScope.CreateScope();
                        if (routingKey == _config.FileChunksCombinerRoutingKey)
                        {
                            var message = JsonSerializer.Deserialize<CombineFileChunksEvent>(body.Span)!;
                            await VideoChunksCombinerService.ProcessChunks(scope, storage, message);
                        }
                        else if (routingKey == _config.VideoConverterRoutingKey)
                        {
                            var message = JsonSerializer.Deserialize<VideoUploadEvent>(body.Span)!;
                            await ConvertVideoFile.ProcessFile(scope, storage, _videoPresets.VideoPresets, _tempPath, message);
                        }
                        else
                        {
                            throw new ArgumentException($"Неизвестный routingKey - {routingKey}");
                        }
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

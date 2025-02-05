//using FileStorage.Service.Models;
//using FileStorage.Service.Service;
//using Infrastructure.Models;
//using MessageBus;
//using MessageBus.Configs;
//using Microsoft.EntityFrameworkCore;
//using Profile.Domain.Entities;
//using RabbitMQ.Client;
//using Shared.Persistence;
//using Shared.Services;
//using System.Text.Json;
//using System.Threading.Channels;
//using VideoProcessing.Cli.Service;

//namespace VideoProcessing.Cli
//{
//    public class FileChunksCombinerHostedService : BackgroundService
//    {
//        private readonly IServiceScopeFactory _serviceScope;
//        private readonly IFileStorageFactory _fileStorageFactory;
//        private readonly RabbitMqMessageBus _messageBus;
//        private readonly RabbitMqConfig _config;

//        public FileChunksCombinerHostedService(IServiceScopeFactory serviceScope, IFileStorageFactory fileStorageFactory, RabbitMqMessageBus messageBus, RabbitMqConfig config)
//        {
//            _serviceScope = serviceScope;
//            _fileStorageFactory = fileStorageFactory;
//            _messageBus = messageBus;
//            _config = config;
//        }
//        protected override Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            _ = ProcessCombineVideoEvents();
//            return Task.CompletedTask;
//        }

//        private async Task ProcessCombineVideoEvents()
//        {
//            using var connection = await _messageBus.GetConnectionAsync();
//            using var channel = await connection.CreateChannelAsync();

//            await channel.ExchangeDeclareAsync(_config.ExchangeName, ExchangeType.Direct, durable: true);
//            await channel.QueueDeclareAsync(_config.FileChunksCombinerQueue, durable: true, exclusive: false, autoDelete: false);
//            await channel.QueueBindAsync(_config.FileChunksCombinerQueue, _config.ExchangeName, _config.FileChunksCombinerRoutingKey);

//            try
//            {
//                using var scope = _serviceScope.CreateScope();
//                var context = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IProfileEntity>>();

//                await _messageBus.SubscribeAsync<CombineFileChunksEvent>(channel,
//                    _config.FileChunksCombinerQueue,
//                    _config.ExchangeName,
//                    _config.FileChunksCombinerRoutingKey,
//                    async (e) =>
//                    {
//                        var storage = _fileStorageFactory.CreateFileStorage(); 
//                        using var scope = _serviceScope.CreateScope();
//                        var context = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IProfileEntity>>();
//                        await VideoChunksCombinerService.ProcessChunks(context,storage, e);
//                    });


//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.Message);
//            }
//        }


//        public Task StopAsync(CancellationToken cancellationToken)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}

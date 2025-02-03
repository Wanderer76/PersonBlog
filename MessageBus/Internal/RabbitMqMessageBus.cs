using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text.Json;
using System.Text;
using Shared.Services;
using RabbitMQ.Client.Events;
using System.Threading.Channels;

namespace MessageBus.Internal
{
    internal class RabbitMqMessageBus : IMessageBus
    {
        //private readonly IConnection _connection;
        //private readonly IModel _channel;
        private readonly IConnectionFactory _factory;
        private readonly string _exchangeName;

        public RabbitMqMessageBus(IConfiguration configuration)
        {
            _factory = new ConnectionFactory
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "admin",
                Password = "admin",
            };

        }

        public async Task<object> SendMessageAsync<T>(string queueName, T message, Action<T> onReceived)
        {
            try
            {
                using var connection = await _factory.CreateConnectionAsync();
                //var channelOpts = new CreateChannelOptions(
                //    publisherConfirmationsEnabled: true,
                //    publisherConfirmationTrackingEnabled: true,
                //    outstandingPublisherConfirmationsRateLimiter: new ThrottlingRateLimiter(50)
                //    );

                using var channel = await connection.CreateChannelAsync();
                //await channel.QueueDeclareAsync(queueName, durable: false,
                //           exclusive: false,
                //           autoDelete: false,
                //           arguments: null);
                //var consumer = new AsyncEventingBasicConsumer(channel);
                //var correlationId = GuidService.GetNewGuid().ToString();
                //consumer.ReceivedAsync += (model, ea) =>
                //{
                //    if (ea.BasicProperties.CorrelationId == correlationId)
                //    {
                //        var response = Encoding.UTF8.GetString(ea.Body.ToArray());
                //        return Task.FromResult(response);
                //    }
                //    return Task.CompletedTask;
                //};


                //channel.BasicConsumeAsync(queueName,autoAck:true)

                //channel.BasicReturnAsync += (sender, ea) =>
                //{
                //    var body = JsonSerializer.Deserialize<T>(ea.Body.Span);
                //    onReceived(body);
                //    Console.WriteLine(ea);
                //    return Task.CompletedTask;
                //};

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
                await channel.BasicPublishAsync(exchange: "test", routingKey: queueName, body: body);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return new object();

        }

        public async Task SubscribeAsync<T>(string queueName, Func<T, Task> messageHandler)
        {
            using var connection = await _factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();
            await channel.QueueBindAsync(queue: queueName, exchange: "test", routingKey: queueName);
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (model, ea) =>
            {
                byte[] body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($" [x] {message}");
                return Task.CompletedTask;
            };

            await channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);
        }

        public void Dispose()
        {
        }

        public Task<IConnection> GetConnectionAsync()
        {
            return _factory.CreateConnectionAsync();
        }

        public async Task SubscribeAsync<T>(IChannel channel, string queueName, Func<T, Task> messageHandler)
        {
            await channel.QueueBindAsync(queue: queueName, exchange: "test", routingKey: queueName);
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = JsonSerializer.Deserialize<T>(ea.Body.Span);
                await messageHandler(body);
                await channel.BasicAckAsync(ea.DeliveryTag, false);
            };

            await channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer);
        }
    }
}

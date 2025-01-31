using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text.Json;
using System.Text;

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
                Password = "admin"
            };

        }

        public async Task SendMessageAsync<T>(string queueName, T message)
        {
            try
            {
                using var connection = await _factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();
                await channel.QueueDeclareAsync(queueName);
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
                await channel.BasicPublishAsync(exchange: "", routingKey: queueName, body: body);
            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public Task SubscribeAsync<T>(string queueName, Func<T, Task> messageHandler)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }
    }
}

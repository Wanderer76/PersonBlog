using RabbitMQ.Client;
using System.Text.Json;
using System.Text;
using RabbitMQ.Client.Events;
using MessageBus.Configs;

namespace MessageBus
{
    public class RabbitMqMessageBus
    {
        private readonly IConnectionFactory _factory;

        public RabbitMqMessageBus(RabbitMqConfig config)
        {
            _factory = new ConnectionFactory
            {
                HostName = config.HostName,
                Port = config.Port,
                UserName = config.UserName,
                Password = config.Password,
            };
        }

        public async Task SendMessageAsync<T>(string exchangeName, string routingKey, T message, Action<T> onReceived)
        {
            try
            {
                using var connection = await _factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
                await channel.BasicPublishAsync(exchange: exchangeName, routingKey: routingKey, body: body);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void Dispose()
        {
        }

        public Task<IConnection> GetConnectionAsync()
        {
            return _factory.CreateConnectionAsync();
        }

        public async Task SubscribeAsync<T>(IChannel channel, string queueName, string exchangeName, string routingKey, Func<T, Task> messageHandler)
        {
            await channel.QueueBindAsync(queue: queueName, exchange: exchangeName, routingKey: routingKey);
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var q = routingKey;
                    try
                    {
                        var a = UTF8Encoding.UTF8.GetString(ea.Body.Span);
                        var b = typeof(T);
                        var body = JsonSerializer.Deserialize<T>(ea.Body.Span);
                        await messageHandler(body);
                    }
                    catch (Exception e)
                    {
                        throw;
                    }
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                }
            };
            await channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer);
        }
    }
}

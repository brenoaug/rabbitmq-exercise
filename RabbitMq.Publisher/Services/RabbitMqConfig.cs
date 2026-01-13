using RabbitMQ.Client;

namespace RabbitMq.Publisher.Configuration
{
    public static class RabbitMqConfiguration
    {
        private const string HOST_NAME = "localhost";
        private const string EXCHANGE_NAME = "topic_exchange";

        public static async Task<IChannel> CreateAndConfigureChannelAsync()
        {
            // 1. Criar conexão
            var factory = new ConnectionFactory() { HostName = HOST_NAME };
            var connection = await factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();

            // 2. Declarar exchange
            await channel.ExchangeDeclareAsync(
                exchange: EXCHANGE_NAME,
                type: ExchangeType.Topic,
                durable: false,
                autoDelete: false,
                arguments: null);

            // 3. Declarar todas as filas
            await channel.QueueDeclareAsync(
                queue: "queue0",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            await channel.QueueDeclareAsync(
                queue: "queue1",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            await channel.QueueDeclareAsync(
                queue: "queue2",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // 4. Fazer bind das filas ao exchange
            await channel.QueueBindAsync(
                queue: "queue0",
                exchange: EXCHANGE_NAME,
                routingKey: "greeting.message");

            await channel.QueueBindAsync(
                queue: "queue1",
                exchange: EXCHANGE_NAME,
                routingKey: "bye.message");

            await channel.QueueBindAsync(
                queue: "queue2",
                exchange: EXCHANGE_NAME,
                routingKey: "*.message");

            return channel;
        }

        public static async Task PublishMessageAsync(IChannel channel, string routingKey, byte[] body)
        {
            await channel.BasicPublishAsync(
                exchange: EXCHANGE_NAME,
                routingKey: routingKey,
                mandatory: false,
                body: body,
                cancellationToken: default);
        }
    }
}
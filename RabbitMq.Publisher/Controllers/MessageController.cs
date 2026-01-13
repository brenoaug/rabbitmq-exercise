using Microsoft.AspNetCore.Mvc;
using RabbitMq.Publisher.Model;
using RabbitMQ.Client;

namespace RabbitMq.Publisher.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private static List<Message> Messages = new List<Message>();

        [HttpGet]
        public async Task<IEnumerable<Message>> GetMessage()
        {
            return Messages;
        }

        [HttpPost("greeting")]
        public async Task<IActionResult> PostMessageGreeting([FromBody] Message message)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            // 1. Declara o exchange
            await channel.ExchangeDeclareAsync(
                exchange: "topic_exchange",
                type: ExchangeType.Topic);

            // 2. Declara duas filas ANTES de fazer bind
            await channel.QueueDeclareAsync(
                queue: "queue0",
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

            // 3. Faz bind das fila aos exchange
            await channel.QueueBindAsync(
                queue: "queue0",
                exchange: "topic_exchange",
                routingKey: "greeting.message");

            await channel.QueueBindAsync(
                queue: "queue2",
                exchange: "topic_exchange",
                routingKey: "*.message");

            var body = System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(message));

            // 4. Publica no exchange com routing key
            await channel.BasicPublishAsync(
                 exchange: "topic_exchange",
                 routingKey: "greeting.message",
                 mandatory: false,
                 body: body,
                 cancellationToken: default);

            Messages.Add(message);
            return Accepted(new { status = "Mensagem de saudação enviada", message });
        }

        [HttpPost("bye")]
        public async Task<IActionResult> PostMessageBye([FromBody] Message message)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            // 1. Declara o exchange
            await channel.ExchangeDeclareAsync(
                exchange: "topic_exchange",
                type: ExchangeType.Topic);

            // 2. Declara a fila ANTES de fazer bind
            await channel.QueueDeclareAsync(
                queue: "queue1",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // 3. Faz bind da fila ao exchange
            await channel.QueueBindAsync(
                queue: "queue1",
                exchange: "topic_exchange",
                routingKey: "bye.message");

            var body = System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(message));

            // 4. Publica no exchange com routing key
            await channel.BasicPublishAsync(
                 exchange: "topic_exchange",
                 routingKey: "bye.message",
                 mandatory: false,
                 body: body,
                 cancellationToken: default);

            Messages.Add(message);
            return Accepted(new { status = "Mensagem de despedida enviada", message });
        }
    }
}

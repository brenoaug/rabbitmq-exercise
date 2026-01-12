using Microsoft.AspNetCore.Mvc;
using RabbitMq.Publisher.Model;
using RabbitMQ.Client;
using System.Threading;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace RabbitMq.Publisher.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {

        private static List<Message> Messages = new List<Message>();

        [HttpGet]
        public async Task<IEnumerable<Message>> GetMessage()
        {
            return Messages;
        }

        [HttpPost]
        public async Task<IActionResult> PostMessage([FromBody] Message message)
        {

            var factory = new ConnectionFactory() { HostName = "localhost" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();


            // 1. Declara o exchange
            await channel.ExchangeDeclareAsync(
                exchange: "direct_exchange",
                type: ExchangeType.Direct);


            // 2. Declara a fila ANTES de fazer bind
            await channel.QueueDeclareAsync(
                queue: "queue0",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // 3. Faz bind da fila ao exchange
            await channel.QueueBindAsync(
                queue: "queue0",
                exchange: "direct_exchange",    
                routingKey: "route0");

            var body = System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(message));


            // 4. Publica no exchange com routing key
            await channel.BasicPublishAsync(
                 exchange: "direct_exchange",
                 routingKey: "route0",
                 mandatory: false,
                 body: body,
                 cancellationToken: default);


            Messages.Add(message);
            return Accepted(new { status = "Mensagem enviada", message });
        }
    }
}

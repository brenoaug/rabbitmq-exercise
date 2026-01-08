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

            await channel.QueueDeclareAsync(queue: "message_queue",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

            var body = System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(message));

            await channel.BasicPublishAsync(
                                 exchange: string.Empty,
                                 routingKey: "message_queue",
                                 mandatory: false,
                                 body: body,
                                 cancellationToken: default);


            Messages.Add(message);
            return Accepted(new { status = "Mensagem enviada", message });
        }
    }
}

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

            //declara uma queue com nome aleatório
            QueueDeclareOk queueDeclareResult = await channel.QueueDeclareAsync();
            string queueName = queueDeclareResult.QueueName;

            await channel.ExchangeDeclareAsync(
                exchange: "fanout_exchange",
                type: ExchangeType.Fanout);

            await channel.QueueBindAsync(
                queue: queueName,
                exchange: "fanout_exchange",
                routingKey: string.Empty,
                arguments: null);

//            await channel.QueueDeclareAsync(
//                queue: queueName,                    
//                durable: false,
//                exclusive: false,
//                autoDelete: false,
//                arguments: null);

            var body = System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(message));

            await channel.BasicPublishAsync(
                 exchange: "fanout_exchange",
                 routingKey: string.Empty,
                 mandatory: false,
                 body: body,
                 cancellationToken: default);


            Messages.Add(message);
            return Accepted(new { status = "Mensagem enviada", message });
        }
    }
}

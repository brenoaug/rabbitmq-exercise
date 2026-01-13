using Microsoft.AspNetCore.Mvc;
using RabbitMq.Publisher.Configuration;
using RabbitMq.Publisher.Model;
using System.Text;
using System.Text.Json;

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
            using var channel = await RabbitMqConfiguration.CreateAndConfigureChannelAsync();

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            await RabbitMqConfiguration.PublishMessageAsync(channel, "greeting.message", body);

            Messages.Add(message);
            return Accepted(new { status = "Mensagem de saudação enviada", message });
        }

        [HttpPost("bye")]
        public async Task<IActionResult> PostMessageBye([FromBody] Message message)
        {
            using var channel = await RabbitMqConfiguration.CreateAndConfigureChannelAsync();

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            await RabbitMqConfiguration.PublishMessageAsync(channel, "bye.message", body);

            Messages.Add(message);
            return Accepted(new { status = "Mensagem de despedida enviada", message });
        }
    }
}
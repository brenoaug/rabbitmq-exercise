using Microsoft.AspNetCore.Mvc;
using RabbitMq.Publisher.Model;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace RabbitMq.Publisher.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        public static List<Message> Messages = [];

        [HttpGet]
        public IEnumerable<Message> GetMessage()
        {
            return Messages;
        }

        [HttpPost]
        public IActionResult Post([FromBody] Message message)
        {
            Messages.Add(message);
            return Accepted(message);
        }
    }
}

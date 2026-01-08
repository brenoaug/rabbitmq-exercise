using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

await channel.QueueDeclareAsync(
    queue: "message_queue", 
    durable: false, 
    exclusive: false, 
    autoDelete: false,
    arguments: null);

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Thread.Sleep(5000);
    Console.WriteLine($" Mensagem Recebida {message}");
     // Simula algum processamento
    return Task.CompletedTask;
};

await channel.BasicConsumeAsync("message_queue", autoAck: true, consumer: consumer);

await Task.Delay(Timeout.Infinite);

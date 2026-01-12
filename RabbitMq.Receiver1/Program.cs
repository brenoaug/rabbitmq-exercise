using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

// 1. Declara o exchange
await channel.ExchangeDeclareAsync(
    exchange: "direct_exchange",
    type: ExchangeType.Direct);

// 2. Declara a fila nomeada "queue"
await channel.QueueDeclareAsync(
    queue: "queue1",
    durable: false,
    exclusive: false,
    autoDelete: false);

// 3. Faz bind da fila ao exchange com routing key "route1"
await channel.QueueBindAsync(
    queue: "queue1",
    exchange: "direct_exchange",
    routingKey: "route1");

Console.WriteLine("Fila 'queue1' vinculada ao exchange 'direct_exchange' com routing key 'route1'");

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);

    // Simula processamento sem bloquear thread
    await Task.Delay(5000);

    Console.WriteLine($"Mensagem Recebida: {message}");
};

await channel.BasicConsumeAsync(
    "queue1", 
    autoAck: true, 
    consumer: consumer);

Console.WriteLine("Aguardando mensagens. Pressione Ctrl+C para sair.");
await Task.Delay(Timeout.Infinite);

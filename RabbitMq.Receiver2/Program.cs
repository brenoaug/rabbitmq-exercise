using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

const string QUEUE_NAME = "queue2";
const string EXCHANGE_NAME = "topic_exchange";
const string ROUTING_KEY = "*.message";

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

// 1. Declara o exchange
await channel.ExchangeDeclareAsync(
    exchange: EXCHANGE_NAME,
    type: ExchangeType.Topic);

// 2. Declara a fila nomeada "queue"
await channel.QueueDeclareAsync(
    queue: QUEUE_NAME,
    durable: false,
    exclusive: false,
    autoDelete: false);

// 3. Faz bind da fila ao exchange com routing key "route1"
await channel.QueueBindAsync(
    queue: QUEUE_NAME,
    exchange: EXCHANGE_NAME,
    routingKey: ROUTING_KEY);

Console.WriteLine($"Fila {QUEUE_NAME} vinculada ao exchange {EXCHANGE_NAME} com routing key {ROUTING_KEY}");

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
    QUEUE_NAME,
    autoAck: true,
    consumer: consumer);

Console.WriteLine("Aguardando mensagens. Pressione Ctrl+C para sair.");
await Task.Delay(Timeout.Infinite);

# Referência Rápida - RabbitMQ APIs

> **Cheat Sheet completo de todas as APIs e comandos usados no projeto**

---

## ConnectionFactory

### Criar Factory

```csharp
var factory = new ConnectionFactory
{
    HostName = "localhost",    // Servidor RabbitMQ
    Port = 5672,               // Porta padrão (opcional)
    UserName = "guest",        // Usuário (opcional, default: guest)
    Password = "guest",        // Senha (opcional, default: guest)
    VirtualHost = "/",         // Virtual host (opcional, default: /)
    
    // Configurações avançadas
    RequestedHeartbeat = TimeSpan.FromSeconds(60),
    AutomaticRecoveryEnabled = true,
    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
};
```

### Criar Conexão

```csharp
// Síncrono (obsoleto)
var connection = factory.CreateConnection();

// Assíncrono (recomendado)
var connection = await factory.CreateConnectionAsync();
```

---

## IConnection

### Propriedades

```csharp
bool IsOpen                  // Verifica se conexão está aberta
string ClientProvidedName    // Nome da conexão
```

### Métodos

```csharp
// Criar canal
IChannel channel = await connection.CreateChannelAsync();

// Fechar conexão
await connection.CloseAsync();

// Dispose
await connection.DisposeAsync();
```

---

## IChannel

### Exchange Operations

```csharp
// Declarar Exchange
await channel.ExchangeDeclareAsync(
    exchange: "my_exchange",        // Nome do exchange
    type: ExchangeType.Topic,       // Tipo: Direct, Fanout, Topic, Headers
    durable: false,                 // Sobrevive a restart?
    autoDelete: false,              // Deleta quando não usado?
    arguments: null                 // Argumentos extras
);

// Tipos de Exchange disponíveis
ExchangeType.Direct   // Roteamento exato por routing key
ExchangeType.Fanout   // Broadcast para todas filas
ExchangeType.Topic    // Padrões com wildcards
ExchangeType.Headers  // Roteamento por headers

// Deletar Exchange
await channel.ExchangeDeleteAsync(
    exchange: "my_exchange",
    ifUnused: false                 // Só deleta se não estiver em uso
);

// Bind entre Exchanges
await channel.ExchangeBindAsync(
    destination: "dest_exchange",
    source: "source_exchange",
    routingKey: "my.routing.key",
    arguments: null
);
```

### Queue Operations

```csharp
// Declarar Queue
QueueDeclareOk result = await channel.QueueDeclareAsync(
    queue: "my_queue",              // Nome da fila ("" = nome aleatório)
    durable: false,                 // Persiste ao restart?
    exclusive: false,               // Exclusiva para esta conexão?
    autoDelete: false,              // Deleta quando não há consumers?
    arguments: null                 // Argumentos extras (ver abaixo)
);

// Propriedades do resultado
string queueName = result.QueueName;      // Nome da fila criada
uint messageCount = result.MessageCount;  // Mensagens na fila
uint consumerCount = result.ConsumerCount; // Consumers conectados

// Argumentos especiais para queues
var arguments = new Dictionary<string, object>
{
    { "x-message-ttl", 60000 },              // TTL de mensagens (ms)
    { "x-max-length", 1000 },                // Máximo de mensagens
    { "x-max-length-bytes", 1048576 },       // Máximo de bytes (1MB)
    { "x-overflow", "reject-publish" },      // O que fazer quando cheio
    { "x-dead-letter-exchange", "dlx" },     // Exchange para msgs mortas
    { "x-dead-letter-routing-key", "dlq" },  // Routing key para DLQ
    { "x-max-priority", 10 },                // Máximo de prioridade
    { "x-queue-mode", "lazy" }               // Modo lazy (salva em disco)
};

// Deletar Queue
await channel.QueueDeleteAsync(
    queue: "my_queue",
    ifUnused: false,                // Só deleta se não há consumers
    ifEmpty: false                  // Só deleta se vazia
);

// Purgar Queue (remover todas mensagens)
await channel.QueuePurgeAsync(queue: "my_queue");
```

### Binding Operations

```csharp
// Bind Queue ? Exchange
await channel.QueueBindAsync(
    queue: "my_queue",
    exchange: "my_exchange",
    routingKey: "my.routing.key",
    arguments: null
);

// Unbind Queue
await channel.QueueUnbindAsync(
    queue: "my_queue",
    exchange: "my_exchange",
    routingKey: "my.routing.key",
    arguments: null
);
```

### Publish Operations

```csharp
// Publicar mensagem (simples)
await channel.BasicPublishAsync(
    exchange: "my_exchange",
    routingKey: "my.routing.key",
    mandatory: false,               // Retorna se não roteada?
    body: messageBytes,
    cancellationToken: default
);

// Publicar com propriedades
var props = channel.CreateBasicProperties();
props.ContentType = "application/json";
props.ContentEncoding = "utf-8";
props.Persistent = true;            // Persiste ao restart
props.Priority = 5;                 // Prioridade (0-10)
props.CorrelationId = Guid.NewGuid().ToString();
props.ReplyTo = "callback_queue";
props.Expiration = "60000";         // TTL da mensagem (ms)
props.MessageId = Guid.NewGuid().ToString();
props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
props.Type = "Order.Created";
props.UserId = "system";
props.AppId = "MyApp";

// Headers customizados
props.Headers = new Dictionary<string, object>
{
    { "custom-header", "value" },
    { "retry-count", 0 }
};

await channel.BasicPublishAsync(
    exchange: "my_exchange",
    routingKey: "my.routing.key",
    mandatory: false,
    basicProperties: props,
    body: messageBytes,
    cancellationToken: default
);
```

### Consume Operations

```csharp
// Configurar QoS (Quality of Service)
await channel.BasicQosAsync(
    prefetchSize: 0,                // 0 = sem limite de tamanho
    prefetchCount: 1,               // Quantas mensagens prefetch
    global: false                   // Aplica ao canal ou consumidor
);

// Criar Consumer
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (sender, ea) =>
{
    // ea.Body           - Bytes da mensagem
    // ea.RoutingKey     - Routing key usada
    // ea.Exchange       - Exchange de origem
    // ea.DeliveryTag    - ID único da entrega
    // ea.BasicProperties - Propriedades da mensagem
    
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    
    Console.WriteLine($"Recebido: {message}");
    
    // Processar mensagem
    await ProcessAsync(message);
};

// Iniciar consumo
string consumerTag = await channel.BasicConsumeAsync(
    queue: "my_queue",
    autoAck: true,                  // Auto-acknowledge?
    consumerTag: "",                // Tag do consumer (opcional)
    noLocal: false,                 // Não recebe próprias msgs
    exclusive: false,               // Consumer exclusivo?
    arguments: null,
    consumer: consumer
);

// Cancelar consumo
await channel.BasicCancelAsync(consumerTag);
```

### Acknowledgment Operations

```csharp
// Confirmar mensagem (ACK)
channel.BasicAck(
    deliveryTag: ea.DeliveryTag,
    multiple: false                 // ACK múltiplas mensagens?
);

// Rejeitar mensagem (NACK)
channel.BasicNack(
    deliveryTag: ea.DeliveryTag,
    multiple: false,
    requeue: true                   // Reenviar para fila?
);

// Rejeitar mensagem (alternativa)
channel.BasicReject(
    deliveryTag: ea.DeliveryTag,
    requeue: true
);
```

### Get Operations (Pull Mode)

```csharp
// Buscar uma mensagem (não recomendado para produção)
BasicGetResult result = await channel.BasicGetAsync(
    queue: "my_queue",
    autoAck: false
);

if (result != null)
{
    var body = result.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    
    Console.WriteLine($"Recebido: {message}");
    
    // Confirmar
    channel.BasicAck(result.DeliveryTag, false);
}
else
{
    Console.WriteLine("Fila vazia");
}
```

---

## Patterns de Routing Key (Topic Exchange)

### Wildcards

```
*  ?  Substitui EXATAMENTE uma palavra
#  ?  Substitui ZERO ou MAIS palavras
```

### Exemplos

```csharp
// Bindings
queue1 ? "user.created"         // Exato
queue2 ? "user.*"               // user + qualquer palavra
queue3 ? "user.#"               // user + zero ou mais palavras
queue4 ? "*.created"            // qualquer + created
queue5 ? "#.error"              // qualquer coisa terminando em error
queue6 ? "#"                    // TODAS mensagens

// Routing Keys
"user.created"         ? queue1, queue2, queue3, queue4
"user.updated"         ? queue2, queue3
"user.deleted.v2"      ? queue3
"order.created"        ? queue4
"payment.error"        ? queue5
"system.critical.error"? queue5, queue6
```

---

## Utilitários

### Encoding

```csharp
// Converter string ? bytes
byte[] bytes = Encoding.UTF8.GetBytes(jsonString);

// Converter bytes ? string
string text = Encoding.UTF8.GetString(bytes);

// Configurar encoding do console
Console.OutputEncoding = Encoding.UTF8;
```

### Serialização

```csharp
using System.Text.Json;

// Serializar objeto ? JSON
var message = new Message { Title = "Teste" };
string json = JsonSerializer.Serialize(message);

// Deserializar JSON ? objeto
var message = JsonSerializer.Deserialize<Message>(json);

// Com opções
var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    WriteIndented = true
};
string json = JsonSerializer.Serialize(message, options);
```

---

## Management API (HTTP)

### Base URL
```
http://localhost:15672/api
```

### Authentication
```
Username: guest
Password: guest
```

### Endpoints Úteis

```bash
# Listar exchanges
GET /api/exchanges

# Listar queues
GET /api/queues

# Detalhes de uma queue
GET /api/queues/{vhost}/{queue}

# Listar bindings
GET /api/bindings

# Publicar mensagem (teste)
POST /api/exchanges/{vhost}/{exchange}/publish
Content-Type: application/json
{
  "properties": {},
  "routing_key": "my.routing.key",
  "payload": "hello",
  "payload_encoding": "string"
}

# Consumir mensagens (teste)
POST /api/queues/{vhost}/{queue}/get
Content-Type: application/json
{
  "count": 5,
  "ackmode": "ack_requeue_true",
  "encoding": "auto"
}
```

### Exemplo com cURL

```bash
# Listar filas
curl -u guest:guest http://localhost:15672/api/queues

# Publicar mensagem
curl -u guest:guest \
  -H "Content-Type: application/json" \
  -X POST \
  -d '{"properties":{},"routing_key":"test","payload":"hello","payload_encoding":"string"}' \
  http://localhost:15672/api/exchanges/%2F/topic_exchange/publish

# Ver mensagens em uma fila
curl -u guest:guest \
  -H "Content-Type: application/json" \
  -X POST \
  -d '{"count":10,"ackmode":"ack_requeue_false","encoding":"auto"}' \
  http://localhost:15672/api/queues/%2F/queue0/get
```

---

## Docker Commands

### RabbitMQ Container

```bash
# Iniciar RabbitMQ com Management
docker run -d \
  --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  -e RABBITMQ_DEFAULT_USER=admin \
  -e RABBITMQ_DEFAULT_PASS=admin123 \
  rabbitmq:3-management

# Ver logs
docker logs -f rabbitmq

# Parar
docker stop rabbitmq

# Iniciar novamente
docker start rabbitmq

# Remover
docker rm -f rabbitmq

# Entrar no container
docker exec -it rabbitmq bash

# Listar filas dentro do container
docker exec rabbitmq rabbitmqctl list_queues

# Listar exchanges
docker exec rabbitmq rabbitmqctl list_exchanges

# Limpar todas mensagens
docker exec rabbitmq rabbitmqctl reset
```

### Docker Compose

```yaml
# docker-compose.yml
version: '3.8'
services:
  rabbitmq:
    image: rabbitmq:3-management
    container_name: rabbitmq
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: admin
      RABBITMQ_DEFAULT_PASS: admin123
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    networks:
      - app_network

volumes:
  rabbitmq_data:

networks:
  app_network:
```

```bash
# Iniciar
docker-compose up -d

# Parar
docker-compose down

# Ver logs
docker-compose logs -f rabbitmq
```

---

## RabbitMQCtl Commands

```bash
# Status do servidor
rabbitmqctl status

# Listar usuários
rabbitmqctl list_users

# Criar usuário
rabbitmqctl add_user myuser mypassword

# Dar permissões
rabbitmqctl set_permissions -p / myuser ".*" ".*" ".*"

# Listar virtual hosts
rabbitmqctl list_vhosts

# Listar conexões
rabbitmqctl list_connections

# Listar canais
rabbitmqctl list_channels

# Listar filas
rabbitmqctl list_queues name messages consumers

# Listar exchanges
rabbitmqctl list_exchanges

# Listar bindings
rabbitmqctl list_bindings

# Purgar fila
rabbitmqctl purge_queue queue_name

# Deletar fila
rabbitmqctl delete_queue queue_name

# Fechar todas conexões
rabbitmqctl close_all_connections "Admin maintenance"

# Reset (cuidado! Apaga tudo)
rabbitmqctl reset

# Parar aplicação (sem parar Erlang VM)
rabbitmqctl stop_app

# Iniciar aplicação
rabbitmqctl start_app
```

---

## Monitoring & Debugging

### Prometheus Metrics

```csharp
// Instalar
// dotnet add package prometheus-net
// dotnet add package prometheus-net.AspNetCore

using Prometheus;

// Definir métricas
private static readonly Counter MessagesPublished = Metrics
    .CreateCounter("messages_published_total", "Total de mensagens publicadas");

private static readonly Histogram MessageProcessingDuration = Metrics
    .CreateHistogram("message_processing_duration_seconds", "Duração do processamento");

// Usar
MessagesPublished.Inc();

using (MessageProcessingDuration.NewTimer())
{
    await ProcessMessageAsync(message);
}
```

### Health Checks

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddRabbitMQ(
        rabbitConnectionString: "amqp://localhost:5672",
        name: "rabbitmq",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready" }
    );

app.MapHealthChecks("/health");
```

---

## Best Practices

### DO (Recomendado)

```csharp
// Use async/await
await channel.BasicPublishAsync(...);

// Configure UTF-8 encoding
Console.OutputEncoding = Encoding.UTF8;

// Use using para dispose automático
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

// Declare idempotentemente (seguro chamar múltiplas vezes)
await channel.ExchangeDeclareAsync(...);
await channel.QueueDeclareAsync(...);

// Use CancellationToken
await PublishAsync(message, cancellationToken);

// Log erros
try { ... } catch (Exception ex) { logger.LogError(ex, "..."); }
```

### DON'T (Evitar)

```csharp
// Não use Thread.Sleep em async
// ERRADO: Thread.Sleep(5000);
// CORRETO: await Task.Delay(5000);

// Não ignore exceções
// ERRADO: try { ... } catch { }
// CORRETO: try { ... } catch (Exception ex) { Log(ex); throw; }

// Não crie conexão por mensagem
// ERRADO: foreach (var msg in msgs) { var conn = CreateConnection(); ... }
// CORRETO: Reutilize conexão

// Não misture sync e async
// ERRADO: connection.CreateChannel(); await PublishAsync();
// CORRETO: Tudo async

// Não use AutoAck em produção (sem try/catch)
// ERRADO: autoAck: true + código que pode falhar
// CORRETO: autoAck: false + try/catch + BasicAck
```

---

## Links Úteis

### Documentação
- [RabbitMQ Official Docs](https://www.rabbitmq.com/documentation.html)
- [.NET Client API](https://www.rabbitmq.com/dotnet-api-guide.html)
- [Tutorials](https://www.rabbitmq.com/getstarted.html)

### Ferramentas
- [RabbitMQ Management](http://localhost:15672)
- [RabbitMQ Simulator](http://tryrabbitmq.com/)
- [RabbitMQ Shovel Plugin](https://www.rabbitmq.com/shovel.html)

### Packages NuGet
```bash
dotnet add package RabbitMQ.Client
dotnet add package Polly
dotnet add package Serilog
dotnet add package prometheus-net
```

---

**Salve este arquivo como referência rápida!**


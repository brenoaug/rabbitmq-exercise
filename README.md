# RabbitMQ Topic Exchange - Guia Completo

> **Projeto educacional**: Sistema de mensageria usando RabbitMQ com Topic Exchange em .NET 8

---

## Índice

1. [Visão Geral](#visão-geral)
2. [Arquitetura](#arquitetura)
3. [O que é RabbitMQ](#o-que-é-rabbitmq)
4. [Topic Exchange](#topic-exchange)
5. [Como Rodar](#como-rodar)
6. [Estrutura do Projeto](#estrutura-do-projeto)
7. [Exemplos de Uso](#exemplos-de-uso)
8. [Referência de APIs](#referência-de-apis)
9. [Troubleshooting](#troubleshooting)
10. [Best Practices](#best-practices)
11. [Recursos Adicionais](#recursos-adicionais)

---

## Visão Geral

Este projeto demonstra o uso de **RabbitMQ** com **Topic Exchange** para criar um sistema de mensagens flexível.

### Componentes

| Componente | Tipo | Porta | Descrição |
|------------|------|-------|-----------|
| **RabbitMq.Publisher** | ASP.NET Core Web API | 7173/5135 | Publica mensagens |
| **RabbitMq.Receiver** | Console App | - | Consome queue0 (greeting) |
| **RabbitMq.Receiver1** | Console App | - | Consome queue1 (bye) |
| **RabbitMq.Receiver2** | Console App | - | Consome queue2 (todas) |

---

## Arquitetura

```
Publisher (API)
     ?
     ? Publica mensagem
     ?
??????????????????????????????
?  topic_exchange (RabbitMQ) ?
??????????????????????????????
      ?         ?         ?
      ?         ?         ?
   queue0    queue1   queue2
      ?         ?         ?
      ?         ?         ?
  Receiver  Receiver1 Receiver2
```

### Fluxo de Mensagens

**Cenário 1: POST /greeting**
- greeting.message ? queue0 + queue2
- Receiver e Receiver2 recebem

**Cenário 2: POST /bye**
- bye.message ? queue1 + queue2
- Receiver1 e Receiver2 recebem

---

## O que é RabbitMQ

**RabbitMQ** é um **message broker** que:
- Recebe mensagens de produtores
- Armazena em filas
- Entrega para consumidores

### Vantagens

- **Desacoplamento**: Publisher não conhece Receiver
- **Assíncrono**: Não bloqueia processamento
- **Confiável**: Garante entrega
- **Escalável**: Adicione receivers facilmente

---

## Topic Exchange

### Wildcards

| Símbolo | Significado | Exemplo |
|---------|-------------|---------|
| `*` | Exatamente uma palavra | `*.message` = `greeting.message` |
| `#` | Zero ou mais palavras | `order.#` = `order.paid.email` |

### Exemplos

```
queue0 ? "greeting.message"  (exato)
queue1 ? "bye.message"       (exato)
queue2 ? "*.message"         (wildcard)

Resultados:
greeting.message ? queue0 + queue2
bye.message      ? queue1 + queue2
outro.message    ? queue2
```

---

## Como Rodar

### 1. Instalar RabbitMQ

**Docker (Recomendado):**
```bash
docker run -d --name rabbitmq \
  -p 5672:5672 -p 15672:15672 \
  rabbitmq:3-management
```

### 2. Executar Projetos

**Terminal 1 - Publisher:**
```bash
cd RabbitMq.Publisher
dotnet run
```

**Terminal 2 - Receiver:**
```bash
cd RabbitMq.Receiver
dotnet run
```

**Terminal 3 - Receiver1:**
```bash
cd RabbitMq.Receiver1
dotnet run
```

**Terminal 4 - Receiver2:**
```bash
cd RabbitMq.Receiver2
dotnet run
```

### 3. Acessar

- **Swagger**: https://localhost:7173/swagger
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)

---

## Estrutura do Projeto

```
RabbitMq.Publisher/
??? Controllers/MessageController.cs   # Endpoints HTTP
??? Services/RabbitMqConfig.cs         # Setup RabbitMQ

RabbitMq.Receiver/
??? Program.cs                         # Consome queue0

RabbitMq.Receiver1/
??? Program.cs                         # Consome queue1

RabbitMq.Receiver2/
??? Program.cs                         # Consome queue2
```

### Código Principal

**Publisher (MessageController.cs):**
```csharp
[HttpPost("greeting")]
public async Task<IActionResult> PostMessageGreeting([FromBody] Message message)
{
    using var channel = await RabbitMqConfiguration.CreateAndConfigureChannelAsync();
    var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
    await RabbitMqConfiguration.PublishMessageAsync(channel, "greeting.message", body);
    return Accepted(new { status = "Mensagem enviada", message });
}
```

**RabbitMQ Configuration:**
```csharp
public static async Task<IChannel> CreateAndConfigureChannelAsync()
{
    var factory = new ConnectionFactory() { HostName = "localhost" };
    var connection = await factory.CreateConnectionAsync();
    var channel = await connection.CreateChannelAsync();

    await channel.ExchangeDeclareAsync(exchange: "topic_exchange", type: ExchangeType.Topic);
    
    await channel.QueueDeclareAsync(queue: "queue0", ...);
    await channel.QueueDeclareAsync(queue: "queue1", ...);
    await channel.QueueDeclareAsync(queue: "queue2", ...);

    await channel.QueueBindAsync(queue: "queue0", routingKey: "greeting.message");
    await channel.QueueBindAsync(queue: "queue1", routingKey: "bye.message");
    await channel.QueueBindAsync(queue: "queue2", routingKey: "*.message");

    return channel;
}
```

**Receiver:**
```csharp
var factory = new ConnectionFactory { HostName = "localhost" };
var connection = await factory.CreateConnectionAsync();
var channel = await connection.CreateChannelAsync();

await channel.QueueDeclareAsync(queue: "queue0", ...);
await channel.QueueBindAsync(queue: "queue0", routingKey: "greeting.message");

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    var message = Encoding.UTF8.GetString(ea.Body.ToArray());
    await Task.Delay(5000);
    Console.WriteLine($"Mensagem Recebida: {message}");
};

await channel.BasicConsumeAsync("queue0", autoAck: true, consumer);
await Task.Delay(Timeout.Infinite);
```

---

## Exemplos de Uso

### 1. Enviar Greeting

**cURL:**
```bash
curl -X POST https://localhost:7173/Message/greeting \
  -H "Content-Type: application/json" \
  -d '{"title":"Olá","text":"Mundo","author":"João"}'
```

**Resultado:**
- Receiver (queue0): Recebe
- Receiver2 (queue2): Recebe
- Receiver1 (queue1): Não recebe

### 2. Enviar Bye

**cURL:**
```bash
curl -X POST https://localhost:7173/Message/bye \
  -H "Content-Type: application/json" \
  -d '{"title":"Tchau","text":"Até","author":"Maria"}'
```

**Resultado:**
- Receiver (queue0): Não recebe
- Receiver1 (queue1): Recebe
- Receiver2 (queue2): Recebe

---

## Referência de APIs

### ConnectionFactory

```csharp
var factory = new ConnectionFactory
{
    HostName = "localhost",
    Port = 5672,
    UserName = "guest",
    Password = "guest"
};
var connection = await factory.CreateConnectionAsync();
```

### Exchange Operations

```csharp
await channel.ExchangeDeclareAsync(
    exchange: "topic_exchange",
    type: ExchangeType.Topic,
    durable: false,
    autoDelete: false
);
```

### Queue Operations

```csharp
await channel.QueueDeclareAsync(
    queue: "my_queue",
    durable: false,
    exclusive: false,
    autoDelete: false
);
```

### Binding Operations

```csharp
await channel.QueueBindAsync(
    queue: "my_queue",
    exchange: "my_exchange",
    routingKey: "my.routing.key"
);
```

### Publish Operations

```csharp
await channel.BasicPublishAsync(
    exchange: "my_exchange",
    routingKey: "my.routing.key",
    body: messageBytes
);
```

### Consume Operations

```csharp
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (sender, ea) =>
{
    var message = Encoding.UTF8.GetString(ea.Body.ToArray());
    Console.WriteLine($"Recebido: {message}");
};

await channel.BasicConsumeAsync(
    queue: "my_queue",
    autoAck: true,
    consumer: consumer
);
```

---

## Troubleshooting

### Connection refused

**Problema:** RabbitMQ não está rodando

**Solução:**
```bash
docker start rabbitmq
# ou
sudo systemctl start rabbitmq-server
```

### Mensagens não chegam

**Checklist:**
1. RabbitMQ rodando?
2. Receivers executando?
3. Routing keys corretas?
4. Verificar http://localhost:15672

### Caracteres estranhos

**Problema:** Encoding incorreto

**Solução:**
```csharp
Console.OutputEncoding = Encoding.UTF8;
```

---

## Best Practices

### DO (Recomendado)

```csharp
// Use async/await
await channel.BasicPublishAsync(...);

// Configure UTF-8
Console.OutputEncoding = Encoding.UTF8;

// Use using
using var connection = await factory.CreateConnectionAsync();

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

// Não crie conexão por mensagem
// Reutilize conexões
```

---

## Conceitos Importantes

### AutoAck vs Manual

| Modo | Prós | Contras |
|------|------|---------|
| AutoAck: true | Simples | Perde mensagens se falhar |
| AutoAck: false | Confiável | Mais complexo |

### Durabilidade

```csharp
durable: false,  // Fila desaparece ao reiniciar (dev)
durable: true,   // Fila persiste (produção)
```

---

## Docker Commands

```bash
# Iniciar RabbitMQ
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# Ver logs
docker logs -f rabbitmq

# Parar/Iniciar
docker stop rabbitmq
docker start rabbitmq

# Remover
docker rm -f rabbitmq
```

---

## RabbitMQCtl Commands

```bash
# Status
rabbitmqctl status

# Listar filas
rabbitmqctl list_queues

# Listar exchanges
rabbitmqctl list_exchanges

# Purgar fila
rabbitmqctl purge_queue queue_name

# Reset
rabbitmqctl reset
```

---

## Recursos Adicionais

### Documentação
- [RabbitMQ Tutorials](https://www.rabbitmq.com/getstarted.html)
- [.NET Client API](https://www.rabbitmq.com/dotnet-api-guide.html)

### Ferramentas
- [RabbitMQ Management](http://localhost:15672)
- [RabbitMQ Simulator](http://tryrabbitmq.com/)

### Packages
```bash
dotnet add package RabbitMQ.Client
```

---

## Autor

**Breno Augusto**
- GitHub: [@brenoaug](https://github.com/brenoaug)
- Repositório: [rabbitmq-exercise](https://github.com/brenoaug/rabbitmq-exercise)

---

**Última atualização:** 2024 | **.NET:** 8.0 | **RabbitMQ.Client:** 7.x

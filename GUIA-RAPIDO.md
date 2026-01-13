# Guia Rápido - RabbitMQ Topic Exchange


---

## O Básico em 30 Segundos

```
Você tem:
├── 1 API (Publisher)  → Envia mensagens
├── 1 Exchange         → Roteia mensagens
├── 3 Filas            → Armazenam mensagens
└── 3 Receivers        → Processam mensagens
```

**Fluxo Simples:**
```
API → RabbitMQ → Fila → Receiver → Processa
```

---

## Routing Keys Usadas

```
┌────────────────────┬─────────────┬────────────────────┐
│   Routing Key      │    Vai para │   Quem recebe      │
├────────────────────┼─────────────┼────────────────────┤
│ greeting.message   │ queue0      │ Receiver           │
│                    │ queue2      │ Receiver2          │
├────────────────────┼─────────────┼────────────────────┤
│ bye.message        │ queue1      │ Receiver1          │
│                    │ queue2      │ Receiver2          │
├────────────────────┼─────────────┼────────────────────┤
│ qualquer.message   │ queue2      │ Receiver2 (*.msg)  │
└────────────────────┴─────────────┴────────────────────┘
```

---

## Comandos Rápidos

### Iniciar Tudo

```bash
# 1. Iniciar RabbitMQ
docker run -d -p 5672:5672 -p 15672:15672 rabbitmq:management

# 2. Iniciar API
cd RabbitMq.Publisher && dotnet run

# 3. Iniciar Receivers (em terminais separados)
cd RabbitMq.Receiver && dotnet run
cd RabbitMq.Receiver1 && dotnet run
cd RabbitMq.Receiver2 && dotnet run
```

### Testar API

```bash
# Greeting
curl -X POST https://localhost:7173/Message/greeting \
  -H "Content-Type: application/json" \
  -d '{"title":"Teste","text":"Olá","author":"João"}'

# Bye
curl -X POST https://localhost:7173/Message/bye \
  -H "Content-Type: application/json" \
  -d '{"title":"Tchau","text":"Até","author":"Maria"}'
```

---

## Estrutura de Arquivos

```
RabbitMq.Publisher/
├── MessageController.cs     → Endpoints HTTP
└── RabbitMqConfig.cs        → Setup RabbitMQ

RabbitMq.Receiver/
└── Program.cs               → Consome queue0

RabbitMq.Receiver1/
└── Program.cs               → Consome queue1

RabbitMq.Receiver2/
└── Program.cs               → Consome queue2
```

---

## Código Essencial

### Publisher (simplificado)

```csharp
// 1. Criar canal
var channel = await RabbitMqConfiguration.CreateAndConfigureChannelAsync();

// 2. Converter mensagem
var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

// 3. Publicar
await RabbitMqConfiguration.PublishMessageAsync(channel, "greeting.message", body);
```

### Receiver (simplificado)

```csharp
// 1. Conectar
var factory = new ConnectionFactory { HostName = "localhost" };
var connection = await factory.CreateConnectionAsync();
var channel = await connection.CreateChannelAsync();

// 2. Declarar fila e bind
await channel.QueueDeclareAsync(queue: "queue0", ...);
await channel.QueueBindAsync(queue: "queue0", routingKey: "greeting.message");

// 3. Consumir
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    var message = Encoding.UTF8.GetString(ea.Body.ToArray());
    Console.WriteLine($"Recebido: {message}");
};
await channel.BasicConsumeAsync("queue0", autoAck: true, consumer);

// 4. Aguardar
await Task.Delay(Timeout.Infinite);
```

---

## Troubleshooting Rápido

| Problema | Solução |
|----------|---------|
| Connection refused | `sudo systemctl start rabbitmq-server` |
| Não recebe mensagens | Verificar routing keys e se receivers estão rodando |
| Caracteres estranhos | Adicionar `Console.OutputEncoding = Encoding.UTF8;` |
| Swagger 500 | Verificar se rotas tem `[HttpPost("nome")]` |

---

## Diagrama Visual Simplificado

```
POST /greeting
     ↓
┌────────────┐
│  Publisher │
└─────┬──────┘
      │ greeting.message
      ↓
┌─────────────────┐
│ topic_exchange  │
└────┬────────┬───┘
     │        │
     ↓        ↓
  queue0   queue2
     │        │
     ↓        ↓
 Receiver  Receiver2
```

---

## Matriz de Roteamento

```
                   queue0   queue1   queue2
                   ------   ------   ------
greeting.message     OK       NO       OK
bye.message          NO       OK       OK
outro.message        NO       NO       OK
test.message         NO       NO       OK
```

---

## Wildcards

```
*  = uma palavra
#  = zero ou mais palavras

Exemplos:
*.message         → qualquer_coisa.message
#.message         → a.b.c.message ou message
greeting.*        → greeting.qualquer_coisa
order.#           → order ou order.paid ou order.paid.email
```

---

## Dicas Importantes

1. **AutoAck: true** = Simples mas perde mensagens se falhar
2. **Durable: false** = Fila desaparece ao reiniciar RabbitMQ
3. **UTF-8** = Sempre configurar para acentos funcionarem
4. **Task.Delay(Timeout.Infinite)** = Mantém receiver rodando
5. **using var** = Fecha conexões automaticamente

---

## Links Úteis

- **RabbitMQ Management**: http://localhost:15672 (guest/guest)
- **Swagger API**: https://localhost:7173/swagger
- **Documentação**: https://www.rabbitmq.com/getstarted.html

---

## Precisa de Ajuda?

1. Verifique se RabbitMQ está rodando: `sudo systemctl status rabbitmq-server`
2. Acesse Management UI: http://localhost:15672
3. Verifique logs dos Receivers no console
4. Teste no Swagger: https://localhost:7173/swagger

---

## Principais Conceitos

| Termo | Significado |
|-------|-------------|
| **Publisher** | Quem envia mensagens (API) |
| **Consumer** | Quem recebe mensagens (Receivers) |
| **Exchange** | Roteador de mensagens |
| **Queue** | Fila que armazena mensagens |
| **Routing Key** | Chave usada para rotear |
| **Binding** | Ligação entre exchange e queue |

---

**Salve este arquivo para consulta rápida!**


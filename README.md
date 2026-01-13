# RabbitMQ Exercise - Sistema de Mensageria

Um projeto de estudos sobre RabbitMQ com .NET 8 para aprender como funciona a troca de mensagens entre aplicações.

## O que é esse projeto?

Este projeto demonstra como usar o **RabbitMQ** (um sistema de filas de mensagens) para fazer diferentes aplicações conversarem entre si de forma assíncrona. Pense nisso como um "correio" que recebe cartas de um lugar e entrega em outro.

### Conceitos Básicos (Glossário Simplificado)

Antes de começar, vamos entender alguns termos importantes:

- **RabbitMQ**: É como um "correio digital" que guarda e entrega mensagens entre aplicações
- **Producer (Produtor)**: Quem envia as mensagens (no nosso caso, a API Publisher)
- **Consumer (Consumidor)**: Quem recebe e processa as mensagens (os Receivers)
- **Queue (Fila)**: Uma "caixa" onde as mensagens ficam guardadas esperando para serem lidas
- **Exchange**: Um "distribuidor" que decide para qual fila cada mensagem deve ir
- **Routing Key**: Uma "etiqueta" que indica para onde a mensagem deve ser enviada
- **Direct Exchange**: Um tipo de distribuidor que entrega mensagens baseado na routing key exata

## Arquitetura do Projeto

O projeto é composto por **3 aplicações**:

```
???????????????????????
?  RabbitMq.Publisher ?  ? API que ENVIA mensagens
?     (Web API)       ?
???????????????????????
           ?
           ?
    ????????????????
    ?   RabbitMQ   ?  ? "Correio" que guarda e distribui
    ?    Server    ?
    ????????????????
           ???????????????????
           ?                 ?
????????????????????  ????????????????????
? RabbitMq.Receiver?  ?RabbitMq.Receiver1?  ? Aplicações que RECEBEM mensagens
?   (Console App)  ?  ?  (Console App)   ?
????????????????????  ????????????????????
```

### Como funciona o fluxo:

1. **RabbitMq.Publisher** (API): Você envia uma mensagem via HTTP
2. **RabbitMQ Exchange**: Recebe a mensagem e decide para onde enviar baseado na routing key
3. **Fila (queue0 ou queue1)**: Armazena a mensagem
4. **Receiver**: Lê a mensagem da fila e processa

## Como funciona na prática

### Sistema de Roteamento (Direct Exchange)

O projeto usa um **Direct Exchange** chamado `direct_exchange` que funciona assim:

| Routing Key | Vai para a Fila | Quem Recebe |
|-------------|-----------------|-------------|
| `route0`    | `queue0`        | RabbitMq.Receiver |
| `route1`    | `queue1`        | RabbitMq.Receiver1 |

**Exemplo prático:**
- Se você enviar uma mensagem com routing key `route0`, apenas o **Receiver** vai recebê-la
- Se enviar com routing key `route1`, apenas o **Receiver1** vai recebê-la

## Tecnologias Utilizadas

- **.NET 8**: Plataforma de desenvolvimento
- **C# 12**: Linguagem de programação
- **RabbitMQ**: Sistema de mensageria
- **RabbitMQ.Client**: Biblioteca para conectar ao RabbitMQ
- **ASP.NET Core Web API**: Para criar a API que envia mensagens

## Pré-requisitos

Antes de rodar o projeto, você precisa ter instalado:

1. **.NET 8 SDK** - [Download aqui](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **RabbitMQ Server** - [Download aqui](https://www.rabbitmq.com/download.html)
   - Ou use Docker: `docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:management`
3. **Visual Studio 2022** ou **VS Code** (opcional, mas recomendado)

## Como executar o projeto

### Passo 1: Iniciar o RabbitMQ

Certifique-se de que o RabbitMQ está rodando:

```bash
# Se instalou localmente, ele já deve estar rodando automaticamente
# Se usar Docker:
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:management
```

Você pode acessar a interface web do RabbitMQ em: `http://localhost:15672`
- Usuário padrão: `guest`
- Senha padrão: `guest`

### Passo 2: Executar os Receivers (Consumidores)

Abra **dois terminais** separados:

**Terminal 1 - Receiver**
```bash
cd RabbitMq.Receiver
dotnet run
```

**Terminal 2 - Receiver1**
```bash
cd RabbitMq.Receiver1
dotnet run
```

Você deve ver mensagens como:
```
Fila 'queue0' vinculada ao exchange 'direct_exchange' com routing key 'route0'
Aguardando mensagens. Pressione Ctrl+C para sair.
```

### Passo 3: Executar o Publisher (Produtor)

Abra um **terceiro terminal**:

```bash
cd RabbitMq.Publisher
dotnet run
```

A API vai iniciar, geralmente em `https://localhost:7XXX` ou `http://localhost:5XXX`

### Passo 4: Enviar mensagens

Você pode enviar mensagens de duas formas:

#### Opção 1: Usando o Swagger (mais fácil)

1. Abra o navegador em `https://localhost:PORTA/swagger` (veja a porta no terminal)
2. Clique em `POST /api/Message`
3. Clique em "Try it out"
4. Cole este JSON no body:

```json
{
  "title": "Minha primeira mensagem",
  "text": "Olá RabbitMQ!",
  "author": "Seu Nome"
}
```

5. Clique em "Execute"

#### Opção 2: Usando curl ou Postman

```bash
curl -X POST "https://localhost:PORTA/api/Message" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Teste",
    "text": "Olá mundo!",
    "author": "Breno"
  }'
```

### Passo 5: Verificar o resultado

Volte para os terminais dos Receivers. Após ~5 segundos (há um delay simulado), você verá:

```
Mensagem Recebida: {"Title":"Minha primeira mensagem","Text":"Olá RabbitMQ!","Author":"Seu Nome"}
```

## Estrutura de uma Mensagem

As mensagens enviadas têm esse formato:

```csharp
{
    "title": "Título da mensagem",    // Assunto
    "text": "Conteúdo da mensagem",   // Corpo do texto
    "author": "Nome do autor"         // Quem enviou
}
```

## Entendendo o Código

### RabbitMq.Publisher (API)

**MessageController.cs**: Recebe requisições HTTP e publica mensagens no RabbitMQ

```csharp
// 1. Conecta ao RabbitMQ
var factory = new ConnectionFactory() { HostName = "localhost" };

// 2. Declara o exchange (distribuidor)
await channel.ExchangeDeclareAsync(exchange: "direct_exchange", type: ExchangeType.Direct);

// 3. Declara a fila
await channel.QueueDeclareAsync(queue: "queue0", ...);

// 4. Liga a fila ao exchange com uma routing key
await channel.QueueBindAsync(queue: "queue0", exchange: "direct_exchange", routingKey: "route0");

// 5. Publica a mensagem
await channel.BasicPublishAsync(exchange: "direct_exchange", routingKey: "route0", body: mensagem);
```

### RabbitMq.Receiver e RabbitMq.Receiver1 (Consumidores)

**Program.cs**: Conecta ao RabbitMQ e fica escutando por mensagens

```csharp
// 1. Cria um consumidor
var consumer = new AsyncEventingBasicConsumer(channel);

// 2. Define o que fazer quando receber mensagem
consumer.ReceivedAsync += async (model, ea) =>
{
    var message = Encoding.UTF8.GetString(ea.Body.ToArray());
    await Task.Delay(5000); // Simula processamento
    Console.WriteLine($"Mensagem Recebida: {message}");
};

// 3. Começa a consumir da fila
await channel.BasicConsumeAsync("queue0", autoAck: true, consumer: consumer);
```

## Conceitos Importantes Explicados

### Por que usar RabbitMQ?

Imagine que você tem duas aplicações:
- **App A**: Processa pedidos de uma loja
- **App B**: Envia e-mails de confirmação

Sem RabbitMQ:
- App A teria que esperar o e-mail ser enviado para continuar
- Se App B estiver offline, o pedido pode falhar

Com RabbitMQ:
- App A envia a mensagem para a fila e já continua trabalhando
- App B processa quando estiver disponível
- Se App B cair, as mensagens ficam guardadas na fila

### Exchange Direct vs Outros tipos

- **Direct**: Entrega baseado na routing key exata (o que usamos aqui)
- **Fanout**: Envia para todas as filas conectadas (broadcast)
- **Topic**: Usa padrões na routing key (ex: `log.error.*`)
- **Headers**: Usa cabeçalhos da mensagem para decidir o destino

### AutoAck: true ou false?

No código usamos `autoAck: true`, o que significa:
- **true**: RabbitMQ remove a mensagem da fila assim que ela é entregue
- **false**: Você precisa confirmar manualmente que processou (mais seguro, mas mais complexo)

## Experimentos Sugeridos

Para entender melhor, tente:

1. **Mudar a routing key**: No Publisher, troque `route0` por `route1` e veja qual Receiver recebe
2. **Desligar um Receiver**: Veja que as mensagens continuam funcionando para o outro
3. **Remover o delay**: Comente o `Task.Delay(5000)` para ver mensagens instantâneas
4. **Ver mensagens acumuladas**: Desligue os Receivers, envie várias mensagens, depois ligue de novo

## Problemas Comuns

### "Não consigo conectar ao RabbitMQ"
- Verifique se o RabbitMQ está rodando: `netstat -an | findstr 5672`
- Confirme que o HostName é "localhost"

### "Mensagens não aparecem"
- Verifique se a routing key no Publisher e no Receiver são iguais
- Confirme que os Receivers estão rodando ANTES de enviar mensagens

### "Erro de porta na API"
- Verifique em `Properties/launchSettings.json` qual porta está configurada

## Recursos para Aprender Mais

- [Documentação Oficial RabbitMQ](https://www.rabbitmq.com/documentation.html)
- [Tutoriais RabbitMQ em .NET](https://www.rabbitmq.com/tutorials/tutorial-one-dotnet.html)
- [RabbitMQ.Client no NuGet](https://www.nuget.org/packages/RabbitMQ.Client)

## Licença

Projeto de estudos - use e modifique à vontade!

## Autor

Breno - Exercício de aprendizado sobre RabbitMQ e Mensageria

---

**Dica Final**: RabbitMQ é muito usado em sistemas reais para:
- Processamento assíncrono (enviar e-mails, gerar relatórios)
- Comunicação entre microserviços
- Fila de tarefas (jobs)
- Sistemas distribuídos

Continue praticando e boa sorte nos estudos!

# Diagramas Visuais - RabbitMQ Topic Exchange

## Arquitetura Completa do Sistema

```
╔═══════════════════════════════════════════════════════════════════════════╗
║                         SISTEMA RABBITMQ - VISÃO GERAL                     ║
╚═══════════════════════════════════════════════════════════════════════════╝

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│                          CAMADA DE APLICAÇÃO                                │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                      RabbitMq.Publisher (API)                       │   │
│  │                                                                     │   │
│  │  ┌──────────────┐     ┌──────────────┐     ┌──────────────┐      │   │
│  │  │   GET /      │     │ POST         │     │ POST         │      │   │
│  │  │   Message    │     │ /greeting    │     │ /bye         │      │   │
│  │  │              │     │              │     │              │      │   │
│  │  │ Lista todas  │     │ Envia hello  │     │ Envia bye    │      │   │
│  │  └──────────────┘     └──────┬───────┘     └──────┬───────┘      │   │
│  │                               │                    │              │   │
│  │                               │  Routing Key:      │  Routing Key:│   │
│  │                               │ "greeting.message" │ "bye.message"│   │
│  └───────────────────────────────┼────────────────────┼──────────────┘   │
│                                  │                    │                   │
└──────────────────────────────────┼────────────────────┼───────────────────┘
                                   │                    │
                                   ▼                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│                       CAMADA DE MENSAGERIA (RabbitMQ)                       │
│                                                                             │
│  ╔═══════════════════════════════════════════════════════════════════════╗ │
│  ║                    Exchange: "topic_exchange"                         ║ │
│  ║                    Type: TOPIC                                        ║ │
│  ╚═════════════════════════════════╦═════════════════════════════════════╝ │
│                                    ║                                       │
│         ┌──────────────────────────┼──────────────────────────────┐       │
│         │                          │                              │       │
│         ▼                          ▼                              ▼       │
│  ┏━━━━━━━━━━━━┓            ┏━━━━━━━━━━━━┓              ┏━━━━━━━━━━━━┓    │
│  ┃   queue0   ┃            ┃   queue1   ┃              ┃   queue2   ┃    │
│  ┃            ┃            ┃            ┃              ┃            ┃    │
│  ┃  Binding:  ┃            ┃  Binding:  ┃              ┃  Binding:  ┃    │
│  ┃  greeting. ┃            ┃    bye.    ┃              ┃    *.      ┃    │
│  ┃  message   ┃            ┃   message  ┃              ┃  message   ┃    │
│  ┃            ┃            ┃            ┃              ┃  (wildcard)┃    │
│  ┃  Msgs: 🟢  ┃            ┃  Msgs: 🟡  ┃              ┃  Msgs: 🔵  ┃    │
│  ┗━━━━━┯━━━━━━┛            ┗━━━━━┯━━━━━━┛              ┗━━━━━┯━━━━━━┛    │
│        │                          │                            │          │
└────────┼──────────────────────────┼────────────────────────────┼──────────┘
         │                          │                            │
         ▼                          ▼                            ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│                       CAMADA DE CONSUMIDORES                                │
│                                                                             │
│  ┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐   │
│  │   Receiver      │      │   Receiver1     │      │   Receiver2     │   │
│  │   (Console)     │      │   (Console)     │      │   (Console)     │   │
│  │                 │      │                 │      │                 │   │
│  │  Consome:       │      │  Consome:       │      │  Consome:       │   │
│  │  queue0         │      │  queue1         │      │  queue2         │   │
│  │                 │      │                 │      │                 │   │
│  │  Processa:      │      │  Processa:      │      │  Processa:      │   │
│  │  greeting.msg   │      │  bye.msg        │      │  TODAS msgs     │   │
│  │                 │      │                 │      │                 │   │
│  │  [5s delay]     │      │  [5s delay]     │      │  [5s delay]     │   │
│  │  └→ Console.log │      │  └→ Console.log │      │  └→ Console.log │   │
│  └─────────────────┘      └─────────────────┘      └─────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Fluxo de Mensagem: Greeting

```
╔══════════════════════════════════════════════════════════════════════╗
║  Cenário: POST /Message/greeting                                     ║
║  Payload: {"title":"Olá","text":"Mundo","author":"João"}           ║
╚══════════════════════════════════════════════════════════════════════╝

ETAPA 1: PUBLICAÇÃO
━━━━━━━━━━━━━━━━━━━
┌────────────────┐
│   Publisher    │
│   (API)        │
└───────┬────────┘
        │
        │ 1. Serializa para JSON
        │    {"title":"Olá",...}
        │
        │ 2. Converte para bytes UTF-8
        │    [123, 34, 116, 105, ...]
        │
        │ 3. Envia com routing key
        │    "greeting.message"
        ▼
┌─────────────────────────────┐
│  RabbitMQ Server            │
│  ┌───────────────────────┐  │
│  │  topic_exchange       │  │
│  │  (Analisa routing key)│  │
│  └───────────────────────┘  │
└─────────────────────────────┘


ETAPA 2: ROTEAMENTO
━━━━━━━━━━━━━━━━━━━━
Routing Key: "greeting.message"

┌────────────────────────────────────────────────────┐
│  Exchange analisa bindings:                        │
│                                                    │
│  ✅ queue0: "greeting.message"  → MATCH exato     │
│  ❌ queue1: "bye.message"       → NO MATCH        │
│  ✅ queue2: "*.message"          → MATCH wildcard │
└────────────────────────────────────────────────────┘

        ┌─────────────────┐
        │  topic_exchange │
        └────────┬────────┘
                 │
        ┌────────┴─────────┐
        │                  │
        ▼                  ▼
   ┏━━━━━━━┓          ┏━━━━━━━┓
   ┃ queue0┃          ┃ queue2┃
   ┗━━━┯━━━┛          ┗━━━┯━━━┛
       │                  │
       │ Cópia da msg     │ Cópia da msg
       │                  │


ETAPA 3: CONSUMO
━━━━━━━━━━━━━━━━
   ┏━━━━━━━┓          ┏━━━━━━━┓
   ┃ queue0┃          ┃ queue2┃
   ┗━━━┯━━━┛          ┗━━━┯━━━┛
       │                  │
       ▼                  ▼
┌─────────────┐    ┌─────────────┐
│  Receiver   │    │  Receiver2  │
│             │    │             │
│ [Aguarda 5s]│    │ [Aguarda 5s]│
│     ↓       │    │     ↓       │
│  Console:   │    │  Console:   │
│  "Olá..."   │    │  "Olá..."   │
└─────────────┘    └─────────────┘


RESULTADO FINAL
━━━━━━━━━━━━━━━
Terminal Receiver (queue0):
  ╔════════════════════════════════════════╗
  ║ Mensagem Recebida:                     ║
  ║ {"title":"Olá","text":"Mundo",...}    ║
  ╚════════════════════════════════════════╝

Terminal Receiver2 (queue2):
  ╔════════════════════════════════════════╗
  ║ Mensagem Recebida:                     ║
  ║ {"title":"Olá","text":"Mundo",...}    ║
  ╚════════════════════════════════════════╝

Terminal Receiver1 (queue1):
  [silêncio - não recebe nada]
```

---

## Fluxo de Mensagem: Bye

```
╔══════════════════════════════════════════════════════════════════════╗
║  Cenário: POST /Message/bye                                          ║
║  Payload: {"title":"Tchau","text":"Até logo","author":"Maria"}     ║
╚══════════════════════════════════════════════════════════════════════╝

ROTEAMENTO
━━━━━━━━━━
Routing Key: "bye.message"

┌────────────────────────────────────────────────────┐
│  Exchange analisa bindings:                        │
│                                                    │
│  ❌ queue0: "greeting.message"  → NO MATCH        │
│  ✅ queue1: "bye.message"       → MATCH exato     │
│  ✅ queue2: "*.message"          → MATCH wildcard │
└────────────────────────────────────────────────────┘

        ┌─────────────────┐
        │  topic_exchange │
        └────────┬────────┘
                 │
        ┌────────┴─────────┐
        │                  │
        ▼                  ▼
   ┏━━━━━━━┓          ┏━━━━━━━┓
   ┃ queue1┃          ┃ queue2┃
   ┗━━━┯━━━┛          ┗━━━┯━━━┛
       │                  │
       ▼                  ▼
┌─────────────┐    ┌─────────────┐
│  Receiver1  │    │  Receiver2  │
│  "Tchau..." │    │  "Tchau..." │
└─────────────┘    └─────────────┘
```

---

## Tabela de Verdade - Roteamento

```
╔═══════════════════════════════════════════════════════════════════╗
║            MATRIZ DE ROTEAMENTO - TOPIC EXCHANGE                  ║
╚═══════════════════════════════════════════════════════════════════╝

┌─────────────────────┬─────────────────────┬───────────────────────┐
│   Routing Key       │   Pattern           │   Resultado           │
│   (Publicação)      │   (Binding)         │                       │
├─────────────────────┼─────────────────────┼───────────────────────┤
│                     │                     │                       │
│ greeting.message    │ greeting.message    │  ✅ MATCH (exato)     │
│ greeting.message    │ bye.message         │  ❌ NO MATCH          │
│ greeting.message    │ *.message           │  ✅ MATCH (wildcard)  │
│                     │                     │                       │
│ bye.message         │ greeting.message    │  ❌ NO MATCH          │
│ bye.message         │ bye.message         │  ✅ MATCH (exato)     │
│ bye.message         │ *.message           │  ✅ MATCH (wildcard)  │
│                     │                     │                       │
│ hello.message       │ greeting.message    │  ❌ NO MATCH          │
│ hello.message       │ bye.message         │  ❌ NO MATCH          │
│ hello.message       │ *.message           │  ✅ MATCH (wildcard)  │
│                     │                     │                       │
│ test.message        │ *.message           │  ✅ MATCH             │
│ message             │ *.message           │  ❌ NO MATCH (precisa 2 palavras) │
│ hello.world.message │ *.message           │  ❌ NO MATCH (3 palavras) │
│ hello.world.message │ #.message           │  ✅ MATCH (# pega tudo) │
└─────────────────────┴─────────────────────┴───────────────────────┘


LEGENDA DOS SÍMBOLOS
━━━━━━━━━━━━━━━━━━━━
*  →  Substitui EXATAMENTE uma palavra
      Exemplo: *.message = palavra.message

#  →  Substitui ZERO OU MAIS palavras
      Exemplo: #.message = message OU a.message OU a.b.c.message
```

---

## Distribuição de Mensagens

```
╔═══════════════════════════════════════════════════════════════════╗
║          QUEM RECEBE O QUÊ? - MATRIZ DE DISTRIBUIÇÃO             ║
╚═══════════════════════════════════════════════════════════════════╝

                    ┌──────────┬──────────┬──────────┐
                    │  queue0  │  queue1  │  queue2  │
                    │(greeting)│  (bye)   │ (catch)  │
                    │          │          │  (all)   │
┌───────────────────┼──────────┼──────────┼──────────┤
│ greeting.message  │    ✅    │    ❌    │    ✅    │
│ bye.message       │    ❌    │    ✅    │    ✅    │
│ hello.message     │    ❌    │    ❌    │    ✅    │
│ test.message      │    ❌    │    ❌    │    ✅    │
│ outro.message     │    ❌    │    ❌    │    ✅    │
└───────────────────┴──────────┴──────────┴──────────┘

OBSERVAÇÃO:
queue2 com binding "*.message" funciona como CATCH-ALL
para qualquer routing key no formato: palavra.message
```

---

## 🔗 Conexões e Canais

```
╔═══════════════════════════════════════════════════════════════════╗
║               ESTRUTURA DE CONEXÕES - RABBITMQ                    ║
╚═══════════════════════════════════════════════════════════════════╝

PUBLISHER (API)
━━━━━━━━━━━━━━━
┌────────────────────────────────────────────┐
│  RabbitMq.Publisher                        │
│                                            │
│  ┌──────────────────────────────────────┐ │
│  │  ConnectionFactory                   │ │
│  │  HostName: "localhost"               │ │
│  │  Port: 5672 (default)                │ │
│  └────────────┬─────────────────────────┘ │
│               │                            │
│               ▼                            │
│  ┌──────────────────────────────────────┐ │
│  │  IConnection (TCP)                   │ │
│  │  └→ Mantém conexão com RabbitMQ      │ │
│  └────────────┬─────────────────────────┘ │
│               │                            │
│               ▼                            │
│  ┌──────────────────────────────────────┐ │
│  │  IChannel (Virtual)                  │ │
│  │  └→ Onde comandos são executados     │ │
│  │     • ExchangeDeclare                │ │
│  │     • QueueDeclare                   │ │
│  │     • QueueBind                      │ │
│  │     • BasicPublish                   │ │
│  └──────────────────────────────────────┘ │
└────────────────────────────────────────────┘


CONSUMER (Receiver)
━━━━━━━━━━━━━━━━━━━
┌────────────────────────────────────────────┐
│  RabbitMq.Receiver                         │
│                                            │
│  ┌──────────────────────────────────────┐ │
│  │  ConnectionFactory                   │ │
│  │  HostName: "localhost"               │ │
│  └────────────┬─────────────────────────┘ │
│               │                            │
│               ▼                            │
│  ┌──────────────────────────────────────┐ │
│  │  IConnection                         │ │
│  └────────────┬─────────────────────────┘ │
│               │                            │
│               ▼                            │
│  ┌──────────────────────────────────────┐ │
│  │  IChannel                            │ │
│  │  • ExchangeDeclare (garante existe)  │ │
│  │  • QueueDeclare (garante existe)     │ │
│  │  • QueueBind (garante binding)       │ │
│  │  • BasicConsume (inicia consumo)     │ │
│  └────────────┬─────────────────────────┘ │
│               │                            │
│               ▼                            │
│  ┌──────────────────────────────────────┐ │
│  │  AsyncEventingBasicConsumer          │ │
│  │  • ReceivedAsync (callback)          │ │
│  │    └→ Quando mensagem chega          │ │
│  └──────────────────────────────────────┘ │
└────────────────────────────────────────────┘
```

---

## Estados e Ciclo de Vida

```
╔═══════════════════════════════════════════════════════════════════╗
║                CICLO DE VIDA DAS MENSAGENS                        ║
╚═══════════════════════════════════════════════════════════════════╝

ETAPA 1: CRIAÇÃO
━━━━━━━━━━━━━━━━
┌─────────────────────────────────────────┐
│  API recebe POST request                │
│  └→ Cria objeto Message                 │
│     └→ Serializa para JSON              │
│        └→ Converte para byte[]          │
│           └→ Estado: READY TO PUBLISH   │
└─────────────────────────────────────────┘
                   │
                   ▼
ETAPA 2: PUBLICAÇÃO
━━━━━━━━━━━━━━━━━━━
┌─────────────────────────────────────────┐
│  BasicPublishAsync()                    │
│  └→ Envia para RabbitMQ                 │
│     └→ Exchange recebe                  │
│        └→ Estado: IN EXCHANGE           │
└─────────────────────────────────────────┘
                   │
                   ▼
ETAPA 3: ROTEAMENTO
━━━━━━━━━━━━━━━━━━━
┌─────────────────────────────────────────┐
│  Exchange analisa routing key           │
│  └→ Compara com bindings                │
│     └→ Identifica filas destino         │
│        └→ Cria cópias da mensagem       │
│           └→ Estado: ROUTING            │
└─────────────────────────────────────────┘
                   │
                   ▼
ETAPA 4: ENFILEIRAMENTO
━━━━━━━━━━━━━━━━━━━━━
┌─────────────────────────────────────────┐
│  Mensagem(ns) copiada(s) para fila(s)   │
│  └→ Armazenada em memória/disco         │
│     └→ Aguarda consumer                 │
│        └→ Estado: QUEUED                │
└─────────────────────────────────────────┘
                   │
                   ▼
ETAPA 5: CONSUMO
━━━━━━━━━━━━━━━━
┌─────────────────────────────────────────┐
│  Consumer recebe mensagem                │
│  └→ Callback ReceivedAsync dispara      │
│     └→ Processa (delay 5s no exemplo)   │
│        └→ Estado: PROCESSING            │
└─────────────────────────────────────────┘
                   │
                   ▼
ETAPA 6: CONFIRMAÇÃO (ACK)
━━━━━━━━━━━━━━━━━━━━━━━━━━
┌─────────────────────────────────────────┐
│  AutoAck = true (nosso caso)            │
│  └→ RabbitMQ remove da fila             │
│     imediatamente                       │ │
│     └→ Estado: ACKNOWLEDGED             │
│        └→ Mensagem destruída            │
└─────────────────────────────────────────┘
                   │
                   ▼
               [FIM]


TIMELINE VISUAL
━━━━━━━━━━━━━━━
0ms   ──┬── POST Request chega
        │
10ms  ──┼── Serializa JSON
        │
15ms  ──┼── Publica no Exchange
        │
20ms  ──┼── Roteamento completo
        │
25ms  ──┼── Mensagem em fila(s)
        │
30ms  ──┼── Consumer recebe
        │
        │   [Aguarda 5000ms]
        │
5030ms──┼── Processamento completo
        │
5035ms──┴── ACK enviado, mensagem removida
```

---

## Segurança e Confiabilidade

```
╔═══════════════════════════════════════════════════════════════════╗
║            GARANTIAS E POSSÍVEIS FALHAS                           ║
╚═══════════════════════════════════════════════════════════════════╝

CONFIGURAÇÃO ATUAL (Desenvolvimento)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
┌────────────────────┬──────────┬─────────────────────────┐
│   Propriedade      │  Valor   │  Impacto                │
├────────────────────┼──────────┼─────────────────────────┤
│ Queue Durable      │  false   │ ⚠️  Perde ao reiniciar  │
│ Message Persistent │  false   │ ⚠️  Perde ao reiniciar  │
│ AutoAck            │  true    │ ⚠️  Perde se processar  │
│                    │          │     falhar              │
│ Mandatory          │  false   │ ⚠️  Ignora se sem fila  │
└────────────────────┴──────────┴─────────────────────────┘


CENÁRIOS DE FALHA
━━━━━━━━━━━━━━━━━
┌──────────────────────────────────────────────────────────┐
│  Falha: RabbitMQ reinicia                                │
│  ├─ Queues com durable=false → PERDIDAS ❌               │
│  ├─ Mensagens em memória → PERDIDAS ❌                   │
│  └─ Bindings → Precisam ser recriados                    │
└──────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│  Falha: Consumer falha durante processamento            │
│  ├─ AutoAck=true → Mensagem JÁ FOI REMOVIDA ❌          │
│  ├─ Processamento perdido                                │
│  └─ Sem retry automático                                 │
└──────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│  Falha: Nenhum consumer conectado                       │
│  ├─ Mensagens acumulam na fila ✅                        │
│  ├─ Quando consumer conectar, processa ✅                │
│  └─ DESDE QUE fila não seja autoDelete                   │
└──────────────────────────────────────────────────────────┘


RECOMENDAÇÕES PARA PRODUÇÃO
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
┌────────────────────┬──────────┬─────────────────────────┐
│   Propriedade      │  Valor   │  Benefício              │
├────────────────────┼──────────┼─────────────────────────┤
│ Queue Durable      │  true    │ ✅ Sobrevive reinicio   │
│ Message Persistent │  true    │ ✅ Salva em disco       │
│ AutoAck            │  false   │ ✅ Retry em falhas      │
│ Mandatory          │  true    │ ✅ Detecta erro rota    │
│ PrefetchCount      │  1-10    │ ✅ Controle de carga    │
└────────────────────┴──────────┴─────────────────────────┘
```

---




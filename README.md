# FCG Payments API — FIAP Cloud Games Fase 2

Worker Service .NET 9 responsável pelo processamento de pagamentos da plataforma FCG.  
Consome eventos `OrderPlacedEvent` do RabbitMQ, simula aprovação/rejeição e publica `PaymentProcessedEvent`.

---

## Visão Geral

O **PaymentsAPI** é um microsserviço **stateless** — sem banco de dados — que atua como processador de pagamentos simulado:

```
[OrdersAPI] → OrderPlacedEvent → [RabbitMQ: fcg.order.placed] → [PaymentsAPI Worker]
                                                                         ↓
                                                              Simulação (500ms delay)
                                                              80% Approved / 20% Rejected
                                                                         ↓
                                                     PaymentProcessedEvent → [RabbitMQ]
                                                                         ↓
                                                              [NotificationsAPI / etc.]
```

---

## Estrutura

```
PaymentsAPI/
├── src/
│   ├── FCG.Payments.Worker/          # Worker Service — entry point, DI, MassTransit
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── appsettings.Development.json
│   └── FCG.Payments.Application/     # Lógica de negócio, eventos, consumer
│       ├── Consumers/
│       │   └── OrderPlacedConsumer.cs
│       ├── Events/
│       │   ├── OrderPlacedEvent.cs
│       │   └── PaymentProcessedEvent.cs
│       ├── Interfaces/
│       │   └── IPaymentSimulatorService.cs
│       └── Services/
│           └── PaymentSimulatorService.cs
├── tests/
│   └── FCG.Payments.Application.Tests/
│       ├── PaymentSimulatorServiceTest.cs
│       └── OrderPlacedConsumerTest.cs
├── k8s/
│   ├── configmap.yaml
│   ├── deployment.yaml
│   └── secret.yaml
├── Dockerfile
├── docker-compose.yml
└── PaymentsAPI.sln
```

---

## Como Funciona a Simulação

O `PaymentSimulatorService` simula um gateway de pagamento externo:

1. Aguarda **500ms** (simula latência de rede/processador)
2. Gera número aleatório entre 1–100
3. Se `número <= TaxaAprovacaoPercent` → **Approved**; caso contrário → **Rejected**
4. Taxa padrão: **80% Approved / 20% Rejected**

### Retry Policy (MassTransit)
Configurado com `3 tentativas` com intervalo de `5 segundos` entre cada uma.  
Em caso de erro irrecuperável, a mensagem é descartada com log de erro (sem relançar exception).

---

## Variáveis de Ambiente

| Variável | Descrição | Padrão |
|----------|-----------|--------|
| `RabbitMQ__Host` | Host do RabbitMQ | `rabbitmq` |
| `RabbitMQ__Username` | Usuário do RabbitMQ | `guest` |
| `RabbitMQ__Password` | Senha do RabbitMQ | `guest` |
| `Payment__TaxaAprovacaoPercent` | % de aprovação (0–100) | `80` |
| `DOTNET_ENVIRONMENT` | Ambiente .NET | `Production` |

---

## Como Rodar Localmente

### Pré-requisitos
- .NET 9 SDK
- Docker + Docker Compose

### Com Docker Compose (recomendado)

```bash
# Na raiz do projeto
docker compose up --build
```

O RabbitMQ Management estará disponível em: http://localhost:15672 (guest/guest)

### Apenas o Worker (com RabbitMQ externo)

```bash
cd src/FCG.Payments.Worker
dotnet run
```

### Executar Testes

```bash
# Na raiz do projeto
dotnet test

# Com relatório detalhado
dotnet test --logger "console;verbosity=detailed"
```

---

## Deploy no Kubernetes

### Pré-requisitos
- Cluster Kubernetes configurado (`kubectl`)
- RabbitMQ já deployado no cluster
- Namespace `fcg` criado:

```bash
kubectl create namespace fcg --dry-run=client -o yaml | kubectl apply -f -
```

### Build e Push da Imagem

```bash
docker build -t payments-api:latest .
# Para registry externo:
docker tag payments-api:latest <seu-registry>/payments-api:latest
docker push <seu-registry>/payments-api:latest
```

### Aplicar Manifests

O serviço é um worker (consome filas, não expõe HTTP), portanto não há manifesto de Service.

```bash
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secret.yaml
kubectl apply -f k8s/deployment.yaml
```

### Verificar Status

```bash
kubectl get pods -n fcg -l app=payments-api
kubectl logs -n fcg -l app=payments-api --follow
```

---

## Eventos de Mensageria

### OrderPlacedEvent (consumido)
```
Queue: fcg.order.placed
```
| Campo | Tipo | Descrição |
|-------|------|-----------|
| `OrderId` | `Guid` | Identificador do pedido |
| `UserId` | `int` | Identificador do usuário |
| `GameId` | `int` | Identificador do jogo |
| `GameName` | `string` | Nome do jogo |
| `Price` | `decimal` | Valor do pedido |
| `UserEmail` | `string` | E-mail do usuário |

### PaymentProcessedEvent (publicado)
| Campo | Tipo | Descrição |
|-------|------|-----------|
| `OrderId` | `Guid` | Identificador do pedido |
| `UserId` | `int` | Identificador do usuário |
| `GameId` | `int` | Identificador do jogo |
| `GameName` | `string` | Nome do jogo |
| `UserEmail` | `string` | E-mail do usuário |
| `Status` | `string` | `"Approved"` ou `"Rejected"` |
| `ProcessedAt` | `DateTime` | Data/hora UTC do processamento |

---

## Tecnologias

- **.NET 9** — Worker Service
- **MassTransit 8** — Abstração de mensageria com retry policy
- **RabbitMQ** — Message broker
- **NUnit 4** — Framework de testes
- **Moq** — Mocking em testes unitários

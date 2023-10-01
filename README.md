# Chat Realtime - SignalR + Apache Kafka

Este projeto tem como objetivo aplicar os meus conhecimentos na plataforma Apache Kafka. Para isso, decidi criar um chat em tempo real que produz mensagens por meio de um cliente frontend, enquanto o servidor as consome. Além disso, o servidor é responsável por enviar essas mensagens para seus respectivos ouvintes, utilizando uma comunicação WebSocket, mais especificamente, a biblioteca SignalR.

### Estruturando o Kafka
O Apache Kafka é uma plataforma de streaming de dados de código aberto que oferece várias vantagens para lidar com fluxos de dados em tempo real, entre elas a escalabilidade, durabilidade, tolerância a falhas e baixa latência. E afim de aproveitarmos essas vantagens iremos estruturar nossso projeto da seguinte forma:

Teremos 3 tópicos, que refletem as salas de nosso chat:
- **room1**: sala 1.
- **room2**: Sala 2.
- **admin**: as mensagens enviadas para essa sala serão também enviadas para a sala 1 e sala 2.

Teremos 3 brokers, cada tópico com 3 partições e com fator de replicação igual a 3. Isso nos garante uma maior segurança no quesito de disponibilidade, além de que futuramente, se necessário, podemos escalar nossos consumers.

### Produzindo mensagens
Como dito anteriormente, estabeleceremos uma comunicação WebSocket entre nosso cliente e servidor, e para isso usaremos a biblioteca SignalR. Em nosso servidor criamos um hub personalizado chamado **ChatHub**, nele é possível inscrever um cliente em até 3 grupos diferentes: room1, room2 e admin. Ao entrarmos em nossa aplicação frontend nos inscrevemos nesses 3 grupos.
O fluxo de comunicação se dá da seguinte forma:
1. Cliente envia uma mensagem passando o nome do grupo (room1, room2, admin).
2. Servidor recebe a mensagem em nosso ChatHub.
3. Hub produz uma mensagem no respectivo tópico Kafka (o nome do grupo é igual ao nome do tópico).  

\
```
public class ChatHub : Hub
{
    private readonly IProducer<string, string> _kafkaProducer;

    public ChatHub()
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = "localhost:9093,localhost:9094,localhost:9095"
        };

        _kafkaProducer = new ProducerBuilder<string, string>(producerConfig).Build();
    }
    
    public void SendMessage(string room, string message)
    {
        var messageValue = JsonSerializer.Serialize(new MessageModel(Context.ConnectionId, message));
        _kafkaProducer.Produce(room, new Message<string, string> { Key = Guid.NewGuid().ToString(), Value = messageValue });
    }

    public async Task JoinRoom(string room)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, room);
    }

    public async Task LeaveRoom(string room)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, room);
    }
}
```

### Consumindo mensagens
Criamos em nosso servidor o **KafkaConsumerService**, um serviço que roda em segundo plano de forma assíncrona. Esse serviço tem como responsibilidade consumir as mensagens do Kafka e transmitir para seus respectivos grupos dentro do SignalR. O fluxo se dá da seguinte forma:
1. KafkaConsumerService recebe uma mensagem da fila do Kafka.
2. Identifica a qual grupo (sala) a mensagem pertence e a distribui para os clientes frontend por meio do SignalR.

```
public class KafkaConsumerService : BackgroundService 
{
    private readonly IHubContext<ChatHub> _chatHubContext;

    public KafkaConsumerService(IHubContext<ChatHub> chatHubContext)
    {
        _chatHubContext = chatHubContext;
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => ConsumeMessages(stoppingToken));
    }

    private void ConsumeMessages(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = "localhost:9093,localhost:9094,localhost:9095",
            GroupId = "consumer-group"
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();

        consumer.Subscribe(new List<string> { "room1", "room2", "admin" });

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                var messageModel =  JsonSerializer.Deserialize<MessageModel>(consumeResult.Message.Value);
                _chatHubContext.Clients
                    .GroupExcept(consumeResult.Topic, messageModel.ConnectionId)
                    .SendAsync("ReceiveMessage", consumeResult.Topic, messageModel.Message);
            }
            catch (OperationCanceledException)
            {
                // Ocorre quando o serviço é parado.
                return;
            }
            catch (Exception ex)
            {
                // Lide com exceções aqui, registre-as ou faça o que for apropriado para sua aplicação.
            }
        }
    }
}
```

### Tecnologias usadas
- AspNet Core
- SignalR
- Apache Kafka
- Angular
- Docker (para criar um cluster kafka)

### Resultado

![Resultado](https://github.com/miguelarquejada/real-time-chat-kafka-signalR/blob/master/result.gif?raw=true)

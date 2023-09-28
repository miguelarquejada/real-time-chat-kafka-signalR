using System.Text.Json;
using backend.Hubs;
using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;

namespace backend;

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
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;

namespace backend.Hubs;

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
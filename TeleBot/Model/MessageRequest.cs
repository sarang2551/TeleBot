using Confluent.Kafka;
using System.Text.Json;

namespace TeleBot.Model;

public record MessageRequest
{
    public required string content { get; set; }
    public required string message_id { get; set; }
    public required long chat_id { get; set; }
    
}
using Confluent.Kafka;
using System.Text.Json;
using TeleBot.Model.Interfaces;

namespace TeleBot.Model;

public class MessageRequest : ITeleMessage
{
    public required string content { get; set; }
    public required int message_id { get; set; }
    public required long chat_id { get; set; }
    
}
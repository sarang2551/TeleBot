using System.Text.Json;
using Confluent.Kafka;

namespace TeleBot.Model;

public class MessageRequestDeserializer : IDeserializer<MessageRequest>
{
    public MessageRequest Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull || data.Length == 0) return null!;
        return JsonSerializer.Deserialize<MessageRequest>(data)!;
    }
}
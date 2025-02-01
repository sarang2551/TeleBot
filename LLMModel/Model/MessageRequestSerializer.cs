using System.Text;
using System.Text.Json;
using Confluent.Kafka;

namespace LLMModel.Model;

public class MessageRequestSerializer : ISerializer<MessageRequest>
{
    public byte[] Serialize(MessageRequest data, SerializationContext context)
    {
        var json = JsonSerializer.Serialize(data);
        return Encoding.UTF8.GetBytes(json);
    }
}
using Confluent.Kafka;
using System.Text.Json;

namespace LLMModel.Model;

public class BaseKafkaEntity<T> : ISerializer<T>, IDeserializer<T>
{
    public byte[] Serialize(T data, SerializationContext context)
    {
        if (data == null) return null!;
        return JsonSerializer.SerializeToUtf8Bytes(data);
    }

    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull || data.Length == 0) return default!;
        return JsonSerializer.Deserialize<T>(data)!;
    }
}

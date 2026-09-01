using System.Text;
using System.Text.Json;
using Confluent.Kafka;

namespace Common.Infrastructure.Kafka.Serialization;

public class JsonSerializer<T> : ISerializer<T>
{
    public byte[] Serialize(T data, SerializationContext context)
    {
        var json = JsonSerializer.Serialize(data);
        return Encoding.ASCII.GetBytes(json);
    }
}

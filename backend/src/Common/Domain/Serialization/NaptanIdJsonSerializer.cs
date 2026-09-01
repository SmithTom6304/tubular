using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Domain.Serialization;

public class NaptanIdJsonSerializer : JsonConverter<NaptanId>
{
    public override NaptanId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return NaptanId.FromString(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, NaptanId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
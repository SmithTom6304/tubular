using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common;

public class TrainIdJsonSerializer : JsonConverter<TrainId>
{
    public override TrainId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return TrainId.FromString(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, TrainId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

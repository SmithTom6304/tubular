using System.Text.Json.Serialization;

namespace Common;

[JsonConverter(typeof(TrainIdJsonSerializer))]
public readonly record struct TrainId
{
    private readonly string _value;

    private TrainId(string value)
    {
        _value = value;
    }

    public static TrainId FromString(string value) => new(value);

    public override string ToString() => _value;
}

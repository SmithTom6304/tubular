using System.Text.Json;
using Shouldly;

namespace Common.Tests;

public class ArrivalPredictionTests
{
    [Fact]
    public void CanBeConvertedRoundTripAsJson()
    {
        var prediction = new ArrivalPrediction(
            TrainId.FromString("218"),
            NaptanId.FromString("940GZZLUHBT"),
            new DateTimeOffset(2026, 8, 26, 12, 30, 0, TimeSpan.Zero));

        var json = JsonSerializer.Serialize(prediction);
        var deserialized = JsonSerializer.Deserialize<ArrivalPrediction>(json);

        deserialized.ShouldBe(prediction);
    }
}

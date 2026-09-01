using Common.Domain;
using Shouldly;

namespace Common.Tests;

public class TrainIdTests
{
    [Theory]
    [InlineData("218")]
    public void CanBeConvertedRoundTrip(string id)
    {
        var trainId = TrainId.FromString(id);
        trainId.ToString().ShouldBe(id);
    }

    [Theory]
    [InlineData("218")]
    public void HasValueEquality(string id)
    {
        var trainId1 = TrainId.FromString(id);
        var trainId2 = TrainId.FromString(id);
        trainId1.ShouldBe(trainId2);
    }
}

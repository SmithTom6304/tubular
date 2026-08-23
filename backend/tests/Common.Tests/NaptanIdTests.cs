using Shouldly;

namespace Common.Tests;

public class NaptanIdTests
{
    [Theory]
    [InlineData("940GZZLUHBT")]
    public void CanBeConvertedRoundTrip(string id)
    {
        var naptanId = NaptanId.FromString(id);
        naptanId.ToString().ShouldBe(id);
    }

    [Theory]
    [InlineData("940GZZLUHBT")]
    public void HasValueEquality(string id)
    {
        var naptanId1 = NaptanId.FromString(id);
        var naptanId2 = NaptanId.FromString(id);
        naptanId1.ShouldBe(naptanId2);
    }
}
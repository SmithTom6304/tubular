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

    [Theory]
    [InlineData("940GZZLUHBT")]
    public void HasValueHashCode(string id)
    {
        var naptanId1 = NaptanId.FromString(id);
        var naptanId2 = NaptanId.FromString(id);
        naptanId1.GetHashCode().ShouldBe(naptanId2.GetHashCode());
    }

    [Theory]
    [InlineData("940GZZLUHBT")]
    public void IsDeduplicatedByHashSet(string id)
    {
        var set = new HashSet<NaptanId> { NaptanId.FromString(id), NaptanId.FromString(id) };
        set.Count.ShouldBe(1);
    }

    [Fact]
    public void DefaultValueDoesNotThrow()
    {
        var naptanId = default(NaptanId);

        Should.NotThrow(() => naptanId.ToString());
        Should.NotThrow(() => naptanId.Equals(default(NaptanId)));
        Should.NotThrow(() => naptanId.GetHashCode());
    }
}
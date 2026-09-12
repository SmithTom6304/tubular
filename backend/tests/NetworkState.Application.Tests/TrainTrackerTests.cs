using Common.Domain;
using NetworkState.Domain;
using Shouldly;

namespace NetworkState.Application.Tests;

public class TrainTrackerTests
{
    private static readonly TrainId Train = TrainId.FromString("T1");
    private const string Line = "victoria";
    private static readonly NaptanId StopA = NaptanId.FromString("940GZZLUOXC");
    private static readonly NaptanId StopB = NaptanId.FromString("940GZZLUVIC");
    private static readonly NaptanId StopC = NaptanId.FromString("940GZZLUPCO");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static TrainState FreshState() => new()
    {
        Id = Train,
        Line = Line,
        LastUpdated = Now,
    };

    private readonly ITrainTracker _tracker = new TrainTracker();

    [Fact]
    public void Apply_WithNoPriorNextStop_SetsNextStopAndDoesNotEmitTraversal()
    {
        var current = FreshState();
        var expectedArrival = Now.AddMinutes(2);

        var (state, traversal) = _tracker.Apply(current, StopB, expectedArrival, Now);

        state.PreviousStopPoint.ShouldBeNull();
        state.NextStopPoint.ShouldBe(StopB);
        state.ExpectedArrivalAtNext.ShouldBe(expectedArrival);
        state.LastUpdated.ShouldBe(Now);
        traversal.ShouldBeNull();
    }

    [Fact]
    public void Apply_WithRefinedPredictionForSameStop_UpdatesEtaAndDoesNotEmitTraversal()
    {
        var current = FreshState() with
        {
            NextStopPoint = StopB,
            ExpectedArrivalAtNext = Now.AddMinutes(2),
        };
        var refinedArrival = Now.AddMinutes(1).AddSeconds(30);
        var refinedNow = Now.AddSeconds(30);

        var (state, traversal) = _tracker.Apply(current, StopB, refinedArrival, refinedNow);

        state.PreviousStopPoint.ShouldBeNull();
        state.NextStopPoint.ShouldBe(StopB);
        state.ExpectedArrivalAtNext.ShouldBe(refinedArrival);
        state.LastUpdated.ShouldBe(refinedNow);
        traversal.ShouldBeNull();
    }

    [Fact]
    public void Apply_WithPredictionForDifferentStop_EmitsTraversalForCompletedSegment()
    {
        var previousEta = Now.AddMinutes(1);
        var current = FreshState() with
        {
            PreviousStopPoint = StopA,
            NextStopPoint = StopB,
            ExpectedArrivalAtNext = previousEta,
        };
        var arrivedAt = Now.AddMinutes(1);
        var nextArrival = arrivedAt.AddMinutes(3);

        var (state, traversal) = _tracker.Apply(current, StopC, nextArrival, arrivedAt);

        traversal.ShouldNotBeNull();
        traversal.Line.ShouldBe(Line);
        traversal.From.ShouldBe(StopB);
        traversal.To.ShouldBe(StopC);
        traversal.Duration.ShouldBe(nextArrival - previousEta);

        state.PreviousStopPoint.ShouldBe(StopB);
        state.NextStopPoint.ShouldBe(StopC);
        state.ExpectedArrivalAtNext.ShouldBe(nextArrival);
        state.LastUpdated.ShouldBe(arrivedAt);
    }

    [Fact]
    public void Apply_ChainedTransitions_EachTransitionUsesUpdatedPreviousStop()
    {
        var current = FreshState();

        var firstEta = Now.AddMinutes(1);
        var (afterFirst, firstTraversal) = _tracker.Apply(current, StopA, firstEta, Now);
        firstTraversal.ShouldBeNull();
        afterFirst.PreviousStopPoint.ShouldBeNull();
        afterFirst.NextStopPoint.ShouldBe(StopA);

        var secondArrival = Now.AddMinutes(1);
        var secondEta = secondArrival.AddMinutes(2);
        var (afterSecond, secondTraversal) = _tracker.Apply(afterFirst, StopB, secondEta, secondArrival);

        secondTraversal.ShouldNotBeNull();
        secondTraversal.From.ShouldBe(StopA);
        secondTraversal.To.ShouldBe(StopB);
        secondTraversal.Duration.ShouldBe(secondEta - firstEta);

        afterSecond.PreviousStopPoint.ShouldBe(StopA);
        afterSecond.NextStopPoint.ShouldBe(StopB);

        var thirdArrival = secondArrival.AddMinutes(2);
        var thirdEta = thirdArrival.AddMinutes(2);
        var (afterThird, thirdTraversal) = _tracker.Apply(afterSecond, StopC, thirdEta, thirdArrival);

        thirdTraversal.ShouldNotBeNull();
        thirdTraversal.From.ShouldBe(StopB);
        thirdTraversal.To.ShouldBe(StopC);
        thirdTraversal.Duration.ShouldBe(thirdEta - secondEta);

        afterThird.PreviousStopPoint.ShouldBe(StopB);
        afterThird.NextStopPoint.ShouldBe(StopC);
    }

    [Fact]
    public void Apply_WithSameStopRepeated_DoesNotTreatItAsATransition()
    {
        var current = FreshState() with
        {
            PreviousStopPoint = StopA,
            NextStopPoint = StopB,
            ExpectedArrivalAtNext = Now.AddMinutes(2),
        };

        var (state, traversal) = _tracker.Apply(current, StopB, Now.AddMinutes(1).AddSeconds(30), Now.AddSeconds(30));

        traversal.ShouldBeNull();
        state.PreviousStopPoint.ShouldBe(StopA);
        state.NextStopPoint.ShouldBe(StopB);
    }

    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(0.25, 0.75)]
    [InlineData(0.5, 0.5)]
    [InlineData(0.75, 0.25)]
    [InlineData(1.0, 0.0)]
    public void PositionBetweenStops_ComputesFractionBasedOnTimeRemaining(double elapsedFraction, double remainingFraction)
    {
        var averageSegmentDuration = TimeSpan.FromMinutes(4);
        var expectedArrival = Now.Add(averageSegmentDuration * remainingFraction);
        var state = FreshState() with
        {
            NextStopPoint = StopB,
            ExpectedArrivalAtNext = expectedArrival,
        };

        var position = _tracker.PositionBetweenStops(state, Now, averageSegmentDuration);

        position.ShouldNotBeNull();
        position.Value.ShouldBe(elapsedFraction, tolerance: 1e-9);
    }

    [Fact]
    public void PositionBetweenStops_ClampsToOneWhenPastExpectedArrival()
    {
        var averageSegmentDuration = TimeSpan.FromMinutes(4);
        var state = FreshState() with
        {
            NextStopPoint = StopB,
            ExpectedArrivalAtNext = Now.AddMinutes(-1),
        };

        var position = _tracker.PositionBetweenStops(state, Now, averageSegmentDuration);

        position.ShouldBe(1.0);
    }

    [Fact]
    public void PositionBetweenStops_ClampsToZeroWhenFurtherAwayThanAverageDuration()
    {
        var averageSegmentDuration = TimeSpan.FromMinutes(4);
        var state = FreshState() with
        {
            NextStopPoint = StopB,
            ExpectedArrivalAtNext = Now.AddMinutes(10),
        };

        var position = _tracker.PositionBetweenStops(state, Now, averageSegmentDuration);

        position.ShouldBe(0.0);
    }

    [Fact]
    public void PositionBetweenStops_ReturnsNullWhenNextStopPointIsUnknown()
    {
        var state = FreshState();

        _tracker.PositionBetweenStops(state, Now, TimeSpan.FromMinutes(4)).ShouldBeNull();
    }

    [Fact]
    public void PositionBetweenStops_ReturnsNullWhenExpectedArrivalIsUnknown()
    {
        var state = FreshState() with { NextStopPoint = StopB };

        _tracker.PositionBetweenStops(state, Now, TimeSpan.FromMinutes(4)).ShouldBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PositionBetweenStops_ReturnsNullWhenAverageSegmentDurationIsNotPositive(int seconds)
    {
        var state = FreshState() with
        {
            NextStopPoint = StopB,
            ExpectedArrivalAtNext = Now.AddMinutes(2),
        };

        _tracker.PositionBetweenStops(state, Now, TimeSpan.FromSeconds(seconds)).ShouldBeNull();
    }
}

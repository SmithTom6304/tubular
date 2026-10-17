using Common.Domain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ArrivalsSimulator;

internal class LineArrivalPredictionSimulator(
    string line,
    Func<string, ICollection<StopPoint>> getStopPointsForLine,
    TrainIdGenerator trainIdGenerator,
    ArrivalPredictionProducer producer,
    ILogger<LineArrivalPredictionSimulator> logger
) : BackgroundService
{
    private class TrainState
    {
        public required TrainId TrainId { get; init; }
        public required int StopPointIndex { get; set; }
        public required TimeSpan TimeRemaining { get; set; }
    }

    private TimeSpan SimulationInterval { get; } = TimeSpan.FromSeconds(5);
    private const int MaxTrainsOnLine = 3;
    private Queue<TrainState> LineState { get; } = new(MaxTrainsOnLine);
    private IReadOnlyList<StopPoint> StopPointsForLine { get; } = getStopPointsForLine(line).ToList();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting LineArrivalPredictionSimulator for line {Line}. {SimulationInterval} {MaxTrainsOnLine}",
            line, SimulationInterval, MaxTrainsOnLine);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Trains will reach end of station one at a time
            if (LineState.Count > 0 && LineState.Peek().StopPointIndex >= StopPointsForLine.Count)
            {
                logger.LogDebug("Train {Train} has reached the end of line {Line}", LineState.Dequeue().TrainId, line);
            }

            // Add trains one loop at a time
            if (LineState.Count < MaxTrainsOnLine)
            {
                var trainState = new TrainState()
                {
                    TrainId = trainIdGenerator.GenerateTrainId(),
                    StopPointIndex = 0,
                    TimeRemaining = TimeSpan.FromSeconds(Random.Shared.Next(10, 20))
                };
                logger.LogDebug("Train {Train} has started running on line {Line}", trainState.TrainId, line);
                LineState.Enqueue(trainState);
            }

            var predictions = new List<ArrivalPrediction>();

            foreach (var train in LineState)
            {
                if (train.StopPointIndex >= StopPointsForLine.Count)
                {
                    logger.LogWarning("Train {Train} exceeded number of stops ({Index}/{StopPointCount}) for line " +
                        "{Line} without being removed from state queue. Skipping.",
                        train.TrainId, train.StopPointIndex, StopPointsForLine.Count, line);
                    continue;
                }

                var timeRemoved = TimeSpan.FromSeconds(Random.Shared.Next(-3, 10));
                train.TimeRemaining -= timeRemoved;
                if (train.TimeRemaining <= TimeSpan.Zero)
                {
                    train.StopPointIndex++;
                    train.TimeRemaining = TimeSpan.FromSeconds(Random.Shared.Next(10, 20));

                    // Past the final stop: no prediction to make; the train is dequeued next loop.
                    if (train.StopPointIndex >= StopPointsForLine.Count)
                    {
                        continue;
                    }
                }

                predictions.Add(new ArrivalPrediction(
                    train.TrainId,
                    line,
                    StopPointsForLine[train.StopPointIndex].NaptanId,
                    DateTimeOffset.UtcNow + train.TimeRemaining));
            }

            if (predictions.Count > 0)
                await producer.SendPredictionsAsync(predictions, stoppingToken);

            await Task.Delay(SimulationInterval, stoppingToken);
        }

        logger.LogInformation("Stopping LineArrivalPredictionSimulator for line {Line}.", line);
    }
}
using System.Collections.Concurrent;
using Common.Domain;
using Microsoft.Extensions.Hosting;
using NetworkState.Application;
using Orleans;
using Orleans.Streams;
using Spectre.Console;

namespace NetworkState.DebugViewer;

/// Renders a live table of every train currently on the shared trains stream (see ADR-0006 and
/// TrainStreamConstants.AllTrainsKey) — Id, Line, From/To stop, ETA and Position. Purely a debug
/// tool: no persistence, state lives only for as long as this process runs.
public sealed class TrainStreamDashboard(IClusterClient client) : BackgroundService
{
    private readonly ConcurrentDictionary<TrainId, TrainPositionChanged> _trains = new();
    private int _offset;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var stream = client.GetStreamProvider(TrainStreamConstants.ProviderName)
            .GetStream<TrainStreamEvent>(TrainStreamConstants.Namespace, TrainStreamConstants.AllTrainsKey);

        await stream.SubscribeAsync((streamEvent, sequenceToken) =>
        {
            switch (streamEvent)
            {
                case TrainPositionChanged positionChanged:
                    _trains[positionChanged.Id] = positionChanged;
                    break;
                case TrainGone gone:
                    _trains.TryRemove(gone.Id, out _);
                    break;
            }

            return Task.CompletedTask;
        });

        var canReadKeys = !Console.IsInputRedirected;
        var refreshInterval = TimeSpan.FromMilliseconds(500);
        var pollInterval = TimeSpan.FromMilliseconds(50);

        await AnsiConsole.Live(BuildTable())
            .StartAsync(async ctx =>
            {
                var lastRefresh = DateTimeOffset.MinValue;
                while (!stoppingToken.IsCancellationRequested)
                {
                    var scrolled = canReadKeys && HandleKeys();
                    if (scrolled || DateTimeOffset.UtcNow - lastRefresh >= refreshInterval)
                    {
                        ctx.UpdateTarget(BuildTable());
                        lastRefresh = DateTimeOffset.UtcNow;
                    }

                    await Task.Delay(pollInterval, stoppingToken);
                }
            });
    }

    /// Drains pending key presses and adjusts the scroll offset. Returns true if any key changed it.
    /// The offset is clamped in BuildTable, so over-scrolling here is harmless.
    private bool HandleKeys()
    {
        var changed = false;
        var page = Math.Max(1, VisibleRows());
        while (Console.KeyAvailable)
        {
            var previous = _offset;
            switch (Console.ReadKey(intercept: true).Key)
            {
                case ConsoleKey.UpArrow: _offset--; break;
                case ConsoleKey.DownArrow: _offset++; break;
                case ConsoleKey.PageUp: _offset -= page; break;
                case ConsoleKey.PageDown: _offset += page; break;
                case ConsoleKey.Home: _offset = 0; break;
                case ConsoleKey.End: _offset = int.MaxValue; break;
            }

            _offset = Math.Max(0, _offset);
            changed |= _offset != previous;
        }

        return changed;
    }

    // Window height minus the table's title, header row, header separator and bottom border,
    // plus a line for the caption. Re-read every time so resizing the terminal works.
    private static int VisibleRows() => Math.Max(1, Console.WindowHeight - 6);

    private Table BuildTable()
    {
        var trains = _trains.Values.OrderBy(t => t.Id.ToString()).ToList();
        var visible = VisibleRows();
        _offset = Math.Clamp(_offset, 0, Math.Max(0, trains.Count - visible));

        var table = new Table().Title("Train Positions").Expand();
        table.Caption(trains.Count == 0
            ? "No trains"
            : $"{_offset + 1}–{Math.Min(_offset + visible, trains.Count)} of {trains.Count}  (↑/↓ PgUp/PgDn Home/End to scroll)");
        table.AddColumn("Train");
        table.AddColumn("Line");
        table.AddColumn("From");
        table.AddColumn("To");
        table.AddColumn("ETA");
        table.AddColumn("SecondsToArrival");
        table.AddColumn("Position");

        var now = DateTimeOffset.UtcNow;
        foreach (var train in trains.Skip(_offset).Take(visible))
        {
            table.AddRow(
                train.Id.ToString(),
                train.Line,
                train.PreviousStopPoint?.ToString() ?? "-",
                train.NextStopPoint?.ToString() ?? "-",
                train.ExpectedArrivalAtNext?.ToString("HH:mm:ss") ?? "-",
                train.ExpectedArrivalAtNext is { } eta ? $"{(eta - now).TotalSeconds:F1}" : "-",
                train.Position is { } position ? $"{position:P0}" : "-");
        }

        return table;
    }
}

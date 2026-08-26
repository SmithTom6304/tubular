using TfL.Entities;
using Common;

namespace TflMapBuilder;

public class ArrivalsProcessor
{
    public void ProcessArrivals(Map map, IEnumerable<TfLArrival> arrivals)
    {
        var groupedByVehicle = arrivals.GroupBy(arrival => arrival.VehicleId);
        foreach(var group in groupedByVehicle)
            ProcessVehicle(group);

        return;

        void ProcessVehicle(IEnumerable<TfLArrival> vehicleArrivals)
        {
            var sortedArrivals = vehicleArrivals
                .OrderBy(arrival => arrival.TimeToStation)
                .DistinctBy(arrival => arrival.StationName)
                .ToList();
            for (int i = 1; i < sortedArrivals.Count(); i++)
            {
                var from = sortedArrivals[i - 1].ToMapStopPoint();
                var to = sortedArrivals[i].ToMapStopPoint();
                // TODO Dont need to parse line each time
                if (!sortedArrivals[i - 1].LineId.TryParse(out var line))
                {
                    // TODO Handle error
                    continue;
                }

                // Add stations if not present
                map.StopPoints.Add(from);
                map.StopPoints.Add(to);

                var link = new MapLink(from, to, line.Value);
                map.Links.Add(link);

                // Due to nature of API messages, we cannot get data going from one endpoint to second stop on line
                // This is the hack to represent those
                var backwardLink = new MapLink(to, from, line.Value);
                map.Links.Add(backwardLink);
            }
        }
    }
}
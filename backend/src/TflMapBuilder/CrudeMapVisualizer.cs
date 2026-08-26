namespace TflMapBuilder;

public class CrudeMapVisualizer
{
    public void Visualize(Map map)
    {
        Dictionary<MapStopPoint, int> stopPointEntries = new();
        foreach (var link in map.Links)
        {
            stopPointEntries.TryAdd(link.To, 0);
            stopPointEntries[link.To]++;
        }

        var endPoints = stopPointEntries.Where(sp => sp.Value == 1).Select(sp => sp.Key).ToList();

        List<List<MapStopPoint>> routes = new();
        List<Exception> exceptions = new();
        foreach (var endPoint in endPoints)
        {
            foreach (var otherEndpoint in endPoints.Where(sp => sp != endPoint))
            {
                List<MapStopPoint> route = new();
                if (TryFindRoute(endPoint, otherEndpoint, new HashSet<MapStopPoint>(), route))
                {
                    routes.Add(route);
                }
                else
                {
                    exceptions.Add(new Exception($"Couldn't find route for {endPoint.Name} to {otherEndpoint.Name}"));
                }
            }
        }

        return;

        bool TryFindRoute(MapStopPoint current, MapStopPoint terminus, HashSet<MapStopPoint> visited,
            List<MapStopPoint> route)
        {
            if (current == terminus)
                return true;
            if (!visited.Add(current))
                return false;
            route.Add(current);
            var links = map.Links.Where(link => link.From == current);
            foreach (var link in links)
            {
                if(TryFindRoute(link.To, terminus, visited, route))
                    return true;
            }

            route.Remove(current);
            return false;
        }
    }
}
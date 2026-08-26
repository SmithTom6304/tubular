// See https://aka.ms/new-console-template for more information

using Common;
using TfL;
using TflMapBuilder;
using TflQuery;
using System.Text.Json;

var tflCLient = new TfLClient();
Line[] lines = [Line.Jubilee];
var tflQueries = new TflQueries(tflCLient, lines);

var stopPoints = (await tflQueries.QueryStopPoints(lines[0], CancellationToken.None))
    .Select(MapStopPointExtensions.ToMapStopPoint).ToList();
var orderedStopPoints = (await tflQueries.QueryRouteSequence(lines[0], false, CancellationToken.None))
    .Select(id => stopPoints.First(s => s.NaptanId == id)).ToList();

Console.WriteLine(string.Join(" -> ", orderedStopPoints.Select(stopPoint => stopPoint.Name)));
const string dataPath = "output-file.json";
var jsonOptions = new JsonSerializerOptions() {Converters = { new NaptanIdJsonSerializer() }};
var json = JsonSerializer.Serialize(orderedStopPoints, jsonOptions);
File.WriteAllText(dataPath, json);

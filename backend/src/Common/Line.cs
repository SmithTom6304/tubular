using System.Diagnostics.CodeAnalysis;

namespace Common;

public enum Line
{
    Jubilee,
}

public static class LineExtensions
{
    public static string ToLineId(this Line line)
    {
        return line switch
        {
            Line.Jubilee => "jubilee",
            _ => throw new ArgumentOutOfRangeException(nameof(line), line, null)
        };
    }

    public static bool TryParse(this string lineId, [NotNullWhen(true)] out Line? line)
    {
        line = lineId switch
        {
            "jubilee" => Line.Jubilee,
            _ => null
        };

        return line != null;
    }
}
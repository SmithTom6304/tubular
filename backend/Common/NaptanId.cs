using System.Text;

namespace Common;

public readonly struct NaptanId : IEquatable<NaptanId>
{
    private readonly byte[] _data;

    private NaptanId(byte[] data)
    {
        _data = data;
    }

    public static NaptanId FromString(string value)
    {
        const byte maxNaptanLength = 12;
        if (value.Length > maxNaptanLength)
            throw new Exception($"Invalid naptan id - {value}");

        var zeroOffset = maxNaptanLength - value.Length;
        var data = new byte[12];
        for (int i = zeroOffset; i < 12; i++)
        {
            data[i] = (byte)(value[i - zeroOffset] - '0');
        }

        return new NaptanId(data);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        int i = 0;
        while (_data[i] == 0)
            i++;

        for (; i < 12; i++)
        {
            sb.Append((char)(_data[i] + '0'));
        }

        return sb.ToString();
    }

    public bool Equals(NaptanId other)
    {
        if (_data.Length != other._data.Length)
            return false;
        return _data.Zip(other._data, (a, b) => a.Equals(b)).All(b => b);
    }

    public override bool Equals(object? obj)
    {
        return obj is NaptanId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _data.GetHashCode();
    }

    public static bool operator ==(NaptanId left, NaptanId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NaptanId left, NaptanId right)
    {
        return !left.Equals(right);
    }
}
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using Common.Domain.Serialization;

namespace Common.Domain;

[JsonConverter(typeof(NaptanIdJsonSerializer))]
public readonly struct NaptanId : IEquatable<NaptanId>
{
    [InlineArray(12)]
    private struct Buffer
    {
        private byte _element0;
    }

    private readonly Buffer _data;

    private NaptanId(Buffer data)
    {
        _data = data;
    }

    public static NaptanId FromString(string value)
    {
        const byte maxNaptanLength = 12;
        if (value.Length > maxNaptanLength)
            throw new Exception($"Invalid naptan id - {value}");

        var zeroOffset = maxNaptanLength - value.Length;
        var data = new Buffer();
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
        while (i < 12 && _data[i] == 0)
            i++;

        for (; i < 12; i++)
        {
            sb.Append((char)(_data[i] + '0'));
        }

        return sb.ToString();
    }

    public bool Equals(NaptanId other)
    {
        for (int i = 0; i < 12; i++)
        {
            if (_data[i] != other._data[i])
                return false;
        }
        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is NaptanId other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        for (int i = 0; i < 12; i++)
        {
            hash.Add(_data[i]);
        }
        return hash.ToHashCode();
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
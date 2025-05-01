using System;
using System.Collections;
using System.Collections.Generic;

namespace WebSocketSharp;

internal class PayloadData : IEnumerable<byte>
{
    private byte[] _data;
    private long _length;

    /// <summary>
    /// Represents the empty payload data.
    /// </summary>
    public static readonly PayloadData Empty;

    /// <summary>
    /// Represents the allowable max length of payload data.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   A <see cref="WebSocketException"/> will occur when the length of
    ///   incoming payload data is greater than the value of this field.
    ///   </para>
    ///   <para>
    ///   If you would like to change the value of this field, it must be
    ///   a number between <see cref="WebSocket.FragmentLength"/> and
    ///   <see cref="Int64.MaxValue"/> inclusive.
    ///   </para>
    /// </remarks>
    public static readonly ulong MaxLength;

    static PayloadData()
    {
        Empty = new PayloadData(WebSocket.EmptyBytes, 0);
        MaxLength = Int64.MaxValue;
    }

    internal PayloadData(byte[] data)
      : this(data, data.LongLength)
    {
    }

    internal PayloadData(byte[] data, long length)
    {
        _data = data;
        _length = length;
    }

    internal PayloadData(ushort code, string reason)
    {
        _data = code.Append(reason);
        _length = _data.LongLength;
    }

    internal ushort Code => _length >= 2
        ? _data.SubArray(0, 2).ToUInt16(ByteOrder.Big)
        : (ushort)1005;

    internal long ExtensionDataLength { get; set; }

    internal bool HasReservedCode => _length >= 2 && Code.IsReserved();

    internal string Reason
    {
        get
        {
            if (_length <= 2)
                return String.Empty;

            var raw = _data.SubArray(2, _length - 2);

            string reason;
            return raw.TryGetUTF8DecodedString(out reason)
                ? reason
                : String.Empty;
        }
    }

    public byte[] ApplicationData => ExtensionDataLength > 0
        ? _data.SubArray(ExtensionDataLength, _length - ExtensionDataLength)
        : _data;

    public byte[] ExtensionData => ExtensionDataLength > 0
        ? _data.SubArray(0, ExtensionDataLength)
        : WebSocket.EmptyBytes;

    public ulong Length => (ulong)_length;

    internal void Mask(byte[] key)
    {
        for (long i = 0; i < _length; i++)
            _data[i] = (byte)(_data[i] ^ key[i % 4]);
    }

    public IEnumerator<byte> GetEnumerator()
    {
        foreach (var b in _data)
            yield return b;
    }

    public byte[] ToArray()
    {
        return _data;
    }

    public override string ToString()
    {
        return BitConverter.ToString(_data);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

using System;

namespace WebSocketSharp;

/// <summary>
/// Represents the event data for the <see cref="WebSocket.OnMessage"/> event.
/// </summary>
/// <remarks>
///   <para>
///   That event occurs when the <see cref="WebSocket"/> receives
///   a message or a ping if the <see cref="WebSocket.EmitOnPing"/>
///   property is set to <c>true</c>.
///   </para>
///   <para>
///   If you would like to get the message data, you should access
///   the <see cref="Data"/> or <see cref="RawData"/> property.
///   </para>
/// </remarks>
public class MessageEventArgs : EventArgs
{

    private string _data;
    private bool _dataSet;
    private Opcode _opcode;
    private byte[] _rawData;

    internal MessageEventArgs(WebSocketFrame frame)
    {
        _opcode = frame.Opcode;
        _rawData = frame.PayloadData.ApplicationData;
    }

    internal MessageEventArgs(Opcode opcode, byte[] rawData)
    {
        if ((ulong)rawData.LongLength > PayloadData.MaxLength)
            throw new WebSocketException(CloseStatusCode.TooBig);

        _opcode = opcode;
        _rawData = rawData;
    }

    /// <summary>
    /// Gets the opcode for the message.
    /// </summary>
    /// <value>
    /// <see cref="Opcode.Text"/>, <see cref="Opcode.Binary"/>,
    /// or <see cref="Opcode.Ping"/>.
    /// </value>
    internal Opcode Opcode => _opcode;

    /// <summary>
    /// Gets the message data as a <see cref="string"/>.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> that represents the message data if its type is
    /// text or ping and if decoding it to a string has successfully done;
    /// otherwise, <see langword="null"/>.
    /// </value>
    public string Data
    {
        get
        {
            setData();
            return _data;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the message type is binary.
    /// </summary>
    /// <value>
    /// <c>true</c> if the message type is binary; otherwise, <c>false</c>.
    /// </value>
    public bool IsBinary => _opcode == Opcode.Binary;

    /// <summary>
    /// Gets a value indicating whether the message type is ping.
    /// </summary>
    /// <value>
    /// <c>true</c> if the message type is ping; otherwise, <c>false</c>.
    /// </value>
    public bool IsPing => _opcode == Opcode.Ping;

    /// <summary>
    /// Gets a value indicating whether the message type is text.
    /// </summary>
    /// <value>
    /// <c>true</c> if the message type is text; otherwise, <c>false</c>.
    /// </value>
    public bool IsText => _opcode == Opcode.Text;

    /// <summary>
    /// Gets the message data as an array of <see cref="byte"/>.
    /// </summary>
    /// <value>
    /// An array of <see cref="byte"/> that represents the message data.
    /// </value>
    public byte[] RawData
    {
        get
        {
            setData();
            return _rawData;
        }
    }

    private void setData()
    {
        if (_dataSet)
            return;

        if (_opcode == Opcode.Binary)
        {
            _dataSet = true;
            return;
        }

        string data;
        if (_rawData.TryGetUTF8DecodedString(out data))
            _data = data;

        _dataSet = true;
    }
}

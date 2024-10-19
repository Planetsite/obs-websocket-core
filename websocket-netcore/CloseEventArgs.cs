using System;

namespace WebSocketSharp;

/// <summary>
/// Represents the event data for the <see cref="WebSocket.OnClose"/> event.
/// </summary>
/// <remarks>
///   <para>
///   That event occurs when the WebSocket connection has been closed.
///   </para>
///   <para>
///   If you would like to get the reason for the connection close, you should
///   access the <see cref="Code"/> or <see cref="Reason"/> property.
///   </para>
/// </remarks>
public class CloseEventArgs : EventArgs
{
    private bool _clean;
    private PayloadData _payloadData;

    internal CloseEventArgs(PayloadData payloadData, bool clean)
    {
        _payloadData = payloadData;
        _clean = clean;
    }

    internal CloseEventArgs(ushort code, string reason, bool clean)
    {
        _payloadData = new PayloadData(code, reason);
        _clean = clean;
    }

    /// <summary>
    /// Gets the status code for the connection close.
    /// </summary>
    /// <value>
    /// A <see cref="ushort"/> that represents the status code for
    /// the connection close if present.
    /// </value>
    public ushort Code => _payloadData.Code;

    /// <summary>
    /// Gets the reason for the connection close.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> that represents the reason for
    /// the connection close if present.
    /// </value>
    public string Reason => _payloadData.Reason;

    /// <summary>
    /// Gets a value indicating whether the connection has been closed cleanly.
    /// </summary>
    /// <value>
    /// <c>true</c> if the connection has been closed cleanly; otherwise,
    /// <c>false</c>.
    /// </value>
    public bool WasClean => _clean;
}

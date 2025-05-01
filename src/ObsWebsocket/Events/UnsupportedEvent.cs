using Newtonsoft.Json.Linq;
using System;

namespace ObsWebsocket.Events;

/// <summary>
/// Event args for unsupported events
/// </summary>
public sealed class UnsupportedEventArgs : EventArgs
{
    public string EventType { get; }
    public JObject Body { get; }

    public UnsupportedEventArgs(string eventType, JObject body)
    {
        EventType = eventType;
        Body = body;
    }
}

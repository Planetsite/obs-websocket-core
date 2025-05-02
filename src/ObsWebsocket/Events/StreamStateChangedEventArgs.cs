using ObsWebsocket.Types;
using System;

namespace ObsWebsocket.Events;

/// <summary>
/// Event args for <see cref="OBSWebsocket.StreamStateChangedAsync"/>
/// </summary>
public sealed class StreamStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// The specific state of the output
    /// </summary>
    public OutputStateChanged OutputState { get; }

    /// <summary>
    /// Default Constructor
    /// </summary>
    /// <param name="outputState">The output state data</param>
    public StreamStateChangedEventArgs(OutputStateChanged outputState)
    {
        OutputState = outputState;
    }
}

using System;

namespace ObsWebsocket;

/// <summary>
/// Thrown when the server responds with an error
/// </summary>
public sealed class ErrorResponseException : Exception
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="message"></param>
    public ErrorResponseException(string message) : base(message)
    {
    }
}

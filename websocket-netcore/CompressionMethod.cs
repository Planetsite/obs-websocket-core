namespace WebSocketSharp;

/// <summary>
/// Specifies the method for compression.
/// </summary>
/// <remarks>
/// The methods are defined in
/// <see href="https://tools.ietf.org/html/rfc7692">
/// Compression Extensions for WebSocket</see>.
/// </remarks>
public enum CompressionMethod : byte
{
    /// <summary>
    /// Specifies no compression.
    /// </summary>
    None,
    /// <summary>
    /// Specifies DEFLATE.
    /// </summary>
    Deflate
}

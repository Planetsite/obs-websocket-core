using Newtonsoft.Json;

namespace ObsWebsocket.Types;

/// <summary>
/// Streaming settings
/// </summary>
public sealed class StreamingService : StandardResponse
{
    /// <summary>
    /// Type of streaming service
    /// </summary>
    [JsonProperty(PropertyName = "type")]
    public string Type { set; get; }

    /// <summary>
    /// Streaming service settings (JSON data)
    /// </summary>
    //[JsonProperty(PropertyName = "source")]
    public StreamingServiceSettings Settings { set; get; }
}
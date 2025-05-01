using Newtonsoft.Json;
namespace OBSWebsocketDotNet;

/// <summary>
///
/// </summary>
public sealed class MediaSourceSettings
{
    /// <summary>
    /// Source Name
    /// </summary>
    [JsonProperty(PropertyName = "sourceName")]
    public string SourceName { get; set; }

    /// <summary>
    /// Source Type
    /// </summary>
    [JsonProperty(PropertyName = "sourceType")]
    public string SourceType { get; set; }

    /// <summary>
    /// Media settings
    /// </summary>
    [JsonProperty(PropertyName = "sourceSettings")]
    public FFMpegSourceSettings Media { get; set; }
}

using Newtonsoft.Json;

namespace OBSWebsocketDotNet.Types;

/// <summary>
/// Response from <see cref="OBSWebsocket.TakeSourceScreenshot(string)"/>
/// </summary>
public sealed class SourceScreenshotResponse
{
    /// <summary>
    /// Source name
    /// </summary>
    [JsonProperty(PropertyName = "sourceName")]
    public string SourceName { internal set; get; }

    /// <summary>
    /// Image Data URI(if embedPictureFormat was specified in the request)
    /// </summary>
    [JsonProperty(PropertyName = "img")]
    public string ImageData { internal set; get; }

    /// <summary>
    /// Absolute path to the saved image file(if saveToFilePath was specified in the request)
    /// </summary>
    [JsonProperty(PropertyName = "imageFile")]
    public string ImageFile { internal set; get; }
}

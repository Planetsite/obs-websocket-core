using Newtonsoft.Json;

namespace OBSWebsocketDotNet.Types;

/// <summary>
/// Stub for scene item that only contains the name or ID of an item
/// </summary>
public sealed class SceneItemStub
{
    /// <summary>
    /// Source name
    /// </summary>
    [JsonProperty(PropertyName = "name")]
    public string SourceName;

    /// <summary>
    /// Scene item ID
    /// </summary>
    [JsonProperty(PropertyName = "id")]
    public int ID { set; get; }
}

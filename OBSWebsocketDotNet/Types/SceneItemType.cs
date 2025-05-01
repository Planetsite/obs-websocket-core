using Newtonsoft.Json;

namespace OBSWebsocketDotNet.Types;

public sealed class SceneItemType
{
    [JsonProperty(PropertyName = "itemId")]
    public int ItemId;

    [JsonProperty(PropertyName = "sourceKind")]
    public SceneItemSourceKind SourceKind;

    [JsonProperty(PropertyName = "sourceName")]
    public string SourceName;

    [JsonProperty(PropertyName = "sourceType")]
    public SceneItemSourceType SourceType;
}

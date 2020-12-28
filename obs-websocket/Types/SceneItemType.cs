using Newtonsoft.Json;

namespace OBSWebsocketDotNet.Types
{
    public class SceneItemType
    {
        [JsonProperty(PropertyName = "itemId")]
        public int ItemId;

        [JsonProperty(PropertyName = "sourceKing")]
        public SceneItemSourceKind SourceKind;

        [JsonProperty(PropertyName = "sourceName")]
        public string SourceName;

        [JsonProperty(PropertyName = "sourceType")]
        public SceneItemSourceType SourceType;
    }
}

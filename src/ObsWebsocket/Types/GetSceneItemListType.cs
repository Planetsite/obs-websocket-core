using Newtonsoft.Json;

namespace ObsWebsocket.Types;

public class GetSceneItemListType
{
    [JsonProperty(PropertyName = "sceneName")]
    public string SceneName { get; set; }

    [JsonProperty(PropertyName = "sceneItems")]
    public SceneItemType[] SceneItems { get; set; }
}

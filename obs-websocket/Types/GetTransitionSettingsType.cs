using Newtonsoft.Json;

namespace OBSWebsocketDotNet.Types;

internal class GetTransitionSettingsType
{
    [JsonProperty(PropertyName = "transitionSettings")]
    public TransitionSettingsAll TransitionSettings { get; set; }
}

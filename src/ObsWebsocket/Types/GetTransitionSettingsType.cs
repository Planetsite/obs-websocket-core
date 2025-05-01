using Newtonsoft.Json;

namespace ObsWebsocket.Types;

internal class GetTransitionSettingsType
{
    [JsonProperty(PropertyName = "transitionSettings")]
    public TransitionSettingsAll TransitionSettings { get; set; }
}

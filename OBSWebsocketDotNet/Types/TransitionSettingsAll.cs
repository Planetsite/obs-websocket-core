using Newtonsoft.Json;

namespace OBSWebsocketDotNet.Types;

public sealed class TransitionSettingsAll
{
    [JsonProperty(PropertyName = "color")]
    public uint? Color { get; set; }

    [JsonProperty(PropertyName = "direction")]
    public Direction? Direction { get; set; }

    [JsonProperty(PropertyName = "path")]
    public string Path { get; set; }

    [JsonProperty(PropertyName = "tp_type")]
    public int? TransitionPointType { get; set; }

    [JsonProperty(PropertyName = "transition_point")]
    public int? TransitionPoint { get; set; }

    [JsonProperty(PropertyName = "luma_image")]
    public string LumaImage { get; set; }

    public LumaWipeType? LumaMode { get; set; }

    [JsonProperty(PropertyName = "luma_invert")]
    public bool? LumaInvert { get; set; }

    [JsonProperty(PropertyName = "luma_softness")]
    public float? LumaSoftness { get; set; }
}

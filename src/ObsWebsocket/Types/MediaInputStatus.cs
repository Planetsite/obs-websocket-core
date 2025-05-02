using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;

namespace ObsWebsocket.Types;

public sealed class MediaInputStatus
{
    /// <summary>
    /// Position of the cursor in milliseconds. `null` if not playing
    /// </summary>
    [JsonProperty(PropertyName = "mediaCursor")]
    public long? Cursor { get; set; }

    /// <summary>
    /// Total duration of the playing media in milliseconds. `null` if not playing
    /// </summary>
    [JsonProperty(PropertyName = "mediaDuration")]
    public long? Duration { get; set; }

    [JsonIgnore]
    public MediaState? State
        => Enum.TryParse(StateString, out MediaState state)
            ? state
            : null;

    [JsonProperty(PropertyName = "mediaState")]
    public string StateString { get; set; }

    public MediaInputStatus(JObject body)
    {
        JsonConvert.PopulateObject(body.ToString(), this);
    }

    public MediaInputStatus() { }
}

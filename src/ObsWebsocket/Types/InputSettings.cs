using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ObsWebsocket.Types;

/// <summary>
/// Settings for a source item
/// </summary>
public sealed class InputSettings : Input
{
    /// <summary>
    /// Settings for the source
    /// </summary>
    [JsonProperty(PropertyName = "inputSettings")]
    public JObject Settings { set; get; }

    /// <summary>
    /// Builds the object from the JSON data
    /// </summary>
    /// <param name="data">JSON item description as a <see cref="JObject"/></param>
    public InputSettings(JObject data) : base(data)
    {
        JsonConvert.PopulateObject(data.ToString(), this);
    }

    public InputSettings() { }
}
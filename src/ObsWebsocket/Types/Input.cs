using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ObsWebsocket.Types;

/// <summary>
/// Abstract class with information on a specific Input
/// </summary>
public abstract class Input
{
    /// <summary>
    /// Name of the Input
    /// </summary>
    [JsonProperty(PropertyName = "inputName")]
    public string InputName { get; set; }

    /// <summary>
    /// Kind of the Input
    /// </summary>
    [JsonProperty(PropertyName = "inputKind")]
    public string InputKind { get; set; }

    /// <summary>
    /// Instantiate object from response data
    /// </summary>
    /// <param name="body"></param>
    public Input(JObject body)
    {
        JsonConvert.PopulateObject(body.ToString(), this);
    }

    public Input() { }
}
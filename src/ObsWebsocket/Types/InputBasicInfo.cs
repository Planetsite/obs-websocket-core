using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ObsWebsocket.Types;

/// <summary>
/// Input class which also shows the Unversioned Input Kind
/// </summary>
public sealed class InputBasicInfo : Input
{
    /// <summary>
    /// Unversioned Kind of the Input
    /// </summary>
    [JsonProperty(PropertyName = "unversionedInputKind")]
    public string UnversionedKind { get; set; }

    /// <summary>
    /// Instantiate object from response data
    /// </summary>
    /// <param name="body"></param>
    public InputBasicInfo(JObject body) : base(body)
    {
        JsonConvert.PopulateObject(body.ToString(), this);
    }

    public InputBasicInfo() { }
}

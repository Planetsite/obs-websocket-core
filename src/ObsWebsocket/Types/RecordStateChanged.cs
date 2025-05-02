using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ObsWebsocket.Types;

public sealed class RecordStateChanged : OutputStateChanged
{
    /// <summary>
    /// File name for the saved recording, if record stopped. null otherwise
    /// </summary>
    [JsonProperty(PropertyName = "outputPath")]
    public string OutputPath { set; get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="body"></param>
    public RecordStateChanged(JObject body) : base(body)
    {
        JsonConvert.PopulateObject(body.ToString(), this);
    }

    public RecordStateChanged() { }
}

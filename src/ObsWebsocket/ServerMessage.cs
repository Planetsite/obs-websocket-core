using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ObsWebsocket;

internal sealed class ServerMessage
{
    [JsonProperty(PropertyName = "op")]
    public MessageV5Types OperationCode { set; get; }

    [JsonProperty(PropertyName = "d")]
    public JObject Data { get; set; }
}

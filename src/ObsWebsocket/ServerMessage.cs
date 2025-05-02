using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ObsWebsocket;

internal class ServerMessage
{
    [JsonProperty(PropertyName = "op")]
    public MessageTypes OperationCode { set; get; }

    [JsonProperty(PropertyName = "d")]
    public JObject Data { get; set; }
}

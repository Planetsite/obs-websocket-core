using Newtonsoft.Json;

namespace OBSWebsocketDotNet.Types
{
    /// <summary>
    /// StandardResponse
    /// </summary>
    public class StandardResponse
    {
        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "message-id")]
        public string MessageId { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }
    }
}

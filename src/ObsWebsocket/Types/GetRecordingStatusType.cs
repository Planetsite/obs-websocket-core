using Newtonsoft.Json;

namespace OBSWebsocketDotNet.Types;

public class GetRecordingStatusType
{
    [JsonProperty(PropertyName = "isRecording")]
    public bool IsRecording { get; set; }

    [JsonProperty(PropertyName = "isRecordingPaused")]
    public bool IsRecordingPaused { get; set; }

    [JsonProperty(PropertyName = "recordTimecode")]
    public string RecordTimecode { get; set; }

    [JsonProperty(PropertyName = "recordingFilename")]
    public string RecordingFilename { get; set; }
}

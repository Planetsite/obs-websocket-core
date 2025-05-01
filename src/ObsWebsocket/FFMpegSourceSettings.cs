using Newtonsoft.Json;

namespace ObsWebsocket;

public sealed class FFMpegSourceSettings
{
    [JsonProperty(PropertyName = "clear_on_media_end")]
    public bool ClearOnMediaEnd { get; set; }

    [JsonProperty(PropertyName = "close_when_inactive")]
    public bool CloseWhenInactive { get; set; }

    [JsonProperty(PropertyName = "color_range")]
    public int ColorRange { get; set; }

    [JsonProperty(PropertyName = "hw_decode")]
    public bool HWDecode { get; set; }

    [JsonProperty(PropertyName = "looping")]
    public bool Looping { get; set; }

    [JsonProperty(PropertyName = "restart_on_activate")]
    public bool RestartOnActivate { get; set; }

    [JsonProperty(PropertyName = "is_local_file")]
    public bool IsLocalFile { get; set; }

    public string LocalFile { get; set; }

    [JsonProperty(PropertyName = "speed_percent")]
    public int SpeedPercent { get; set; }

    public FFMpegSourceSettings()
    {
        SpeedPercent = 100;
        RestartOnActivate = true;
        HWDecode = true;
        ClearOnMediaEnd = true;
        IsLocalFile = true;
    }
}

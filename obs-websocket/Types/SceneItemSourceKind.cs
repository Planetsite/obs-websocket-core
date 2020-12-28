using Newtonsoft.Json;

namespace OBSWebsocketDotNet.Types
{
    public enum SceneItemSourceKind
    {
        ffmpeg_source,
        window_capture,
        image_source,
        color_source_v3,
        wasapi_input_capture,
        dshow_input,
        vlc_source,
        text_gdiplus_v2,
        scene,
        ndi_source,
        slideshow,
        game_capture,
        monitor_capture,
        browser_source,
        [JsonProperty(PropertyName = "decklink-input")]
        decklink_input,
        wasapi_output_capture,
        text_ft2_source_v2,
        group
    }
}

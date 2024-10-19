using Newtonsoft.Json;

namespace OBSWebsocketDotNet.Types;

public sealed class TextFreetype2Properties
{
    public TextFreetype2Properties(string sourceName) =>
        SourceName = sourceName;

    /// <summary>
    /// Source name.
    /// </summary>
    [JsonProperty(PropertyName = "source")]
    public string SourceName { set; get; }

    /// <summary>
    /// </summary>
    [JsonProperty(PropertyName = "color1")]
    public ulong GradientTopColor { set; get; }

    /// <summary>
    /// </summary>
    [JsonProperty(PropertyName = "color2")]
    public ulong GradientBottomColor { set; get; }

    /// <summary>
    /// Holds data for the font. Ex: "font": { "face": "Arial", "flags": 0, "size": 150, "style": "" }
    /// </summary>
    [JsonProperty(PropertyName = "font")]
    public TextGDIPlusFont Font { set; get; }

    /// <summary>
    /// Outline.
    /// </summary>
    [JsonProperty(PropertyName = "outline")]
    public bool HasOutline { set; get; }

    /// <summary>
    /// Text content to be displayed.
    /// </summary>
    [JsonProperty(PropertyName = "text")]
    public string Text { set; get; }

    /// <summary>
    /// </summary>
    [JsonProperty(PropertyName = "drop_shadow")]
    public bool DropShadow { set; get; }

    /// <summary>
    /// </summary>
    [JsonProperty(PropertyName = "word_wrap")]
    public bool WordWrap { set; get; }

    /// <summary>
    /// </summary>
    [JsonProperty(PropertyName = "log_mode")]
    public bool ChatLog { set; get; }

    /// <summary>
    /// </summary>
    [JsonProperty(PropertyName = "from_file")]
    public bool TextFromFile { set; get; }

    /// <summary>
    /// </summary>
    [JsonProperty(PropertyName = "text_file")]
    public string TextFilenamePath { set; get; }
}
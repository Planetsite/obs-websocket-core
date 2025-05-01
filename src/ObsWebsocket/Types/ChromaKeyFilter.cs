namespace OBSWebsocketDotNet.Types;

public class ChromaKeyFilter : IFilterProperties
{
    public float brightness;
    public float contrast;
    public float gamma;
    public uint key_color;
    public ChromaKeyFilterColorType key_color_type;
    public int opacity;
    public int similarity;
    public int smoothness;
    public int spill;
}

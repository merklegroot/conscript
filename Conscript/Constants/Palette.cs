using Raylib_cs;

namespace Conscript.Constants;

public static class Palette
{
    // Base tones - oppressive, cold, earth
    public static readonly Color Bg = new Color(10, 12, 9, 255);
    public static readonly Color PanelBg = new Color(16, 18, 14, 255);
    public static readonly Color SceneBg = new Color(22, 24, 20, 255);
    public static readonly Color Frame = new Color(45, 50, 42, 255);
    public static readonly Color FrameLight = new Color(70, 75, 65, 255);

    // Text
    public static readonly Color TextPrimary = new Color(220, 215, 200, 255);
    public static readonly Color TextDim = new Color(150, 145, 130, 255);
    public static readonly Color TextMuted = new Color(110, 105, 95, 255);

    // Accent / state colors
    public static readonly Color Suspicion = new Color(140, 70, 55, 255);
    public static readonly Color Health = new Color(70, 120, 80, 255);
    public static readonly Color Morale = new Color(80, 95, 130, 255);
    public static readonly Color Exposure = new Color(110, 105, 70, 255);
    public static readonly Color Supplies = new Color(120, 100, 60, 255);

    // UI interaction
    public static readonly Color SelectedBg = new Color(35, 38, 30, 255);
    public static readonly Color SelectedBorder = new Color(90, 95, 80, 255);
    public static readonly Color ActionFlash = new Color(180, 160, 100, 255);

    // Scene elements (muted realistic)
    public static readonly Color Wall = new Color(38, 36, 32, 255);
    public static readonly Color Floor = new Color(28, 26, 22, 255);
    public static readonly Color TableWood = new Color(55, 42, 30, 255);
    public static readonly Color Envelope = new Color(235, 230, 220, 255);
    public static readonly Color StampRed = new Color(120, 35, 30, 255);
    public static readonly Color Person = new Color(30, 28, 26, 255);
    public static readonly Color LampLight = new Color(90, 80, 55, 255);
}
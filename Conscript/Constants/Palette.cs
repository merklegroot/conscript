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

    // Night forest / winter scene tones (for placeholder)
    public static readonly Color NightBg = new Color(8, 10, 16, 255);
    public static readonly Color NightGround = new Color(18, 22, 28, 255);
    public static readonly Color TreeDark = new Color(12, 14, 18, 255);
    public static readonly Color Snow = new Color(170, 175, 185, 255);
    public static readonly Color Shelter = new Color(25, 27, 30, 255);

    // Overlay panels (slightly transparent feeling but solid for readability)
    public static readonly Color OverlayBg = new Color(14, 16, 20, 235);
    public static readonly Color OverlayBorder = new Color(55, 58, 65, 255);

    // Button styles
    public static readonly Color ButtonBg = new Color(22, 24, 28, 255);
    public static readonly Color ButtonBorder = new Color(60, 63, 70, 255);
    public static readonly Color ButtonSelectedBg = new Color(30, 34, 40, 255);
    public static readonly Color ButtonSelectedBorder = new Color(110, 115, 125, 255);

    // Delta / status colors
    public static readonly Color Positive = new Color(130, 175, 95, 255);
    public static readonly Color Negative = new Color(175, 85, 75, 255);
}
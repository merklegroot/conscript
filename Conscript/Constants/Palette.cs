using Raylib_cs;

namespace Conscript.Constants;

/// <summary>
/// A cold, desaturated, high-tension cinematic palette.
/// Inspired by This War of Mine / Papers, Please but pushed darker and more oppressive.
/// </summary>
public static class Palette
{
    // === Foundational Backgrounds (very dark, cold) ===
    public static readonly Color Bg = new Color(7, 8, 11, 255);           // near-black cold void
    public static readonly Color HeaderBg = new Color(11, 12, 16, 255);   // top bar
    public static readonly Color SidebarBg = new Color(13, 15, 19, 255);  // left panel
    public static readonly Color SceneBg = new Color(9, 11, 15, 255);     // the "stage" behind the art
    public static readonly Color ActionBarBg = new Color(10, 11, 15, 255);

    // Subtle structure
    public static readonly Color Divider = new Color(28, 30, 36, 255);
    public static readonly Color SubtleBorder = new Color(38, 41, 48, 255);
    public static readonly Color StrongBorder = new Color(55, 58, 66, 255);

    // === Text Hierarchy (high contrast for tension) ===
    public static readonly Color TextPrimary = new Color(232, 228, 218, 255);   // warm off-white, very readable
    public static readonly Color TextSecondary = new Color(185, 180, 168, 255); // slightly warmer gray
    public static readonly Color TextMuted = new Color(125, 122, 112, 255);     // for labels and notes
    public static readonly Color TextDim = new Color(92, 90, 82, 255);          // very low priority

    // === Core Stat Colors (desaturated, cold, uneasy) ===
    public static readonly Color Health = new Color(78, 118, 92, 255);         // desaturated forest green
    public static readonly Color Satiation = new Color(168, 105, 68, 255);     // dull clay / warmth (higher = better)
    public static readonly Color Hydration = new Color(72, 118, 138, 255);       // cold desaturated blue
    public static readonly Color Comfort = new Color(138, 122, 88, 255);         // warm sheltered ochre (higher = better)
    public static readonly Color Money = new Color(138, 125, 78, 255);         // tarnished gold

    // === UI State ===
    public static readonly Color ActionFlash = new Color(195, 175, 105, 255);  // warm but desaturated highlight

    // === Bottom Action Buttons (more visual weight) ===
    public static readonly Color ButtonBg = new Color(16, 18, 23, 255);
    public static readonly Color ButtonBorder = new Color(48, 51, 58, 255);
    public static readonly Color ButtonSelectedBg = new Color(24, 27, 34, 255);
    public static readonly Color ButtonSelectedBorder = new Color(92, 98, 110, 255);
    public static readonly Color ButtonTopAccent = new Color(72, 78, 88, 255); // thin highlight line when active

    // === Overlay Cards (semi-transparent dark panels for narrative) ===
    public static readonly Color CardBg = new Color(12, 14, 18, 232);
    public static readonly Color CardBorder = new Color(52, 55, 63, 255);

    // === Atmospheric Scene Colors (for the rich placeholder art) ===
    public static readonly Color DeepNight = new Color(6, 7, 12, 255);
    public static readonly Color SnowMid = new Color(155, 160, 170, 255);
    public static readonly Color SnowHighlight = new Color(195, 198, 205, 255);
    public static readonly Color TreeFar = new Color(14, 16, 21, 255);
    public static readonly Color TreeMid = new Color(18, 20, 26, 255);
    public static readonly Color TreeNear = new Color(22, 24, 30, 255);
    public static readonly Color ShelterWood = new Color(32, 34, 38, 255);
    public static readonly Color GroundCold = new Color(16, 19, 24, 255);
    public static readonly Color MoonGlow = new Color(120, 125, 140, 40); // very faint cold light

    // === Status / Delta ===
    public static readonly Color Positive = new Color(118, 152, 92, 255);
    public static readonly Color Negative = new Color(168, 78, 72, 255);
}
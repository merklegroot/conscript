namespace Conscript.Constants;

/// <summary>
/// Core layout measurements for the cinematic survival UI.
/// Designed for generous breathing room, clear hierarchy, and a dark, oppressive atmosphere.
/// </summary>
public static class GameConstants
{
    public const int ScreenWidth = 1280;
    public const int ScreenHeight = 720;

    // === Top Header Bar ===
    public const int TopBarHeight = 62;   // more breathing room, less cramped

    // === Left Sidebar (Stats + Flavor) ===
    public const int SidebarWidth = 292;  // wider for clean list + long status text
    public const int SidebarPadding = 22;
    public const int SidebarInternalGap = 16;

    // === Central Scene Area (the "stage") ===
    // Generous margins + padding so the art and overlays have room to breathe
    public const int SceneMarginTop = 14;
    public const int SceneMarginBottom = 14;
    public const int SceneMarginLeft = 16;
    public const int SceneMarginRight = 20;

    // Inner padding around the actual artwork
    public const int ScenePadding = 24;

    // === Bottom Action Bar ===
    public const int ActionBarHeight = 86;      // taller for visual weight and comfortable padding
    public const int ActionBarPaddingY = 13;
    public const int ActionButtonGap = 12;
    public const int ActionButtonCount = 4;

    // Computed helpers (used in drawing)
    public static int SceneLeft => SidebarWidth + SceneMarginLeft;
    public static int SceneTop => TopBarHeight + SceneMarginTop;
    public static int SceneRight => ScreenWidth - SceneMarginRight;
    public static int SceneBottom => ScreenHeight - ActionBarHeight - SceneMarginBottom;

    public static int SceneWidth => SceneRight - SceneLeft;
    public static int SceneHeight => SceneBottom - SceneTop;
}
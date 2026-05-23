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
    public const int TopBarHeight = 58;

    // === Left Sidebar (Stats + Flavor) ===
    public const int SidebarWidth = 268;
    public const int SidebarPadding = 20;
    public const int SidebarInternalGap = 14;

    // === Central Scene Area (the "stage") ===
    // Generous margins so the image breathes and never feels cramped
    public const int SceneMarginTop = 12;      // gap under the top bar
    public const int SceneMarginBottom = 12;   // gap above the action bar
    public const int SceneMarginLeft = 14;     // gap to the right of the sidebar
    public const int SceneMarginRight = 18;

    // The actual image rect inside the margins (this is where the "art" lives)
    public const int ScenePadding = 18; // inner breathing room around the placeholder art

    // === Bottom Action Bar ===
    public const int ActionBarHeight = 78;      // taller for visual weight and comfortable clicking
    public const int ActionBarPaddingY = 10;
    public const int ActionButtonGap = 10;
    public const int ActionButtonCount = 4;

    // Computed helpers (used in drawing)
    public static int SceneLeft => SidebarWidth + SceneMarginLeft;
    public static int SceneTop => TopBarHeight + SceneMarginTop;
    public static int SceneRight => ScreenWidth - SceneMarginRight;
    public static int SceneBottom => ScreenHeight - ActionBarHeight - SceneMarginBottom;

    public static int SceneWidth => SceneRight - SceneLeft;
    public static int SceneHeight => SceneBottom - SceneTop;
}
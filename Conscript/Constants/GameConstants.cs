namespace Conscript.Constants;

public static class GameConstants
{
    public const int ScreenWidth = 1280;
    public const int ScreenHeight = 720;

    // Top info bar
    public const int TopBarHeight = 50;

    // Main visual area (below top bar, above bottom buttons)
    public const int MainAreaTop = TopBarHeight + 6;
    public const int BottomButtonHeight = 60;
    public const int MainAreaBottom = ScreenHeight - BottomButtonHeight - 6;

    // Inset for the large scene rect inside the main area
    public const int SceneInset = 12;

    // Stats panel (overlaid on left side of scene)
    public const int StatsPanelWidth = 222;
    public const int StatsPanelHeight = 198;

    // Narrative overlays
    public const int ShortNarrativeWidth = 440;
    public const int ShortNarrativeHeight = 28;
    public const int LongNarrativeWidth = 248;
    public const int LongNarrativeHeight = 108;

    // Bottom action buttons row
    public const int ButtonGap = 8;
    public const int ButtonCount = 4;
}
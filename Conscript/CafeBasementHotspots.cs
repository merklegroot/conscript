namespace Conscript;

/// <summary>Clickable regions on cafe-basement.png — steel exit door, wall keypad, coal bins, and storage crate.</summary>
internal static class CafeBasementHotspots
{
    /// <summary>Steel cellar door at the head of the stairs (top of scene).</summary>
    public const float DoorX1 = 0.30f;
    public const float DoorY1 = 0.02f;
    public const float DoorX2 = 0.62f;
    public const float DoorY2 = 0.40f;

    /// <summary>Numeric keypad mounted beside the door frame.</summary>
    public const float KeypadX1 = 0.64f;
    public const float KeypadY1 = 0.06f;
    public const float KeypadX2 = 0.82f;
    public const float KeypadY2 = 0.24f;

    /// <summary>Wooden coal bins along the left wall — corners from area select.</summary>
    public const float CoalBinX1 = 0.02f;
    public const float CoalBinY1 = 0.50f;
    public const float CoalBinX2 = 0.36f;
    public const float CoalBinY2 = 0.98f;

    /// <summary>Stacked wooden crate against the back wall — corners from area select.</summary>
    public const float CrateX1 = 0.49f;
    public const float CrateY1 = 0.42f;
    public const float CrateX2 = 0.57f;
    public const float CrateY2 = 0.58f;

    public const string LockCode = "1973";
}

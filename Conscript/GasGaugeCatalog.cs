using System.Numerics;
using Raylib_cs;

namespace Conscript;

/// <summary>Fuel gauge levels and layout for the warehouse truck cab viewer.</summary>
internal static class GasGaugeCatalog
{
    public const int LevelCount = 5;
    public const int DefaultLevel = 2;

    public static float GetLevelNormalized(int level) =>
        Math.Clamp(level, 0, LevelCount - 1) / (float)(LevelCount - 1);

    public const float PlateOverhang = 12f;
    public const float HubRadius = 16f;

    /// <summary>Vertical extent of the drawn gauge from pivot (plate above, hub below).</summary>
    public static float GetVisualHeight(float radius) => radius + PlateOverhang + HubRadius;

    public static void DrawInRect(Rectangle rect, int level, Font labelFont)
    {
        float radius = rect.Width * 0.5f - 14f;
        float pivotY = rect.Y + rect.Height * 0.5f + (radius - 4f) * 0.5f;
        var pivot = new Vector2(rect.X + rect.Width * 0.5f, pivotY);
        GasGauge.Draw(pivot, radius, GetLevelNormalized(level), labelFont);
    }

    public static string GetLevelLabel(int level) =>
        Math.Clamp(level, 0, LevelCount - 1) switch
        {
            0 => "EMPTY",
            1 => "1/4 TANK",
            2 => "1/2 TANK",
            3 => "3/4 TANK",
            _ => "FULL"
        };
}

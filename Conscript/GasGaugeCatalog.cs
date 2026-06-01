using System.Numerics;
using Raylib_cs;

namespace Conscript;

/// <summary>Fuel gauge levels and layout for the delivery truck cab viewer.</summary>
internal static class GasGaugeCatalog
{
    /// <summary>Completely empty (E).</summary>
    public const float EmptyFuel = 0f;

    /// <summary>Just above empty when Boris hands over the truck.</summary>
    public const float AlmostEmptyFuel = 0.06f;

    public const float PlateOverhang = 12f;
    public const float HubRadius = 16f;

    /// <summary>Vertical extent of the drawn gauge from pivot (plate above, hub below).</summary>
    public static float GetVisualHeight(float radius) => radius + PlateOverhang + HubRadius;

    public static void DrawInRect(Rectangle rect, float fuel, Font labelFont)
    {
        float radius = rect.Width * 0.5f - 14f;
        float pivotY = rect.Y + rect.Height * 0.5f + (radius - 4f) * 0.5f;
        var pivot = new Vector2(rect.X + rect.Width * 0.5f, pivotY);
        GasGauge.Draw(pivot, radius, Math.Clamp(fuel, 0f, 1f), labelFont);
    }

    public static string GetLevelLabel(float fuel)
    {
        fuel = Math.Clamp(fuel, 0f, 1f);
        if (fuel <= 0.001f)
            return "EMPTY";

        if (fuel < 0.125f)
            return "NEAR EMPTY";

        if (fuel < 0.375f)
            return "1/4 TANK";

        if (fuel < 0.625f)
            return "1/2 TANK";

        if (fuel < 0.875f)
            return "3/4 TANK";

        return "FULL";
    }
}

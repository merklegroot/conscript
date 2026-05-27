using System.Numerics;
using Raylib_cs;

namespace Conscript;

/// <summary>Geographic bounds for region-map.png — sync with img/region-map.bounds.json.</summary>
internal static class RegionMapGeo
{
    public const double MinLon = 22.0;
    public const double MaxLon = 175.0;
    public const double MinLat = 26.0;
    public const double MaxLat = 82.0;

    public const double UlanUdeLon = 107.584;
    public const double UlanUdeLat = 51.834;
    public const double ForestCampLon = 107.35;
    public const double ForestCampLat = 51.95;
    public const double ForestStreamLon = 107.32;
    public const double ForestStreamLat = 51.97;

    public static float LonLatAspect =>
        (float)((MaxLon - MinLon) / (MaxLat - MinLat));

    /// <summary>Math.Clamp throws when min &gt; max due to floating-point error at full zoom.</summary>
    public static double SafeClamp(double value, double min, double max) =>
        min >= max ? (min + max) / 2 : Math.Clamp(value, min, max);

    public static Vector2 LonLatToPixel(
        Rectangle mapRect,
        double lon,
        double lat,
        double viewMinLon = MinLon,
        double viewMaxLon = MaxLon,
        double viewMinLat = MinLat,
        double viewMaxLat = MaxLat)
    {
        double nx = (lon - viewMinLon) / (viewMaxLon - viewMinLon);
        double ny = (viewMaxLat - lat) / (viewMaxLat - viewMinLat);
        nx = Math.Clamp(nx, 0, 1);
        ny = Math.Clamp(ny, 0, 1);
        return new Vector2(mapRect.X + (float)(nx * mapRect.Width), mapRect.Y + (float)(ny * mapRect.Height));
    }
}

using System.Numerics;
using Raylib_cs;

namespace Conscript;

internal static class SceneRegion
{
    public static Rectangle ToScreenRect(float nx, float ny, float nw, float nh, Rectangle artBounds) =>
        new(
            artBounds.X + artBounds.Width * nx,
            artBounds.Y + artBounds.Height * ny,
            artBounds.Width * nw,
            artBounds.Height * nh);
}

using System.Numerics;
using Raylib_cs;

namespace Conscript;

internal static class ItemIconDrawing
{
    /// <summary>
    /// Partial-use items: dim the drained portion, show the remaining slice at full strength,
    /// then tint used (red) and remaining (green) on top of the icon.
    /// </summary>
    public static void DrawPartialCharge(Texture2D tex, Rectangle dest, Color tint, int remaining, int maxCharges)
    {
        float remainFrac = remaining / (float)maxCharges;
        float usedFrac = 1f - remainFrac;

        Rectangle fullSrc = new(0, 0, tex.Width, tex.Height);
        var dimmed = new Color(
            (byte)(tint.R * 0.45f),
            (byte)(tint.G * 0.45f),
            (byte)(tint.B * 0.45f),
            (byte)(tint.A * 0.85f));
        Raylib.DrawTexturePro(tex, fullSrc, dest, Vector2.Zero, 0f, dimmed);

        if (remainFrac > 0.001f)
        {
            float srcH = tex.Height * remainFrac;
            var srcRemain = new Rectangle(0, tex.Height - srcH, tex.Width, srcH);
            float destH = dest.Height * remainFrac;
            var destRemain = new Rectangle(dest.X, dest.Y + dest.Height - destH, dest.Width, destH);
            Raylib.DrawTexturePro(tex, srcRemain, destRemain, Vector2.Zero, 0f, tint);
            Raylib.DrawRectangle((int)destRemain.X, (int)destRemain.Y, (int)destRemain.Width, (int)destRemain.Height,
                new Color(48, 108, 58, 72));
        }

        if (usedFrac > 0.001f)
        {
            int usedH = (int)(dest.Height * usedFrac);
            Raylib.DrawRectangle((int)dest.X, (int)dest.Y, (int)dest.Width, usedH,
                new Color(128, 52, 52, 95));
        }
    }
}

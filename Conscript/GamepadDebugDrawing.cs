using System.Numerics;
using Conscript.Constants;
using Raylib_cs;

namespace Conscript;

internal static class GamepadDebugDrawing
{
    public static void DrawStick(int cx, int cy, int radius, float axisX, float axisY, Color color)
    {
        Raylib.DrawCircleLines(cx, cy, radius, Palette.SubtleBorder);
        Raylib.DrawLine(cx - radius, cy, cx + radius, cy, Palette.SubtleBorder);
        Raylib.DrawLine(cx, cy - radius, cx, cy + radius, Palette.SubtleBorder);

        float px = cx + axisX * (radius - 4);
        float py = cy + axisY * (radius - 4);
        Raylib.DrawCircleV(new Vector2(px, py), 7f, color);
    }

    public static void DrawAxisBar(int x, int y, int width, int height, float value)
    {
        value = Math.Clamp(value, -1f, 1f);
        Raylib.DrawRectangle(x, y, width, height, new Color(18, 20, 24, 255));
        int mid = x + width / 2;
        int half = width / 2 - 2;
        int fill = (int)(Math.Abs(value) * half);
        if (fill < 1 && Math.Abs(value) > 0.02f)
            fill = 1;

        Color fillColor = value >= 0
            ? new Color(90, 130, 150, 255)
            : new Color(150, 110, 90, 255);

        if (value >= 0)
            Raylib.DrawRectangle(mid, y + 1, fill, height - 2, fillColor);
        else
            Raylib.DrawRectangle(mid - fill, y + 1, fill, height - 2, fillColor);

        Raylib.DrawRectangle(mid, y, 1, height, Palette.TextDim);
    }

    public static void DrawTruncatedLine(Font font, string text, int x, ref int y, int maxWidth, int fontSize, Color color)
    {
        if (string.IsNullOrEmpty(text))
            return;

        string line = text;
        while (line.Length > 1 && Raylib.MeasureTextEx(font, line, fontSize, 0.4f).X > maxWidth)
            line = line[..^1];

        Raylib.DrawTextEx(font, line, new Vector2(x, y), fontSize, 0.4f, color);
        y += fontSize + 4;
    }
}

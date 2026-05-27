using System.Numerics;
using Conscript.Constants;
using Raylib_cs;

namespace Conscript;

internal static class GameDialogUi
{
    private static readonly Color ToolbarBgActive = new(58, 63, 74, 255);
    private static readonly Color ToolbarBgIdle = new(32, 35, 42, 255);
    private static readonly Color ToolbarBorderActive = new(125, 130, 140, 255);

    public static void DrawDialogButton(Rectangle rect, string label, bool hovered, Font font)
    {
        Color btnBg = hovered ? Palette.ButtonSelectedBg : Palette.ButtonBg;
        Color btnBorder = hovered ? Palette.ButtonSelectedBorder : Palette.ButtonBorder;

        Raylib.DrawRectangleRec(rect, btnBg);
        Raylib.DrawRectangleLinesEx(rect, 1.5f, btnBorder);
        Raylib.DrawRectangle((int)rect.X + 2, (int)rect.Y + 2, (int)rect.Width - 4, 2, Palette.ButtonTopAccent);

        float labelSize = LayoutConstants.DialogButtonFontSize;
        Vector2 labelSizeVec = Raylib.MeasureTextEx(font, label, labelSize, 0.7f);
        float tx = rect.X + (rect.Width - labelSizeVec.X) / 2f;
        float ty = rect.Y + (rect.Height - labelSizeVec.Y) / 2f - 1f;
        Raylib.DrawTextEx(font, label, new Vector2(tx, ty),
            labelSize, 0.7f, Palette.TextPrimary);
    }

    public static void DrawInfoIcon(Font font, Rectangle rect, bool hovered)
    {
        float cx = rect.X + rect.Width / 2f;
        float cy = rect.Y + rect.Height / 2f;
        float radius = rect.Width / 2f;
        Color fill = hovered ? Palette.ButtonSelectedBg : new Color(28, 30, 36, 255);
        Color border = hovered ? Palette.ButtonSelectedBorder : Palette.TextDim;
        Raylib.DrawCircleV(new Vector2(cx, cy), radius, fill);
        Raylib.DrawCircleLines((int)cx, (int)cy, radius, border);
        const float labelSize = 11f;
        const string label = "i";
        Vector2 size = Raylib.MeasureTextEx(font, label, labelSize, 0.5f);
        Color textColor = hovered ? Palette.TextPrimary : Palette.TextSecondary;
        Raylib.DrawTextEx(font, label,
            new Vector2(cx - size.X / 2f, cy - size.Y / 2f - 1f),
            labelSize, 0.5f, textColor);
    }

    public static void DrawToolbarIconButton(Rectangle rect, bool active, Action<float, float, float, Color> drawIcon)
    {
        if (rect.Width <= 0)
            return;

        Color bg = active ? ToolbarBgActive : ToolbarBgIdle;
        Color border = active ? ToolbarBorderActive : Palette.SubtleBorder;

        Raylib.DrawRectangleRec(rect, bg);
        Raylib.DrawRectangleLinesEx(rect, 1.0f, border);

        Color iconColor = active ? Palette.TextPrimary : Palette.TextSecondary;
        float cx = rect.X + rect.Width / 2f;
        float cy = rect.Y + rect.Height / 2f;
        float iconSize = rect.Width * 0.72f;
        drawIcon(cx, cy, iconSize, iconColor);
    }

    public static void DrawToolbarTextButton(Rectangle rect, bool active, Font font, string label, float labelSize)
    {
        if (rect.Width <= 0)
            return;

        Color bg = active ? ToolbarBgActive : ToolbarBgIdle;
        Color border = active ? ToolbarBorderActive : Palette.SubtleBorder;

        Raylib.DrawRectangleRec(rect, bg);
        Raylib.DrawRectangleLinesEx(rect, 1.0f, border);

        Vector2 m = Raylib.MeasureTextEx(font, label, labelSize, 0.5f);
        float lx = rect.X + (rect.Width - m.X) / 2f;
        float ly = rect.Y + (rect.Height - labelSize) / 2f - 0.5f;
        Raylib.DrawTextEx(font, label, new Vector2(lx, ly), labelSize, 0.5f, Palette.TextSecondary);
    }
}

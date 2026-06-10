using System.Numerics;
using Conscript.Constants;
using Raylib_cs;

namespace Conscript;

internal static class DebugRoomListDrawing
{
    public readonly struct Layout
    {
        public Rectangle[] RowRects { get; init; }
    }

    public static Layout ComputeLayout(int artX, int artY, int artW, int artH)
    {
        const int pad = 18;
        const int headerH = 42;
        const int rowGap = 6;
        const int colGap = 10;

        int listX = artX + pad;
        int listY = artY + pad + headerH;
        int listW = artW - pad * 2;
        int listH = artH - pad * 2 - headerH;

        int columnCount = 2;
        int rowCount = (DebugRoomCatalog.Rooms.Length + columnCount - 1) / columnCount;
        int colW = (listW - colGap) / columnCount;
        int rowH = rowCount > 0
            ? Math.Clamp((listH - (rowCount - 1) * rowGap) / rowCount, 24, 34)
            : 30;

        var rowRects = new Rectangle[DebugRoomCatalog.Rooms.Length];
        for (int i = 0; i < DebugRoomCatalog.Rooms.Length; i++)
        {
            int col = i / rowCount;
            int row = i % rowCount;
            int x = listX + col * (colW + colGap);
            int y = listY + row * (rowH + rowGap);
            rowRects[i] = new Rectangle(x, y, colW, rowH);
        }

        return new Layout { RowRects = rowRects };
    }

    public static void Draw(
        int artX,
        int artY,
        int artW,
        int artH,
        Font font,
        Game.Phase currentPhase,
        Layout layout,
        int hoveredIndex)
    {
        Raylib.DrawRectangle(artX, artY, artW, artH, new Color(14, 16, 20, 255));

        const int pad = 18;
        Raylib.DrawTextEx(font, "DEBUG — ROOMS",
            new Vector2(artX + pad, artY + pad + 4),
            16f, 0.65f, Palette.TextPrimary);
        Raylib.DrawTextEx(font, "Click a room to teleport. Toggle RM to return to the scene.",
            new Vector2(artX + pad, artY + pad + 24),
            11f, 0.45f, Palette.TextMuted);

        for (int i = 0; i < DebugRoomCatalog.Rooms.Length; i++)
        {
            DebugRoomEntry entry = DebugRoomCatalog.Rooms[i];
            Rectangle rect = layout.RowRects[i];
            bool isCurrent = entry.Phase == currentPhase;
            bool hovered = i == hoveredIndex;

            Color bg = isCurrent
                ? new Color(56, 72, 48, 255)
                : hovered
                    ? new Color(42, 40, 36, 255)
                    : new Color(28, 26, 24, 255);
            Color border = isCurrent
                ? Palette.ActionFlash
                : hovered
                    ? Palette.TextSecondary
                    : Palette.SubtleBorder;

            Raylib.DrawRectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, bg);
            Raylib.DrawRectangleLines((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, border);

            string phaseLabel = entry.Phase.ToString();
            float phaseSize = 10f;
            Vector2 phaseSizeVec = Raylib.MeasureTextEx(font, phaseLabel, phaseSize, 0.35f);
            Raylib.DrawTextEx(font, phaseLabel,
                new Vector2(rect.X + 8, rect.Y + 5),
                phaseSize, 0.35f, isCurrent ? Palette.TextPrimary : Palette.TextSecondary);

            string nameLabel = entry.DisplayName;
            float nameSize = 9f;
            float nameX = rect.X + 8;
            float nameY = rect.Y + 5 + phaseSizeVec.Y + 1;
            float maxNameW = rect.Width - 16;
            while (nameLabel.Length > 3 && Raylib.MeasureTextEx(font, nameLabel, nameSize, 0.3f).X > maxNameW)
                nameLabel = nameLabel[..^1];
            if (nameLabel.Length < entry.DisplayName.Length)
                nameLabel = nameLabel.TrimEnd() + "…";

            Raylib.DrawTextEx(font, nameLabel,
                new Vector2(nameX, nameY),
                nameSize, 0.3f, Palette.TextMuted);
        }

        Raylib.DrawRectangleLines(artX + 2, artY + 2, artW - 4, artH - 4, Palette.SubtleBorder);
    }
}

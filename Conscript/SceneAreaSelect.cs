using System.Numerics;
using Conscript.Constants;
using Raylib_cs;

namespace Conscript;

internal sealed class SceneAreaSelect
{
    private const float MinSelectionSize = 6f;

    private bool _dragging;
    private Vector2 _start;
    private Vector2 _current;

    public bool IsActive { get; private set; }

    public bool IsDragging => _dragging;

    public bool SelectionTooSmall { get; private set; }

    public void Open()
    {
        IsActive = true;
        _dragging = false;
    }

    public void Close()
    {
        IsActive = false;
        _dragging = false;
    }

    public SceneAreaSelection? Update(
        Vector2 mouse,
        Rectangle artBounds,
        Game.Phase phase,
        int textureWidth,
        int textureHeight,
        bool leftPressed,
        bool leftReleased)
    {
        SelectionTooSmall = false;

        if (!IsActive)
            return null;

        if (leftPressed && Raylib.CheckCollisionPointRec(mouse, artBounds))
        {
            _dragging = true;
            _start = ClampToBounds(mouse, artBounds);
            _current = _start;
            return null;
        }

        if (_dragging)
        {
            _current = ClampToBounds(mouse, artBounds);

            if (leftReleased)
            {
                _dragging = false;
                SceneAreaSelection? selection = BuildSelection(artBounds, phase, textureWidth, textureHeight);
                if (selection.HasValue)
                {
                    Close();
                    return selection;
                }

                Rectangle screenRect = NormalizeDragRect(_start, _current);
                if (screenRect.Width > 1f || screenRect.Height > 1f)
                    SelectionTooSmall = true;

                return null;
            }
        }

        return null;
    }

    public void Draw(Font font, Rectangle artBounds)
    {
        if (!IsActive)
            return;

        Raylib.DrawRectangleRec(artBounds, new Color(0, 0, 0, 90));

        if (_dragging || HasPendingSelection())
        {
            Rectangle screenRect = NormalizeDragRect(_start, _current);
            Raylib.DrawRectangle(
                (int)screenRect.X,
                (int)screenRect.Y,
                (int)screenRect.Width,
                (int)screenRect.Height,
                new Color(200, 185, 120, 45));
            Raylib.DrawRectangleLinesEx(screenRect, 2f, Palette.ActionFlash);
        }

        const string hint = "Drag on the background to select a region. Esc to cancel.";
        const float hintSize = 15f;
        Vector2 hintMeasure = Raylib.MeasureTextEx(font, hint, hintSize, 0.55f);
        float hintX = artBounds.X + (artBounds.Width - hintMeasure.X) / 2f;
        float hintY = artBounds.Y + 10f;
        var hintBg = new Rectangle(hintX - 10f, hintY - 4f, hintMeasure.X + 20f, hintMeasure.Y + 8f);
        Raylib.DrawRectangleRec(hintBg, new Color(10, 12, 16, 210));
        Raylib.DrawRectangleLinesEx(hintBg, 1f, Palette.SubtleBorder);
        Raylib.DrawTextEx(font, hint, new Vector2(hintX, hintY), hintSize, 0.55f, Palette.TextPrimary);
    }

    private SceneAreaSelection? BuildSelection(
        Rectangle artBounds,
        Game.Phase phase,
        int textureWidth,
        int textureHeight)
    {
        Rectangle screenRect = NormalizeDragRect(_start, _current);
        if (screenRect.Width < MinSelectionSize || screenRect.Height < MinSelectionSize)
            return null;

        float nx = (screenRect.X - artBounds.X) / artBounds.Width;
        float ny = (screenRect.Y - artBounds.Y) / artBounds.Height;
        float nw = screenRect.Width / artBounds.Width;
        float nh = screenRect.Height / artBounds.Height;

        nx = Math.Clamp(nx, 0f, 1f);
        ny = Math.Clamp(ny, 0f, 1f);
        nw = Math.Clamp(nw, 0f, 1f - nx);
        nh = Math.Clamp(nh, 0f, 1f - ny);

        int px = (int)MathF.Round(nx * textureWidth);
        int py = (int)MathF.Round(ny * textureHeight);
        int pw = Math.Max(1, (int)MathF.Round(nw * textureWidth));
        int ph = Math.Max(1, (int)MathF.Round(nh * textureHeight));

        string imageFile = SceneBackgroundFiles.GetImageFile(phase);
        string clipboard =
            $"{phase}: x={nx:F3}, y={ny:F3}, w={nw:F3}, h={nh:F3} " +
            $"({imageFile} {textureWidth}x{textureHeight}, px x={px} y={py} w={pw} h={ph})";
        string display =
            $"Region x={nx:F3} y={ny:F3} w={nw:F3} h={nh:F3} — copied to clipboard";

        return new SceneAreaSelection(nx, ny, nw, nh, px, py, pw, ph, clipboard, display);
    }

    private bool HasPendingSelection()
    {
        Rectangle screenRect = NormalizeDragRect(_start, _current);
        return screenRect.Width >= MinSelectionSize && screenRect.Height >= MinSelectionSize;
    }

    private static Vector2 ClampToBounds(Vector2 point, Rectangle bounds)
    {
        float x = Math.Clamp(point.X, bounds.X, bounds.X + bounds.Width);
        float y = Math.Clamp(point.Y, bounds.Y, bounds.Y + bounds.Height);
        return new Vector2(x, y);
    }

    private static Rectangle NormalizeDragRect(Vector2 a, Vector2 b)
    {
        float x = MathF.Min(a.X, b.X);
        float y = MathF.Min(a.Y, b.Y);
        float w = MathF.Abs(a.X - b.X);
        float h = MathF.Abs(a.Y - b.Y);
        return new Rectangle(x, y, w, h);
    }
}

internal readonly record struct SceneAreaSelection(
    float X,
    float Y,
    float W,
    float H,
    int Px,
    int Py,
    int Pw,
    int Ph,
    string ClipboardText,
    string DisplayMessage);

internal static class SceneBackgroundFiles
{
    public static string GetImageFile(Game.Phase phase) =>
        phase switch
        {
            Game.Phase.Opening => "apartment-inside.png",
            Game.Phase.Outside => "apartment-outside.png",
            Game.Phase.Town => "town.png",
            Game.Phase.IndustrialDistrict => "industrial.png",
            Game.Phase.CommercialDistrict => "commercial.png",
            Game.Phase.Store => "store.png",
            Game.Phase.Cafe => "cafe.png",
            Game.Phase.DeliveryTruck => "delivery-truck-cab.png",
            Game.Phase.WarehouseTruck => "warehouse-14.png",
            Game.Phase.WarehouseAmbush => "warehouse-14-ambush.png",
            Game.Phase.WarehouseAftermath => "warehouse-14-aftermath.png",
            Game.Phase.ForestEntry => "forest-entry.png",
            Game.Phase.ForestStream => "forest-stream.png",
            Game.Phase.Forest => "trees.png",
            Game.Phase.Tent => "tent-interior.png",
            _ => "unknown.png",
        };
}

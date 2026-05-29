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

    public bool SelectionTooSmall { get; private set; }

    public void Open()
    {
        IsActive = true;
        ResetDrag();
    }

    public void Close()
    {
        IsActive = false;
        ResetDrag();
    }

    public SceneAreaSelection? Update(
        Vector2 mouse,
        Rectangle artBounds,
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
                SceneAreaSelection? selection = BuildSelection(artBounds);
                if (selection.HasValue)
                {
                    Close();
                    return selection;
                }

                Rectangle screenRect = NormalizeDragRect(_start, _current);
                if (screenRect.Width > 1f || screenRect.Height > 1f)
                    SelectionTooSmall = true;

                ResetDrag();
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

        if (_dragging)
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

    private SceneAreaSelection? BuildSelection(Rectangle artBounds)
    {
        Rectangle screenRect = NormalizeDragRect(_start, _current);
        if (screenRect.Width < MinSelectionSize || screenRect.Height < MinSelectionSize)
            return null;

        float x1 = Math.Clamp((screenRect.X - artBounds.X) / artBounds.Width, 0f, 1f);
        float y1 = Math.Clamp((screenRect.Y - artBounds.Y) / artBounds.Height, 0f, 1f);
        float x2 = Math.Clamp((screenRect.X + screenRect.Width - artBounds.X) / artBounds.Width, 0f, 1f);
        float y2 = Math.Clamp((screenRect.Y + screenRect.Height - artBounds.Y) / artBounds.Height, 0f, 1f);

        string text = $"({x1:F3}, {y1:F3}), ({x2:F3}, {y2:F3})";
        return new SceneAreaSelection(x1, y1, x2, y2, text, $"{text} — copied to clipboard");
    }

    private void ResetDrag()
    {
        _dragging = false;
        _start = Vector2.Zero;
        _current = Vector2.Zero;
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
    float X1,
    float Y1,
    float X2,
    float Y2,
    string ClipboardText,
    string DisplayMessage);

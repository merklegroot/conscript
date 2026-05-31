using System.Numerics;
using Conscript.Constants;
using Raylib_cs;

namespace Conscript;

internal sealed class GasGaugeViewerDialog
{
    private Rectangle _panelRect;
    private Rectangle _faceRect;
    private Rectangle _closeRect;
    private Rectangle _prevRect;
    private Rectangle _nextRect;
    private bool _closeHovered;
    private bool _prevHovered;
    private bool _nextHovered;
    private bool _hasPivotMarker;
    private Vector2 _pivotMarkerNormalized;

    public bool IsOpen { get; private set; }

    public void Open()
    {
        IsOpen = true;
        _closeHovered = false;
        _prevHovered = false;
        _nextHovered = false;
        _hasPivotMarker = false;
    }

    public void Close()
    {
        IsOpen = false;
        _closeHovered = false;
        _prevHovered = false;
        _nextHovered = false;
        _hasPivotMarker = false;
    }

    public void Update(Vector2 mouse, bool leftClicked, ref int level, Action? onLevelChanged)
    {
        if (!IsOpen)
            return;

        _closeHovered = Raylib.CheckCollisionPointRec(mouse, _closeRect);
        _prevHovered = Raylib.CheckCollisionPointRec(mouse, _prevRect);
        _nextHovered = Raylib.CheckCollisionPointRec(mouse, _nextRect);

        if (!leftClicked)
            return;

        if (_closeHovered)
        {
            Close();
            return;
        }

        if (_prevHovered)
        {
            if (level > 0)
            {
                level--;
                onLevelChanged?.Invoke();
            }

            return;
        }

        if (_nextHovered)
        {
            if (level < GasGaugeCatalog.LevelCount - 1)
            {
                level++;
                onLevelChanged?.Invoke();
            }

            return;
        }

        if (Raylib.CheckCollisionPointRec(mouse, _faceRect))
        {
            _pivotMarkerNormalized = new Vector2(
                (mouse.X - _faceRect.X) / _faceRect.Width,
                (mouse.Y - _faceRect.Y) / _faceRect.Height);
            _hasPivotMarker = true;
            Raylib.SetClipboardText(
                $"({_pivotMarkerNormalized.X:F3}, {_pivotMarkerNormalized.Y:F3})");
            return;
        }

        if (!Raylib.CheckCollisionPointRec(mouse, _panelRect))
            Close();
    }

    public void Draw(Font font, Texture2D faceTexture, Texture2D needleTexture, int level, int screenWidth, int screenHeight)
    {
        if (!IsOpen || faceTexture.Id == 0)
            return;

        GameDialogUi.DrawModalBackdrop(screenWidth, screenHeight, alpha: 200);

        const int margin = 28;
        int maxPanelW = Math.Min(420, screenWidth - margin * 2);
        int maxFaceSize = maxPanelW - 32;

        int faceSize = maxFaceSize;
        int panelW = faceSize + 32;
        int panelH = faceSize + 156;
        int panelX = (screenWidth - panelW) / 2;
        int panelY = (screenHeight - panelH) / 2 - 8;
        _panelRect = new Rectangle(panelX, panelY, panelW, panelH);

        Raylib.DrawRectangle(panelX, panelY, panelW, panelH, Palette.CardBg);
        Raylib.DrawRectangleLines(panelX, panelY, panelW, panelH, Palette.CardBorder);

        Raylib.DrawTextEx(font, "FUEL GAUGE",
            new Vector2(panelX + 20, panelY + 14), 22, 0.7f, Palette.TextPrimary);

        const string pivotHint = "Click gauge face to mark pivot (copies coords)";
        int hintSize = 12;
        Raylib.DrawTextEx(font, pivotHint,
            new Vector2(panelX + 20, panelY + 36), hintSize, 0.45f, Palette.TextSecondary);

        int faceX = panelX + (panelW - faceSize) / 2;
        int faceY = panelY + 52;
        _faceRect = new Rectangle(faceX, faceY, faceSize, faceSize);

        Raylib.DrawRectangleRec(_faceRect, new Color(8, 8, 10, 255));

        var faceSrc = new Rectangle(0, 0, faceTexture.Width, faceTexture.Height);
        Raylib.DrawTexturePro(faceTexture, faceSrc, _faceRect, Vector2.Zero, 0f, Color.WHITE);

        if (needleTexture.Id != 0)
        {
            GetActiveHubRatios(out float hubXRatio, out float hubYRatio);
            GasGaugeCatalog.DrawNeedle(needleTexture, _faceRect, level, hubXRatio, hubYRatio);
        }

        DrawNeedleHubMarker();
        if (_hasPivotMarker)
            DrawPivotReticle(font);

        string levelLabel = GasGaugeCatalog.GetLevelLabel(level);
        int labelSize = 16;
        int labelW = (int)Raylib.MeasureTextEx(font, levelLabel, labelSize, 0.55f).X;
        Raylib.DrawTextEx(font, levelLabel,
            new Vector2(panelX + (panelW - labelW) / 2, faceY + faceSize + 10),
            labelSize, 0.55f, Palette.TextSecondary);

        int btnH = 36;
        int btnW = 52;
        int btnY = panelY + panelH - btnH - 14;
        int gap = 10;
        int closeW = 100;
        int rowW = btnW * 2 + gap + closeW + gap;
        int rowX = panelX + (panelW - rowW) / 2;

        _prevRect = new Rectangle(rowX, btnY, btnW, btnH);
        _nextRect = new Rectangle(rowX + btnW + gap, btnY, btnW, btnH);
        _closeRect = new Rectangle(rowX + (btnW + gap) * 2, btnY, closeW, btnH);

        GameDialogUi.DrawDialogButton(_prevRect, "◀", _prevHovered, font);
        GameDialogUi.DrawDialogButton(_nextRect, "▶", _nextHovered, font);
        GameDialogUi.DrawDialogButton(_closeRect, "CLOSE", _closeHovered, font);
    }

    private void GetActiveHubRatios(out float hubXRatio, out float hubYRatio)
    {
        if (_hasPivotMarker)
        {
            hubXRatio = _pivotMarkerNormalized.X;
            hubYRatio = _pivotMarkerNormalized.Y;
            return;
        }

        hubXRatio = GasGaugeCatalog.FacePivotXRatio;
        hubYRatio = GasGaugeCatalog.FacePivotYRatio;
    }

    private void DrawNeedleHubMarker()
    {
        GetActiveHubRatios(out float hubXRatio, out float hubYRatio);
        float x = _faceRect.X + _faceRect.Width * hubXRatio;
        float y = _faceRect.Y + _faceRect.Height * hubYRatio;
        var hubColor = new Color(255, 140, 60, 220);

        Raylib.DrawCircleLines((int)x, (int)y, 6f, hubColor);
        Raylib.DrawCircle((int)x, (int)y, 2f, hubColor);
    }

    private void DrawPivotReticle(Font font)
    {
        float x = _faceRect.X + _pivotMarkerNormalized.X * _faceRect.Width;
        float y = _faceRect.Y + _pivotMarkerNormalized.Y * _faceRect.Height;
        const float arm = 14f;
        var reticleColor = new Color(120, 220, 255, 255);
        var ringColor = new Color(120, 220, 255, 180);

        Raylib.DrawLineEx(new Vector2(x - arm, y), new Vector2(x + arm, y), 2f, reticleColor);
        Raylib.DrawLineEx(new Vector2(x, y - arm), new Vector2(x, y + arm), 2f, reticleColor);
        Raylib.DrawCircleLines((int)x, (int)y, 8f, ringColor);
        Raylib.DrawCircle((int)x, (int)y, 2f, reticleColor);

        string coords = $"({_pivotMarkerNormalized.X:F3}, {_pivotMarkerNormalized.Y:F3}) — copied";
        int coordSize = 14;
        Vector2 coordMeasure = Raylib.MeasureTextEx(font, coords, coordSize, 0.5f);
        float coordX = x - coordMeasure.X / 2f;
        float coordY = y + arm + 6f;
        if (coordY + coordMeasure.Y > _faceRect.Y + _faceRect.Height - 4f)
            coordY = y - arm - coordMeasure.Y - 6f;

        var bg = new Rectangle(coordX - 6f, coordY - 2f, coordMeasure.X + 12f, coordMeasure.Y + 4f);
        Raylib.DrawRectangleRec(bg, new Color(8, 10, 14, 220));
        Raylib.DrawRectangleLinesEx(bg, 1f, ringColor);
        Raylib.DrawTextEx(font, coords, new Vector2(coordX, coordY), coordSize, 0.5f, Palette.TextPrimary);
    }
}

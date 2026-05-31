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

    public bool IsOpen { get; private set; }

    public void Open()
    {
        IsOpen = true;
        _closeHovered = false;
        _prevHovered = false;
        _nextHovered = false;
    }

    public void Close()
    {
        IsOpen = false;
        _closeHovered = false;
        _prevHovered = false;
        _nextHovered = false;
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
        int panelH = faceSize + 148;
        int panelX = (screenWidth - panelW) / 2;
        int panelY = (screenHeight - panelH) / 2 - 8;
        _panelRect = new Rectangle(panelX, panelY, panelW, panelH);

        Raylib.DrawRectangle(panelX, panelY, panelW, panelH, Palette.CardBg);
        Raylib.DrawRectangleLines(panelX, panelY, panelW, panelH, Palette.CardBorder);

        Raylib.DrawTextEx(font, "FUEL GAUGE",
            new Vector2(panelX + 20, panelY + 14), 22, 0.7f, Palette.TextPrimary);

        int faceX = panelX + (panelW - faceSize) / 2;
        int faceY = panelY + 44;
        _faceRect = new Rectangle(faceX, faceY, faceSize, faceSize);

        FlushDrawBatch();

        DrawGaugeFace(faceTexture);
        FlushDrawBatch();

        DrawGaugeNeedle(needleTexture, level);
        FlushDrawBatch();

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

    private static void FlushDrawBatch() => Rlgl.DrawRenderBatchActive();

    private void DrawGaugeFace(Texture2D faceTexture)
    {
        Raylib.DrawRectangleRec(_faceRect, new Color(8, 8, 10, 255));

        var faceSrc = new Rectangle(0, 0, faceTexture.Width, faceTexture.Height);
        Raylib.DrawTexturePro(faceTexture, faceSrc, _faceRect, Vector2.Zero, 0f, Color.WHITE);
    }

    private void DrawGaugeNeedle(Texture2D needleTexture, int level)
    {
        if (needleTexture.Id == 0)
            return;

        GasGaugeCatalog.DrawNeedle(needleTexture, _faceRect, level);
    }
}

using System.Numerics;
using Conscript.Constants;
using Raylib_cs;

namespace Conscript;

internal sealed class GasGaugeViewerDialog
{
    private Rectangle _panelRect;
    private Rectangle _faceRect;
    private Rectangle _closeRect;
    private bool _closeHovered;

    public bool IsOpen { get; private set; }

    public void Open()
    {
        IsOpen = true;
        _closeHovered = false;
    }

    public void Close()
    {
        IsOpen = false;
        _closeHovered = false;
    }

    public void Update(Vector2 mouse, bool leftClicked)
    {
        if (!IsOpen)
            return;

        _closeHovered = Raylib.CheckCollisionPointRec(mouse, _closeRect);

        if (!leftClicked)
            return;

        if (_closeHovered)
        {
            Close();
            return;
        }

        if (!Raylib.CheckCollisionPointRec(mouse, _panelRect))
            Close();
    }

    public void Draw(Font font, float fuel, int screenWidth, int screenHeight)
    {
        if (!IsOpen)
            return;

        GameDialogUi.DrawModalBackdrop(screenWidth, screenHeight, alpha: 200);

        const int margin = 28;
        int maxPanelW = Math.Min(420, screenWidth - margin * 2);
        int maxFaceSize = maxPanelW - 32;

        const int headerH = 42;
        const int btnH = 36;
        const int btnPad = 14;
        const int labelSize = 16;
        const int labelGap = 10;

        int faceSize = maxFaceSize;
        int panelW = faceSize + 32;
        int panelH = faceSize + 108;
        int panelX = (screenWidth - panelW) / 2;
        int panelY = (screenHeight - panelH) / 2 - 8;
        _panelRect = new Rectangle(panelX, panelY, panelW, panelH);

        int btnY = panelY + panelH - btnH - btnPad;
        int contentTop = panelY + headerH;
        int contentBottom = btnY - labelGap - labelSize - labelGap;
        float contentMid = (contentTop + contentBottom) * 0.5f;

        float radius = faceSize * 0.5f - 14f;
        float pivotY = contentMid + (radius - 4f) * 0.5f;
        float visualTop = pivotY - radius - GasGaugeCatalog.PlateOverhang;
        float visualBottom = pivotY + GasGaugeCatalog.HubRadius;
        int gaugePad = 4;
        int gaugeY = (int)visualTop - gaugePad;
        int gaugeH = (int)Math.Ceiling(visualBottom - visualTop) + gaugePad * 2;

        Raylib.DrawRectangle(panelX, panelY, panelW, panelH, Palette.CardBg);
        Raylib.DrawRectangleLines(panelX, panelY, panelW, panelH, Palette.CardBorder);

        Raylib.DrawTextEx(font, "FUEL GAUGE",
            new Vector2(panelX + 20, panelY + 14), 22, 0.7f, Palette.TextPrimary);

        int faceX = panelX + (panelW - faceSize) / 2;
        _faceRect = new Rectangle(faceX, gaugeY, faceSize, gaugeH);

        Raylib.DrawRectangleRec(_faceRect, new Color(8, 8, 10, 255));
        GasGaugeCatalog.DrawInRect(_faceRect, fuel, font);

        string levelLabel = GasGaugeCatalog.GetLevelLabel(fuel);
        int labelW = (int)Raylib.MeasureTextEx(font, levelLabel, labelSize, 0.55f).X;
        Raylib.DrawTextEx(font, levelLabel,
            new Vector2(panelX + (panelW - labelW) / 2, contentBottom),
            labelSize, 0.55f, Palette.TextSecondary);

        const int closeW = 100;
        int closeX = panelX + (panelW - closeW) / 2;
        _closeRect = new Rectangle(closeX, btnY, closeW, btnH);

        GameDialogUi.DrawDialogButton(_closeRect, "CLOSE", _closeHovered, font);
    }
}

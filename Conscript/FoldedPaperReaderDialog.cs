using System.Numerics;
using Conscript.Constants;
using Raylib_cs;

namespace Conscript;

internal sealed class FoldedPaperReaderDialog
{
    private Rectangle _panelRect;
    private Rectangle _imageRect;
    private Rectangle _closeRect;
    private bool _closeHovered;
    private string _title = "FOLDED NOTE";

    public bool IsOpen { get; private set; }

    public void Open(string title = "FOLDED NOTE")
    {
        _title = title;
        IsOpen = true;
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

    public void Draw(Font font, Texture2D paperTexture, int screenWidth, int screenHeight)
    {
        if (!IsOpen || paperTexture.Id == 0)
            return;

        GameDialogUi.DrawModalBackdrop(screenWidth, screenHeight, alpha: 200);

        const int margin = 28;
        int maxPanelW = screenWidth - margin * 2;
        int maxPanelH = screenHeight - margin * 2 - 48;

        float texAspect = paperTexture.Width / (float)paperTexture.Height;
        int imageW = maxPanelW;
        int imageH = (int)(imageW / texAspect);
        if (imageH > maxPanelH)
        {
            imageH = maxPanelH;
            imageW = (int)(imageH * texAspect);
        }

        int panelW = imageW + 32;
        int panelH = imageH + 88;
        int panelX = (screenWidth - panelW) / 2;
        int panelY = (screenHeight - panelH) / 2 - 8;
        _panelRect = new Rectangle(panelX, panelY, panelW, panelH);

        Raylib.DrawRectangle(panelX, panelY, panelW, panelH, Palette.CardBg);
        Raylib.DrawRectangleLines(panelX, panelY, panelW, panelH, Palette.CardBorder);

        Raylib.DrawTextEx(font, _title,
            new Vector2(panelX + 20, panelY + 14), 22, 0.7f, Palette.TextPrimary);

        int imageX = panelX + (panelW - imageW) / 2;
        int imageY = panelY + 44;
        _imageRect = new Rectangle(imageX, imageY, imageW, imageH);

        Raylib.DrawRectangleRec(_imageRect, new Color(12, 11, 10, 255));
        Raylib.DrawRectangleLinesEx(_imageRect, 1f, Palette.SubtleBorder);

        var src = new Rectangle(0, 0, paperTexture.Width, paperTexture.Height);
        Raylib.DrawTexturePro(paperTexture, src, _imageRect, Vector2.Zero, 0f, Color.WHITE);

        int closeH = 32;
        int closeW = 100;
        int closeX = panelX + (panelW - closeW) / 2;
        int closeY = panelY + panelH - closeH - 12;
        _closeRect = new Rectangle(closeX, closeY, closeW, closeH);
        GameDialogUi.DrawDialogButton(_closeRect, "CLOSE", _closeHovered, font);
    }
}

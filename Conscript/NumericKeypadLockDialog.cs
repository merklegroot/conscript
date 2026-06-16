using System.Numerics;
using System.Text;
using Conscript.Constants;
using Raylib_cs;

namespace Conscript;

internal sealed class NumericKeypadLockDialog
{
    private const int KeyClear = 9;
    private const int KeyZero = 10;
    private const int KeyEnter = 11;
    private const int KeyClose = 12;
    private const int KeyCount = 12;

    private readonly string _correctCode;
    private readonly int _maxDigits;
    private readonly StringBuilder _entry = new();
    private readonly Rectangle[] _keyRects = new Rectangle[KeyCount];

    private Rectangle _panelRect;
    private Rectangle _closeRect;
    private int _hoveredKey = -1;
    private string _feedback = "";
    private float _feedbackTimer;

    public bool IsOpen { get; private set; }

    public bool IsUnlocked { get; private set; }

    public NumericKeypadLockDialog(string correctCode, int maxDigits)
    {
        _correctCode = correctCode;
        _maxDigits = maxDigits;
    }

    public void Open()
    {
        if (IsUnlocked)
            return;

        IsOpen = true;
        _entry.Clear();
        _feedback = "";
        _feedbackTimer = 0f;
        _hoveredKey = -1;
    }

    public void Close()
    {
        IsOpen = false;
        _hoveredKey = -1;
    }

    public void Reset()
    {
        IsUnlocked = false;
        Close();
        _entry.Clear();
        _feedback = "";
        _feedbackTimer = 0f;
    }

    public void RestoreUnlockedState(bool unlocked)
    {
        IsUnlocked = unlocked;
        Close();
        _entry.Clear();
        _feedback = "";
        _feedbackTimer = 0f;
    }

    public void Update(float dt, Vector2 mouse, bool leftClicked)
    {
        if (!IsOpen)
            return;

        if (_feedbackTimer > 0f)
        {
            _feedbackTimer -= dt;
            if (_feedbackTimer <= 0f)
                _feedback = "";
        }

        HandleKeyboardInput();
        HandleMouseInput(mouse, leftClicked);
    }

    private void HandleKeyboardInput()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_BACKSPACE))
        {
            if (_entry.Length > 0)
            {
                _entry.Length--;
                _feedback = "";
            }

            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ENTER) || Raylib.IsKeyPressed(KeyboardKey.KEY_KP_ENTER))
        {
            TrySubmit();
            return;
        }

        for (int d = 0; d <= 9; d++)
        {
            char digit = (char)('0' + d);
            if (Raylib.IsKeyPressed((KeyboardKey)((int)KeyboardKey.KEY_ZERO + d)) ||
                Raylib.IsKeyPressed((KeyboardKey)((int)KeyboardKey.KEY_KP_0 + d)))
            {
                AppendDigit(digit);
                return;
            }
        }
    }

    private void HandleMouseInput(Vector2 mouse, bool leftClicked)
    {
        _hoveredKey = -1;
        if (Raylib.CheckCollisionPointRec(mouse, _closeRect))
            _hoveredKey = KeyClose;
        else
        {
            for (int i = 0; i < KeyCount; i++)
            {
                if (_keyRects[i].Width > 0 && Raylib.CheckCollisionPointRec(mouse, _keyRects[i]))
                {
                    _hoveredKey = i;
                    break;
                }
            }
        }

        if (!leftClicked)
            return;

        if (_hoveredKey == KeyClose)
        {
            Close();
            return;
        }

        if (_hoveredKey < 0)
        {
            if (!Raylib.CheckCollisionPointRec(mouse, _panelRect))
                Close();

            return;
        }

        if (_hoveredKey == KeyClear)
        {
            _entry.Clear();
            _feedback = "";
            return;
        }

        if (_hoveredKey == KeyEnter)
        {
            TrySubmit();
            return;
        }

        char digit = _hoveredKey switch
        {
            KeyZero => '0',
            < 9 => (char)('1' + _hoveredKey),
            _ => '\0'
        };

        if (digit != '\0')
            AppendDigit(digit);
    }

    private void AppendDigit(char digit)
    {
        if (digit < '0' || digit > '9' || _entry.Length >= _maxDigits)
            return;

        _entry.Append(digit);
        _feedback = "";

        if (_entry.Length >= _maxDigits)
            TrySubmit();
    }

    public void Draw(Font font, int screenWidth, int screenHeight)
    {
        if (!IsOpen)
            return;

        GameDialogUi.DrawModalBackdrop(screenWidth, screenHeight);

        int panelW = 300;
        int panelH = 420;
        int panelX = (screenWidth - panelW) / 2;
        int panelY = (screenHeight - panelH) / 2 - 12;
        _panelRect = new Rectangle(panelX, panelY, panelW, panelH);

        Raylib.DrawRectangle(panelX, panelY, panelW, panelH, Palette.CardBg);
        Raylib.DrawRectangleLines(panelX, panelY, panelW, panelH, Palette.CardBorder);

        Raylib.DrawTextEx(font, "KEYPAD",
            new Vector2(panelX + 24, panelY + 18), 26, 0.75f, Palette.TextPrimary);

        string hint = "Type the code or use the keypad";
        int hintW = (int)Raylib.MeasureTextEx(font, hint, 16, 0.55f).X;
        Raylib.DrawTextEx(font, hint,
            new Vector2(panelX + (panelW - hintW) / 2, panelY + 52),
            16, 0.55f, Palette.TextSecondary);

        int displayY = panelY + 78;
        string display = FormatEntryDisplay();
        int displaySize = 32;
        int displayW = (int)Raylib.MeasureTextEx(font, display, displaySize, 0.6f).X;
        var displayBg = new Rectangle(panelX + 24, displayY, panelW - 48, 44);
        Raylib.DrawRectangleRec(displayBg, new Color(18, 20, 24, 255));
        Raylib.DrawRectangleLinesEx(displayBg, 1f, Palette.SubtleBorder);
        Raylib.DrawTextEx(font, display,
            new Vector2(panelX + (panelW - displayW) / 2, displayY + 6),
            displaySize, 0.6f, Palette.TextPrimary);

        if (_feedbackTimer > 0f && !string.IsNullOrEmpty(_feedback))
        {
            int fbW = (int)Raylib.MeasureTextEx(font, _feedback, 15, 0.5f).X;
            Raylib.DrawTextEx(font, _feedback,
                new Vector2(panelX + (panelW - fbW) / 2, displayY + 50),
                15, 0.5f, new Color(200, 130, 110, 255));
        }

        int gridTop = panelY + 140;
        int keyGap = 8;
        int keyW = (panelW - 48 - keyGap * 2) / 3;
        int keyH = 44;
        int gridX = panelX + 24;

        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                int keyIndex = row * 3 + col;
                int x = gridX + col * (keyW + keyGap);
                int y = gridTop + row * (keyH + keyGap);
                _keyRects[keyIndex] = new Rectangle(x, y, keyW, keyH);

                string label = keyIndex switch
                {
                    KeyClear => "CLR",
                    KeyZero => "0",
                    KeyEnter => "ENT",
                    _ => ((char)('1' + keyIndex)).ToString()
                };

                bool hovered = _hoveredKey == keyIndex;
                GameDialogUi.DrawDialogButton(_keyRects[keyIndex], label, hovered, font);
            }
        }

        int closeH = 32;
        int closeW = 100;
        int closeY = panelY + panelH - closeH - 16;
        int closeX = panelX + (panelW - closeW) / 2;
        _closeRect = new Rectangle(closeX, closeY, closeW, closeH);
        bool closeHovered = _hoveredKey == KeyClose;
        GameDialogUi.DrawDialogButton(_closeRect, "CLOSE", closeHovered, font);
    }

    private string FormatEntryDisplay()
    {
        var display = new StringBuilder(_maxDigits * 2);
        for (int i = 0; i < _maxDigits; i++)
        {
            if (i > 0)
                display.Append(' ');

            display.Append(i < _entry.Length ? _entry[i] : '_');
        }

        return display.ToString();
    }

    private void TrySubmit()
    {
        if (_entry.Length < _maxDigits)
        {
            _feedback = "Code too short.";
            _feedbackTimer = 1.4f;
            return;
        }

        if (_entry.ToString() != _correctCode)
        {
            _entry.Clear();
            _feedback = "Wrong code.";
            _feedbackTimer = 1.6f;
            return;
        }

        IsUnlocked = true;
        Close();
    }
}

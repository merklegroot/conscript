using System.Numerics;
using Conscript.Constants;
using Raylib_cs;

namespace Conscript;

public interface IGame
{
    void Run();
    bool ShouldExit { get; }
}

public sealed class Game : IGame
{
    private const float ActionMessageDuration = 3.5f;

    private readonly int _screenWidth = GameConstants.ScreenWidth;
    private readonly int _screenHeight = GameConstants.ScreenHeight;

    private bool _shouldExit;
    public bool ShouldExit => _shouldExit;

    // Core stats (0-100)
    private int _suspicion = 4;
    private int _health = 93;
    private int _morale = 77;
    private int _exposure = 7;
    private int _provisions = 52; // shown as "Provisions"

    private int _selectedIndex;
    private readonly string[] _choices =
    {
        "Burn the Letter and Run",
        "Ignore It For Now",
        "Call Family",
        "Pack Essentials and Wait"
    };

    private string _actionMessage = "";
    private float _actionMessageTimer;

    public void Run()
    {
        Raylib.InitWindow(_screenWidth, _screenHeight, "CONSCRIPT");
        Raylib.SetTargetFPS(60);
        Raylib.SetExitKey(KeyboardKey.KEY_NULL); // we handle ESC ourselves

        while (!ShouldExit && !Raylib.WindowShouldClose())
        {
            Update();
            Draw();
        }

        Raylib.CloseWindow();
    }

    private void Update()
    {
        float dt = Raylib.GetFrameTime();

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE) || Raylib.IsKeyPressed(KeyboardKey.KEY_Q))
        {
            _shouldExit = true;
            return;
        }

        // Navigation
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_DOWN) || Raylib.IsKeyPressed(KeyboardKey.KEY_S))
        {
            _selectedIndex = (_selectedIndex + 1) % _choices.Length;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_UP) || Raylib.IsKeyPressed(KeyboardKey.KEY_W))
        {
            _selectedIndex = (_selectedIndex - 1 + _choices.Length) % _choices.Length;
        }

        // Direct selection + activate with 1-4
        for (int i = 0; i < _choices.Length && i < 4; i++)
        {
            KeyboardKey key = (KeyboardKey)((int)KeyboardKey.KEY_ONE + i);
            if (Raylib.IsKeyPressed(key))
            {
                _selectedIndex = i;
                PerformChoice(i);
                return;
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ENTER) || Raylib.IsKeyPressed(KeyboardKey.KEY_SPACE))
        {
            PerformChoice(_selectedIndex);
        }

        if (_actionMessageTimer > 0f)
        {
            _actionMessageTimer -= dt;
            if (_actionMessageTimer <= 0f)
            {
                _actionMessage = "";
            }
        }
    }

    private void PerformChoice(int index)
    {
        switch (index)
        {
            case 0: // Burn and flee
                _morale = Clamp(_morale - 18);
                _provisions = Clamp(_provisions - 22);
                _exposure = Clamp(_exposure + 14);
                _actionMessage = "You set the letter on fire and vanish into the stairwell.";
                break;

            case 1: // Ignore
                _suspicion = Clamp(_suspicion + 22);
                _morale = Clamp(_morale - 28);
                _actionMessage = "The envelope stays sealed. The walls feel closer.";
                break;

            case 2: // Call family
                _suspicion = Clamp(_suspicion + 31);
                _morale = Clamp(_morale + 6);
                _actionMessage = "Your mother's voice cracks. They already came asking.";
                break;

            case 3: // Pack and wait
                _provisions = Clamp(_provisions + 8);
                _exposure = Clamp(_exposure - 5);
                _morale = Clamp(_morale - 9);
                _actionMessage = "You stuff a bag. Every sound outside makes you freeze.";
                break;
        }

        _actionMessageTimer = ActionMessageDuration;
    }

    private static int Clamp(int v) => Math.Max(0, Math.Min(100, v));

    private void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Palette.Bg);

        DrawTopBar();
        DrawLeftStatPanel();
        DrawCentralScene();
        DrawChoicePanel();
        DrawFooter();

        Raylib.EndDrawing();
    }

    private void DrawTopBar()
    {
        Raylib.DrawRectangle(0, 0, _screenWidth, GameConstants.TopBarHeight, Palette.PanelBg);
        Raylib.DrawLine(0, GameConstants.TopBarHeight, _screenWidth, GameConstants.TopBarHeight, Palette.Frame);

        // Title
        var font = Raylib.GetFontDefault();
        Raylib.DrawTextEx(font, "CONSCRIPT", new Vector2(20, 11), LayoutConstants.TitleFontSize, 1.2f, Palette.TextPrimary);

        // Subtitle / location
        Raylib.DrawTextEx(font, "Day 1  •  City Apartment  •  Early Winter",
            new Vector2(GameConstants.LeftPanelWidth + 30, 16),
            LayoutConstants.SubtitleFontSize, 1f, Palette.TextDim);
    }

    private void DrawLeftStatPanel()
    {
        int x = 0;
        int y = GameConstants.TopBarHeight;
        int w = GameConstants.LeftPanelWidth;
        int h = _screenHeight - y - GameConstants.FooterHeight;

        Raylib.DrawRectangle(x, y, w, h, Palette.PanelBg);
        Raylib.DrawLine(w, y, w, y + h, Palette.Frame);

        var font = Raylib.GetFontDefault();
        int textX = x + 16;
        int currentY = y + 18;

        Raylib.DrawTextEx(font, "STATS", new Vector2(textX, currentY), 15, 1f, Palette.TextMuted);
        currentY += 28;

        DrawStatRow(ref currentY, textX, "SUSPICION", _suspicion, Palette.Suspicion);
        DrawStatRow(ref currentY, textX, "HEALTH", _health, Palette.Health);
        DrawStatRow(ref currentY, textX, "MORALE", _morale, Palette.Morale);
        DrawStatRow(ref currentY, textX, "EXPOSURE", _exposure, Palette.Exposure);
        DrawStatRow(ref currentY, textX, "PROVISIONS", _provisions, Palette.Supplies);

        // Tiny footer note inside panel
        Raylib.DrawTextEx(font, "Every choice has weight.",
            new Vector2(textX, y + h - 28), 13, 0.8f, Palette.TextMuted);
    }

    private void DrawStatRow(ref int y, int x, string label, int value, Color barColor)
    {
        var font = Raylib.GetFontDefault();
        float pct = value / 100f;

        Raylib.DrawTextEx(font, label, new Vector2(x, y), LayoutConstants.StatLabelFontSize, 0.9f, Palette.TextDim);
        y += 20;

        int barX = x + 4;
        int barW = GameConstants.LeftPanelWidth - 40;
        int barH = 7;

        // background track
        Raylib.DrawRectangle(barX, y, barW, barH, new Color(30, 32, 28, 255));
        // fill
        int fillW = (int)(barW * pct);
        if (fillW > 0)
            Raylib.DrawRectangle(barX, y, fillW, barH, barColor);

        // numeric
        string valStr = $"{value}%";
        Raylib.DrawTextEx(font, valStr, new Vector2(barX + barW + 8, y - 2), LayoutConstants.StatValueFontSize, 0.8f, Palette.TextPrimary);

        y += 22;
    }

    private void DrawCentralScene()
    {
        int sceneX = GameConstants.SceneMarginLeft;
        int sceneY = GameConstants.SceneTop;
        int sceneW = _screenWidth - sceneX - GameConstants.SceneMarginRight;
        int sceneH = GameConstants.SceneHeight;

        // Outer frame
        Raylib.DrawRectangle(sceneX - 2, sceneY - 2, sceneW + 4, sceneH + 4, Palette.Frame);
        Raylib.DrawRectangle(sceneX, sceneY, sceneW, sceneH, Palette.SceneBg);

        // Inner subtle border
        Raylib.DrawRectangleLines(sceneX + 6, sceneY + 6, sceneW - 12, sceneH - 12, Palette.FrameLight);

        // === Room drawing (procedural stand-in for "central image") ===
        int roomX = sceneX + 20;
        int roomY = sceneY + 20;
        int roomW = sceneW - 40;
        int roomH = sceneH - 50;

        // Back wall
        Raylib.DrawRectangle(roomX, roomY, roomW, roomH - 60, Palette.Wall);

        // Floor
        Raylib.DrawRectangle(roomX, roomY + roomH - 60, roomW, 60, Palette.Floor);

        // Small window (upper left of room)
        int winX = roomX + 30;
        int winY = roomY + 25;
        Raylib.DrawRectangle(winX, winY, 90, 70, new Color(25, 35, 45, 255));
        Raylib.DrawRectangleLines(winX, winY, 90, 70, Palette.FrameLight);
        // window panes
        Raylib.DrawLine(winX + 45, winY, winX + 45, winY + 70, Palette.FrameLight);
        Raylib.DrawLine(winX, winY + 35, winX + 90, winY + 35, Palette.FrameLight);

        // Lamp (hanging, upper center-right)
        int lampX = roomX + roomW - 110;
        int lampY = roomY + 18;
        Raylib.DrawLine(lampX, lampY, lampX, lampY + 22, Palette.TextMuted);
        Raylib.DrawCircle(lampX, lampY + 28, 9, Palette.LampLight);
        Raylib.DrawCircle(lampX, lampY + 28, 5, new Color(140, 120, 70, 255));

        // Table
        int tableY = roomY + roomH - 95;
        Raylib.DrawRectangle(roomX + 60, tableY, roomW - 120, 18, Palette.TableWood);
        // table legs
        Raylib.DrawRectangle(roomX + 70, tableY + 18, 6, 35, Palette.TableWood);
        Raylib.DrawRectangle(roomX + roomW - 85, tableY + 18, 6, 35, Palette.TableWood);

        // Seated figure (side view, back-ish)
        int personX = roomX + 95;
        int personY = tableY - 55;
        // head
        Raylib.DrawCircle(personX + 18, personY + 12, 11, Palette.Person);
        // torso
        Raylib.DrawRectangle(personX + 8, personY + 22, 20, 28, Palette.Person);
        // arm on table
        Raylib.DrawRectangle(personX + 26, personY + 30, 38, 6, Palette.Person);
        // legs under table
        Raylib.DrawRectangle(personX + 10, personY + 48, 8, 22, Palette.Person);
        Raylib.DrawRectangle(personX + 22, personY + 48, 8, 22, Palette.Person);

        // Chair back
        Raylib.DrawRectangle(personX - 2, personY + 18, 6, 50, new Color(40, 36, 30, 255));

        // The envelope / draft summons on the table
        int envX = roomX + 160;
        int envY = tableY - 6;
        Raylib.DrawRectangle(envX, envY, 78, 48, Palette.Envelope);
        Raylib.DrawRectangleLines(envX, envY, 78, 48, new Color(90, 85, 75, 255));
        // "seal" / stamp
        Raylib.DrawCircle(envX + 58, envY + 14, 9, Palette.StampRed);
        Raylib.DrawCircle(envX + 58, envY + 14, 5, new Color(160, 50, 45, 255));

        var font = Raylib.GetFontDefault();
        Raylib.DrawTextEx(font, "SUMMONS", new Vector2(envX + 6, envY + 18), 13, 0.6f, new Color(80, 30, 20, 255));

        // Scene caption
        Raylib.DrawTextEx(font, "The letter arrived this morning. The city feels very small.",
            new Vector2(sceneX + 20, sceneY + sceneH - 26),
            LayoutConstants.SceneCaptionFontSize, 0.9f, Palette.TextDim);

        // Action result message (temporary)
        if (_actionMessageTimer > 0f && !string.IsNullOrEmpty(_actionMessage))
        {
            float alpha = MathF.Min(1f, _actionMessageTimer / 0.8f);
            var msgColor = new Color((byte)Palette.ActionFlash.R, (byte)Palette.ActionFlash.G, (byte)Palette.ActionFlash.B, (byte)(alpha * 255));
            Raylib.DrawTextEx(font, _actionMessage,
                new Vector2(sceneX + 20, sceneY + 18),
                LayoutConstants.LogFontSize, 0.8f, msgColor);
        }
    }

    private void DrawChoicePanel()
    {
        int y = GameConstants.ChoicePanelTop;
        int x = GameConstants.SceneMarginLeft;
        int w = _screenWidth - x - GameConstants.SceneMarginRight;
        int h = GameConstants.ChoicePanelHeight;

        Raylib.DrawRectangle(x, y, w, h, Palette.PanelBg);
        Raylib.DrawLine(x, y, x + w, y, Palette.Frame);

        var font = Raylib.GetFontDefault();
        Raylib.DrawTextEx(font, "WHAT DO YOU DO?", new Vector2(x + 12, y + 8), 14, 0.9f, Palette.TextMuted);

        int choiceY = y + 32;
        for (int i = 0; i < _choices.Length; i++)
        {
            bool selected = i == _selectedIndex;
            string prefix = selected ? "▶  " : "   ";
            string text = prefix + (i + 1) + ". " + _choices[i];

            if (selected)
            {
                Raylib.DrawRectangle(x + 6, choiceY - 2, w - 12, 22, Palette.SelectedBg);
                Raylib.DrawRectangle(x + 6, choiceY - 2, 3, 22, Palette.SelectedBorder);
            }

            Raylib.DrawTextEx(font, text, new Vector2(x + 16, choiceY),
                LayoutConstants.ChoiceFontSize, 0.95f,
                selected ? Palette.TextPrimary : Palette.TextDim);

            choiceY += 24;
        }
    }

    private void DrawFooter()
    {
        int y = _screenHeight - GameConstants.FooterHeight;
        Raylib.DrawRectangle(0, y, _screenWidth, GameConstants.FooterHeight, Palette.PanelBg);
        Raylib.DrawLine(0, y, _screenWidth, y, Palette.Frame);

        var font = Raylib.GetFontDefault();
        string hint = "W/S or ↑/↓  navigate   •   ENTER or 1-4  choose   •   Q or ESC  quit";
        Raylib.DrawTextEx(font, hint, new Vector2(20, y + 8), 14, 0.8f, Palette.TextMuted);

        // small right side
        Raylib.DrawTextEx(font, "prototype • raylib", new Vector2(_screenWidth - 160, y + 8), 13, 0.7f, Palette.TextMuted);
    }
}

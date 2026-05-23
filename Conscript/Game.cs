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

    // === Top bar context (matches reference image) ===
    private int _day = 3;
    private string _timeOfDay = "Morning";
    private string _warIntensity = "Low";
    private int _age = 28;
    private string _season = "Early Winter";

    // === Core stats (values from the reference) ===
    private int _suspicion = 26;
    private int _money = 35200;
    private int _health = 81;
    private int _morale = 63;
    private string _documents = "None";
    private string _status = "Fugitive - Deep Forest";
    private int _exposure = 38;

    private int _selectedIndex;
    private readonly string[] _choices =
    {
        "SET UP IMPROVED CAMP",
        "GATHER WINTER SUPPLIES",
        "FORTIFY SHELTER FOR COLD",
        "CHECK RADIO / LISTEN FOR NEWS"
    };

    // Narrative text for the current scene (from reference)
    private const string ShortNarrative = "Deeper now. The trees are closing in. Winter is coming early.";
    private const string LongNarrative =
        "You pushed deeper into the forest.\nThe city is far behind. First light snow\nhas begun to fall — winter is arriving\nsooner than expected. This will not be easy.";

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

        // Horizontal navigation for bottom action buttons
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_RIGHT) || Raylib.IsKeyPressed(KeyboardKey.KEY_D))
        {
            _selectedIndex = (_selectedIndex + 1) % _choices.Length;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_LEFT) || Raylib.IsKeyPressed(KeyboardKey.KEY_A))
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
        // Forest camp actions (Day 3). Simple trade-offs for the prototype.
        switch (index)
        {
            case 0: // Set up improved camp
                _exposure = Clamp(_exposure - 11);
                _morale = Clamp(_morale + 8);
                _health = Clamp(_health - 3);
                _actionMessage = "Better shelter. The wind is less cruel tonight.";
                break;

            case 1: // Gather winter supplies (harder now)
                _money = Math.Max(0, _money - 180);
                // (provisions conceptually increased; not shown on this screen)
                _exposure = Clamp(_exposure + 7);
                _actionMessage = "You found dry branches and a few cans. Exhausting work.";
                break;

            case 2: // Fortify for cold
                _exposure = Clamp(_exposure - 18);
                _morale = Clamp(_morale - 6);
                _health = Clamp(_health + 5);
                _actionMessage = "The walls are tighter. The cold bites less.";
                break;

            case 3: // Check radio / listen for news
                _suspicion = Clamp(_suspicion + 5);
                _morale = Clamp(_morale + 4);
                _actionMessage = "Static. A name you know. Nothing about you yet.";
                break;
        }

        _actionMessageTimer = ActionMessageDuration;
    }

    private void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Palette.Bg);

        DrawTopBar();
        DrawScenePlaceholder();
        DrawStatsPanel();
        DrawNarrativeOverlays();
        DrawActionButtons();

        Raylib.EndDrawing();
    }

    // Top segmented info bar (exactly as in the reference image)
    private void DrawTopBar()
    {
        int barH = GameConstants.TopBarHeight;
        Raylib.DrawRectangle(0, 0, _screenWidth, barH, Palette.PanelBg);

        var font = Raylib.GetFontDefault();
        int yText = 9;
        int yLabel = 28;

        // Helper to draw one dark cell
        void DrawCell(int x, int w, string main, string? subLabel = null, bool isTitle = false)
        {
            Raylib.DrawRectangle(x, 4, w, barH - 8, new Color(18, 20, 24, 255));
            Raylib.DrawRectangleLines(x, 4, w, barH - 8, Palette.Frame);

            if (isTitle)
            {
                Raylib.DrawTextEx(font, main, new Vector2(x + 12, yText + 2), LayoutConstants.TitleFontSize, 1.0f, Palette.TextPrimary);
            }
            else
            {
                Raylib.DrawTextEx(font, main, new Vector2(x + 10, yText), LayoutConstants.TopInfoFontSize, 0.8f, Palette.TextPrimary);
                if (!string.IsNullOrEmpty(subLabel))
                {
                    Raylib.DrawTextEx(font, subLabel, new Vector2(x + 10, yLabel), LayoutConstants.TopLabelFontSize, 0.6f, Palette.TextMuted);
                }
            }
        }

        int x = 8;
        int gap = 6;

        // 1. CONSCRIPT (title cell)
        int w0 = 148;
        DrawCell(x, w0, "CONSCRIPT", null, isTitle: true);
        x += w0 + gap;

        // 2. Day
        int w1 = 172;
        DrawCell(x, w1, $"Day {_day} ({_timeOfDay})", "DAY / TIME");
        x += w1 + gap;

        // 3. War Intensity
        int w2 = 198;
        DrawCell(x, w2, $"War Intensity: {_warIntensity}", "THREAT LEVEL");
        x += w2 + gap;

        // 4. Age
        int w3 = 138;
        DrawCell(x, w3, $"{_age} years old", "AGE");
        x += w3 + gap;

        // 5. Season (fills remaining)
        int w4 = _screenWidth - x - 8;
        DrawCell(x, w4, $"Season: {_season}", "ENVIRONMENT");
    }

    // Large central scene area (placeholder for future background art)
    private void DrawScenePlaceholder()
    {
        int sceneX = GameConstants.SceneInset;
        int sceneY = GameConstants.MainAreaTop;
        int sceneW = _screenWidth - GameConstants.SceneInset * 2;
        int sceneH = GameConstants.MainAreaBottom - GameConstants.MainAreaTop;

        // Outer subtle frame
        Raylib.DrawRectangle(sceneX - 1, sceneY - 1, sceneW + 2, sceneH + 2, Palette.Frame);
        Raylib.DrawRectangle(sceneX, sceneY, sceneW, sceneH, Palette.NightBg);

        // Very light ground plane (snow)
        int groundY = sceneY + (int)(sceneH * 0.72f);
        Raylib.DrawRectangle(sceneX, groundY, sceneW, sceneH - (groundY - sceneY), Palette.NightGround);

        var font = Raylib.GetFontDefault();

        // Minimal tree silhouettes (distant and mid)
        DrawSimpleTree(sceneX + 80, groundY - 40, 110, 140, 0.6f);
        DrawSimpleTree(sceneX + 180, groundY - 30, 70, 95, 0.45f);
        DrawSimpleTree(sceneX + 260, groundY - 55, 95, 130, 0.7f);
        DrawSimpleTree(sceneX + sceneW - 220, groundY - 35, 85, 115, 0.55f);
        DrawSimpleTree(sceneX + sceneW - 140, groundY - 48, 120, 155, 0.85f);
        DrawSimpleTree(sceneX + sceneW - 70, groundY - 25, 55, 80, 0.35f);

        // Small lean-to shelter on the left (matches reference)
        int shelterX = sceneX + 95;
        int shelterY = groundY - 68;
        Raylib.DrawTriangle(
            new Vector2(shelterX, shelterY + 68),
            new Vector2(shelterX + 95, shelterY + 68),
            new Vector2(shelterX + 48, shelterY),
            Palette.Shelter);
        Raylib.DrawLine(shelterX + 20, shelterY + 20, shelterX + 48, shelterY, Palette.TreeDark);
        Raylib.DrawLine(shelterX + 75, shelterY + 20, shelterX + 48, shelterY, Palette.TreeDark);

        // Tiny walking figure with backpack (center-right path)
        int figX = sceneX + sceneW / 2 + 30;
        int figY = groundY - 52;
        // head
        Raylib.DrawCircle(figX + 6, figY + 6, 5, new Color((byte)20, (byte)22, (byte)26, (byte)255));
        // body + backpack
        Raylib.DrawRectangle(figX, figY + 11, 12, 22, new Color(25, 27, 32, 255));
        Raylib.DrawRectangle(figX - 4, figY + 13, 6, 16, new Color(30, 32, 36, 255)); // pack
        // legs
        Raylib.DrawRectangle(figX + 2, figY + 32, 4, 14, new Color(22, 24, 28, 255));
        Raylib.DrawRectangle(figX + 7, figY + 32, 4, 14, new Color(22, 24, 28, 255));

        // Light snow dots (static for prototype)
        for (int i = 0; i < 42; i++)
        {
            int sx = sceneX + 30 + (i * 31 % (sceneW - 60));
            int sy = sceneY + 20 + (i * 17 % (groundY - sceneY - 30));
            Raylib.DrawPixel(sx, sy, Palette.Snow);
            if (i % 3 == 0)
                Raylib.DrawPixel(sx + 1, sy + 1, new Color((byte)150, (byte)155, (byte)165, (byte)120));
        }

        // Subtle inner vignette border
        Raylib.DrawRectangleLines(sceneX + 4, sceneY + 4, sceneW - 8, sceneH - 8, new Color((byte)20, (byte)22, (byte)28, (byte)140));
    }

    private void DrawSimpleTree(int x, int baseY, int w, int h, float alpha)
    {
        byte a = (byte)(alpha * 255);
        Color c = new Color((byte)Palette.TreeDark.R, (byte)Palette.TreeDark.G, (byte)Palette.TreeDark.B, a);

        // trunk
        Raylib.DrawRectangle(x + w / 2 - 3, baseY - h / 3, 6, h / 3, c);
        // foliage triangle
        Raylib.DrawTriangle(
            new Vector2(x, baseY),
            new Vector2(x + w, baseY),
            new Vector2(x + w / 2, baseY - h),
            c);
    }

    // "Updated Stats" panel (left, overlaid on scene) matching the reference exactly
    private void DrawStatsPanel()
    {
        int x = GameConstants.SceneInset + 18;
        int y = GameConstants.MainAreaTop + 16;
        int w = GameConstants.StatsPanelWidth;
        int h = GameConstants.StatsPanelHeight;

        Raylib.DrawRectangle(x, y, w, h, Palette.OverlayBg);
        Raylib.DrawRectangleLines(x, y, w, h, Palette.OverlayBorder);

        var font = Raylib.GetFontDefault();
        int tx = x + 12;
        int cy = y + 10;

        Raylib.DrawTextEx(font, "Updated Stats", new Vector2(tx, cy), 13, 0.7f, Palette.TextMuted);
        cy += 20;

        DrawStatLine(ref cy, tx, "Suspicion", $"{_suspicion}%", "(down from moving)");
        DrawStatLine(ref cy, tx, "Money", $"{_money:N0}", "+", deltaColor: Palette.Positive);
        DrawStatLine(ref cy, tx, "Health", $"{_health}%", "(tired from hike)");
        DrawStatLine(ref cy, tx, "Morale", $"{_morale}%", "");
        DrawStatLine(ref cy, tx, "Documents", _documents, "");
        DrawStatLine(ref cy, tx, "Status", _status, "");
        DrawStatLine(ref cy, tx, "Exposure", $"{_exposure}%", "(reset lower)");
    }

    private void DrawStatLine(ref int y, int x, string label, string value, string note, Color? deltaColor = null)
    {
        var font = Raylib.GetFontDefault();

        Raylib.DrawTextEx(font, label + ":", new Vector2(x, y), LayoutConstants.StatListFontSize, 0.7f, Palette.TextDim);

        int valX = x + 92;
        Raylib.DrawTextEx(font, value, new Vector2(valX, y), LayoutConstants.StatListFontSize, 0.7f, Palette.TextPrimary);

        if (!string.IsNullOrEmpty(note))
        {
            int noteX = valX + (int)Raylib.MeasureTextEx(font, value, LayoutConstants.StatListFontSize, 0.7f).X + 6;
            Color nc = deltaColor ?? Palette.TextMuted;
            Raylib.DrawTextEx(font, note, new Vector2(noteX, y), LayoutConstants.SmallNoteFontSize, 0.6f, nc);
        }

        y += 18;
    }

    // The two narrative text boxes overlaid on the scene (upper center + right side)
    private void DrawNarrativeOverlays()
    {
        var font = Raylib.GetFontDefault();
        int sceneLeft = GameConstants.SceneInset;
        int sceneTop = GameConstants.MainAreaTop;

        // Short banner near top of scene (slightly left of center)
        int shortX = sceneLeft + 70;
        int shortY = sceneTop + 22;
        Raylib.DrawRectangle(shortX, shortY, GameConstants.ShortNarrativeWidth, GameConstants.ShortNarrativeHeight, Palette.OverlayBg);
        Raylib.DrawRectangleLines(shortX, shortY, GameConstants.ShortNarrativeWidth, GameConstants.ShortNarrativeHeight, Palette.OverlayBorder);
        Raylib.DrawTextEx(font, ShortNarrative,
            new Vector2(shortX + 10, shortY + 6),
            LayoutConstants.NarrativeFontSize, 0.8f, Palette.TextPrimary);

        // Longer right-side narrative box
        int longX = _screenWidth - GameConstants.SceneInset - GameConstants.LongNarrativeWidth - 18;
        int longY = sceneTop + 160;
        Raylib.DrawRectangle(longX, longY, GameConstants.LongNarrativeWidth, GameConstants.LongNarrativeHeight, Palette.OverlayBg);
        Raylib.DrawRectangleLines(longX, longY, GameConstants.LongNarrativeWidth, GameConstants.LongNarrativeHeight, Palette.OverlayBorder);

        // Draw multi-line text
        string[] lines = LongNarrative.Split('\n');
        int lineY = longY + 10;
        foreach (string line in lines)
        {
            Raylib.DrawTextEx(font, line, new Vector2(longX + 10, lineY), LayoutConstants.NarrativeFontSize, 0.75f, Palette.TextPrimary);
            lineY += 18;
        }

        // Temporary action result toast (fades out)
        if (_actionMessageTimer > 0f && !string.IsNullOrEmpty(_actionMessage))
        {
            float alpha = MathF.Min(1f, _actionMessageTimer / 0.6f);
            int toastW = 380;
            int toastX = sceneLeft + 90;
            int toastY = sceneTop + 58;

            var toastColor = new Color((byte)20, (byte)22, (byte)26, (byte)(alpha * 230));
            Raylib.DrawRectangle(toastX, toastY, toastW, 24, toastColor);
            Raylib.DrawRectangleLines(toastX, toastY, toastW, 24, new Color((byte)70, (byte)75, (byte)85, (byte)(alpha * 200)));

            var c = new Color((byte)Palette.ActionFlash.R, (byte)Palette.ActionFlash.G, (byte)Palette.ActionFlash.B, (byte)(alpha * 255));
            Raylib.DrawTextEx(font, _actionMessage, new Vector2(toastX + 10, toastY + 5), 13, 0.7f, c);
        }
    }

    // Bottom row of 4 large action buttons (horizontal)
    private void DrawActionButtons()
    {
        int y = _screenHeight - GameConstants.BottomButtonHeight - 4;
        int totalGaps = (GameConstants.ButtonCount - 1) * GameConstants.ButtonGap;
        int available = _screenWidth - (GameConstants.SceneInset * 2) - totalGaps;
        int btnW = available / GameConstants.ButtonCount;
        int btnH = GameConstants.BottomButtonHeight - 8;
        int x = GameConstants.SceneInset;

        var font = Raylib.GetFontDefault();

        for (int i = 0; i < GameConstants.ButtonCount; i++)
        {
            bool selected = i == _selectedIndex;
            Color bg = selected ? Palette.ButtonSelectedBg : Palette.ButtonBg;
            Color border = selected ? Palette.ButtonSelectedBorder : Palette.ButtonBorder;

            Raylib.DrawRectangle(x, y, btnW, btnH, bg);
            Raylib.DrawRectangleLines(x, y, btnW, btnH, border);

            // Center the text
            string label = _choices[i];
            // Special case for the second button (shows the note from image)
            if (i == 1)
            {
                label += " (harder now)";
            }

            Vector2 size = Raylib.MeasureTextEx(font, label, LayoutConstants.ButtonFontSize, 0.8f);
            int tx = x + (btnW - (int)size.X) / 2;
            int ty = y + (btnH - (int)size.Y) / 2 - 1;

            Raylib.DrawTextEx(font, label, new Vector2(tx, ty), LayoutConstants.ButtonFontSize, 0.8f,
                selected ? Palette.TextPrimary : Palette.TextDim);

            x += btnW + GameConstants.ButtonGap;
        }

        // Tiny control hint under the buttons
        Raylib.DrawTextEx(font, "← → or A/D  select   •   ENTER / 1-4  act   •   Q or ESC  quit",
            new Vector2(GameConstants.SceneInset, y + btnH + 6),
            12, 0.6f, Palette.TextMuted);
    }

    private static int Clamp(int v) => Math.Max(0, Math.Min(100, v));
}

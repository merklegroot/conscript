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
        DrawLeftSidebar();
        DrawCentralScene();
        DrawActionBar();

        Raylib.EndDrawing();
    }

    // =====================================================================
    // TOP BAR — Clean, cinematic header with generous breathing room
    // =====================================================================
    private void DrawTopBar()
    {
        int h = GameConstants.TopBarHeight;

        // Deep header background
        Raylib.DrawRectangle(0, 0, _screenWidth, h, Palette.HeaderBg);

        // Very subtle bottom divider
        Raylib.DrawLine(0, h, _screenWidth, h, Palette.Divider);

        var font = Raylib.GetFontDefault();
        int centerY = (h - 18) / 2; // vertical center for primary text

        // --- Left: Title ---
        Raylib.DrawTextEx(font, "CONSCRIPT",
            new Vector2(24, centerY - 2),
            LayoutConstants.TitleFontSize, 0.9f, Palette.TextPrimary);

        // Thin elegant underline under the title for presence
        int titleWidth = (int)Raylib.MeasureTextEx(font, "CONSCRIPT", LayoutConstants.TitleFontSize, 0.9f).X;
        Raylib.DrawLine(24, centerY + 20, 24 + titleWidth, centerY + 20, Palette.StrongBorder);

        // --- Center-left: Day + Time ---
        int x = 210;
        Raylib.DrawTextEx(font, $"Day {_day}  •  {_timeOfDay}",
            new Vector2(x, centerY), LayoutConstants.TopInfoFontSize, 0.8f, Palette.TextSecondary);

        // Subtle vertical divider
        x += 155;
        Raylib.DrawLine(x, 14, x, h - 14, Palette.Divider);

        // --- Center-right: War Intensity ---
        x += 18;
        Raylib.DrawTextEx(font, "War Intensity",
            new Vector2(x, centerY - 9), LayoutConstants.TopMetaFontSize, 0.6f, Palette.TextMuted);
        Raylib.DrawTextEx(font, _warIntensity,
            new Vector2(x, centerY + 3), LayoutConstants.TopInfoFontSize, 0.8f, Palette.TextSecondary);

        // --- Far Right: Age + Season ---
        string rightText = $"{_age} years  •  {_season}";
        int rightWidth = (int)Raylib.MeasureTextEx(font, rightText, LayoutConstants.TopInfoFontSize, 0.8f).X;
        Raylib.DrawTextEx(font, rightText,
            new Vector2(_screenWidth - 28 - rightWidth, centerY),
            LayoutConstants.TopInfoFontSize, 0.8f, Palette.TextSecondary);
    }

    // =====================================================================
    // LEFT SIDEBAR — Fixed panel with flavor text + clean stat list
    // =====================================================================
    private void DrawLeftSidebar()
    {
        int x = 0;
        int y = GameConstants.TopBarHeight;
        int w = GameConstants.SidebarWidth;
        int h = _screenHeight - y - GameConstants.ActionBarHeight;

        // Main sidebar background
        Raylib.DrawRectangle(x, y, w, h, Palette.SidebarBg);

        // Right edge divider (subtle but present)
        Raylib.DrawLine(w, y, w, y + h, Palette.Divider);

        var font = Raylib.GetFontDefault();
        int tx = x + GameConstants.SidebarPadding;
        int cy = y + 18;

        // --- Flavor / Situation box (the "Deeper now..." message) ---
        int flavorH = 52;
        Raylib.DrawRectangle(tx - 4, cy, w - GameConstants.SidebarPadding * 2 + 8, flavorH, Palette.CardBg);
        Raylib.DrawRectangleLines(tx - 4, cy, w - GameConstants.SidebarPadding * 2 + 8, flavorH, Palette.CardBorder);

        Raylib.DrawTextEx(font, "THE FOREST",
            new Vector2(tx, cy + 6), LayoutConstants.SidebarHeaderSize, 0.6f, Palette.TextMuted);

        Raylib.DrawTextEx(font, ShortNarrative,
            new Vector2(tx, cy + 22), LayoutConstants.SidebarFlavorSize, 0.75f, Palette.TextPrimary);

        cy += flavorH + GameConstants.SidebarInternalGap + 6;

        // --- Status header ---
        Raylib.DrawTextEx(font, "STATUS",
            new Vector2(tx, cy), LayoutConstants.SidebarHeaderSize, 0.6f, Palette.TextMuted);
        cy += 20;

        // Thin line under header
        Raylib.DrawLine(tx, cy - 4, tx + 48, cy - 4, Palette.SubtleBorder);
        cy += 6;

        // --- Stats ---
        DrawSidebarStat(ref cy, tx, "Suspicion", _suspicion, Palette.Suspicion, showBar: true);
        DrawSidebarStat(ref cy, tx, "Health", _health, Palette.Health, showBar: true);
        DrawSidebarStat(ref cy, tx, "Morale", _morale, Palette.Morale, showBar: true);
        DrawSidebarStat(ref cy, tx, "Exposure", _exposure, Palette.Exposure, showBar: true);

        cy += 4;

        // Text-only stats
        DrawSidebarTextStat(ref cy, tx, "Money", $"{_money:N0}", Palette.Money);
        DrawSidebarTextStat(ref cy, tx, "Documents", _documents, null);
        DrawSidebarTextStat(ref cy, tx, "Status", _status, null, wrap: true);
    }

    private void DrawSidebarStat(ref int y, int x, string label, int value, Color barColor, bool showBar)
    {
        var font = Raylib.GetFontDefault();

        // Label
        Raylib.DrawTextEx(font, label, new Vector2(x, y), LayoutConstants.StatLabelSize, 0.8f, Palette.TextSecondary);

        // Value on the right
        string val = $"{value}%";
        int valWidth = (int)Raylib.MeasureTextEx(font, val, LayoutConstants.StatValueSize, 0.7f).X;
        Raylib.DrawTextEx(font, val, new Vector2(x + GameConstants.SidebarWidth - GameConstants.SidebarPadding * 2 - valWidth, y),
            LayoutConstants.StatValueSize, 0.7f, Palette.TextPrimary);

        y += 16;

        if (showBar)
        {
            int barWidth = GameConstants.SidebarWidth - GameConstants.SidebarPadding * 2;
            int barHeight = 5;

            // Track
            Raylib.DrawRectangle(x, y, barWidth, barHeight, new Color((byte)20, (byte)22, (byte)26, (byte)255));
            // Fill
            float pct = Math.Clamp(value / 100f, 0f, 1f);
            if (pct > 0)
            {
                Raylib.DrawRectangle(x, y, (int)(barWidth * pct), barHeight, barColor);
            }
            y += 10;
        }
        else
        {
            y += 6;
        }
    }

    private void DrawSidebarTextStat(ref int y, int x, string label, string value, Color? valueColor, bool wrap = false)
    {
        var font = Raylib.GetFontDefault();

        Raylib.DrawTextEx(font, label, new Vector2(x, y), LayoutConstants.StatLabelSize, 0.8f, Palette.TextMuted);
        y += 15;

        Color vc = valueColor ?? Palette.TextPrimary;
        Raylib.DrawTextEx(font, value, new Vector2(x, y), LayoutConstants.StatValueSize, 0.7f, vc);

        y += wrap ? 32 : 20;
    }

    // =====================================================================
    // CENTRAL SCENE — Rich, layered, cinematic night forest placeholder
    // =====================================================================
    private void DrawCentralScene()
    {
        var font = Raylib.GetFontDefault();

        int left = GameConstants.SceneLeft;
        int top = GameConstants.SceneTop;
        int w = GameConstants.SceneWidth;
        int h = GameConstants.SceneHeight;

        // Outer dark stage
        Raylib.DrawRectangle(left, top, w, h, Palette.SceneBg);

        // Inner breathing room for the "art"
        int artX = left + GameConstants.ScenePadding;
        int artY = top + GameConstants.ScenePadding;
        int artW = w - GameConstants.ScenePadding * 2;
        int artH = h - GameConstants.ScenePadding * 2;

        // Deep night base
        Raylib.DrawRectangle(artX, artY, artW, artH, Palette.DeepNight);

        // Ground plane (cold snow)
        int groundY = artY + (int)(artH * 0.68f);
        Raylib.DrawRectangle(artX, groundY, artW, artH - (groundY - artY), Palette.GroundCold);

        // === Atmospheric layers (far to near) ===

        // Far distant treeline (very dark, almost silhouette)
        DrawLayeredForest(artX, groundY, artW, 0.35f, 0.55f, Palette.TreeFar);

        // Mid distance trees
        DrawLayeredForest(artX + 40, groundY - 8, artW - 80, 0.55f, 0.72f, Palette.TreeMid);

        // Nearer, darker trees (more detail)
        DrawLayeredForest(artX + 80, groundY - 18, artW - 160, 0.72f, 0.95f, Palette.TreeNear);

        // === Shelter (left side, more detailed than before) ===
        int shelterBaseX = artX + 110;
        int shelterBaseY = groundY - 12;
        DrawLeanToShelter(shelterBaseX, shelterBaseY);

        // === Small human figure (walking toward shelter or away) ===
        int figX = artX + artW / 2 + 70;
        int figY = groundY - 58;
        DrawSmallFigure(figX, figY);

        // === Snow particles (layered for depth) ===
        DrawAtmosphericSnow(artX, artY, artW, groundY, 68);

        // === Very faint cold moonlight from upper right ===
        int moonX = artX + artW - 90;
        int moonY = artY + 70;
        Raylib.DrawCircle(moonX, moonY, 38, Palette.MoonGlow);

        // === Inner elegant frame + vignette for cinematic feel ===
        Raylib.DrawRectangleLines(artX + 2, artY + 2, artW - 4, artH - 4, Palette.SubtleBorder);

        // Stronger vignette on the edges
        Raylib.DrawRectangle(artX, artY, artW, 18, new Color(0, 0, 0, 70));
        Raylib.DrawRectangle(artX, artY + artH - 22, artW, 22, new Color(0, 0, 0, 80));
        Raylib.DrawRectangle(artX, artY, 22, artH, new Color(0, 0, 0, 55));
        Raylib.DrawRectangle(artX + artW - 22, artY, 22, artH, new Color(0, 0, 0, 55));

        // === Narrative cards inside the scene (elegant placement) ===
        DrawSceneNarrativeCards(artX, artY, artW, artH, groundY);

        // Temporary action result toast (centered low in the image)
        if (_actionMessageTimer > 0f && !string.IsNullOrEmpty(_actionMessage))
        {
            float alpha = MathF.Min(1f, _actionMessageTimer / 0.55f);
            int toastW = 420;
            int toastX = artX + (artW - toastW) / 2;
            int toastY = artY + artH - 68;

            var bg = new Color((byte)10, (byte)12, (byte)16, (byte)(alpha * 235));
            Raylib.DrawRectangle(toastX, toastY, toastW, 26, bg);
            Raylib.DrawRectangleLines(toastX, toastY, toastW, 26, new Color((byte)58, (byte)62, (byte)72, (byte)(alpha * 210)));

            var c = new Color((byte)Palette.ActionFlash.R, (byte)Palette.ActionFlash.G, (byte)Palette.ActionFlash.B, (byte)(alpha * 255));
            Raylib.DrawTextEx(font, _actionMessage, new Vector2(toastX + 14, toastY + 6), 13, 0.7f, c);
        }
    }

    private void DrawLayeredForest(int baseX, int baseY, int width, float density, float heightFactor, Color color)
    {
        int count = (int)(width / 38 * density) + 2;
        for (int i = 0; i < count; i++)
        {
            int tx = baseX + (int)(i * (width / (float)count)) + (i % 3) * 7;
            int th = (int)(62 + (i % 5) * 18 * heightFactor);
            int tw = (int)(42 + (i % 4) * 11);

            // trunk
            Raylib.DrawRectangle(tx + tw / 2 - 2, baseY - th / 2, 4, th / 2, color);
            // foliage
            Raylib.DrawTriangle(
                new Vector2(tx, baseY),
                new Vector2(tx + tw, baseY),
                new Vector2(tx + tw / 2, baseY - th),
                color);
        }
    }

    private void DrawLeanToShelter(int baseX, int baseY)
    {
        // Main triangular shelter
        Raylib.DrawTriangle(
            new Vector2(baseX, baseY),
            new Vector2(baseX + 92, baseY),
            new Vector2(baseX + 46, baseY - 72),
            Palette.ShelterWood);

        // Snow on the roof (lighter cap)
        Raylib.DrawTriangle(
            new Vector2(baseX - 3, baseY - 2),
            new Vector2(baseX + 95, baseY - 2),
            new Vector2(baseX + 46, baseY - 74),
            Palette.SnowMid);

        // Support poles
        Raylib.DrawRectangle(baseX + 12, baseY - 48, 3, 48, new Color((byte)24, (byte)25, (byte)28, (byte)255));
        Raylib.DrawRectangle(baseX + 76, baseY - 52, 3, 52, new Color((byte)24, (byte)25, (byte)28, (byte)255));

        // Opening (darker)
        Raylib.DrawTriangle(
            new Vector2(baseX + 28, baseY),
            new Vector2(baseX + 64, baseY),
            new Vector2(baseX + 46, baseY - 38),
            new Color(8, 9, 11, 255));
    }

    private void DrawSmallFigure(int x, int y)
    {
        // Head
        Raylib.DrawCircle(x + 7, y + 7, 6, new Color(18, 19, 22, 255));
        // Body + heavy backpack
        Raylib.DrawRectangle(x + 1, y + 13, 13, 24, new Color((byte)23, (byte)25, (byte)29, (byte)255));
        Raylib.DrawRectangle(x - 6, y + 15, 8, 18, new Color((byte)28, (byte)30, (byte)34, (byte)255)); // pack
        // Legs (walking pose)
        Raylib.DrawRectangle(x + 3, y + 36, 4, 15, new Color((byte)20, (byte)21, (byte)24, (byte)255));
        Raylib.DrawRectangle(x + 8, y + 35, 4, 16, new Color((byte)20, (byte)21, (byte)24, (byte)255));
    }

    private void DrawAtmosphericSnow(int artX, int artY, int artW, int groundY, int count)
    {
        for (int i = 0; i < count; i++)
        {
            // Use a simple hash for stable positions
            int sx = artX + 18 + ((i * 47 + 11) % (artW - 36));
            int sy = artY + 14 + ((i * 29 + 7) % (groundY - artY - 24));

            byte alpha = (byte)(120 + (i % 5) * 18);
            Raylib.DrawPixel(sx, sy, new Color((byte)175, (byte)178, (byte)185, alpha));

            // Occasional larger flakes
            if (i % 7 == 0)
            {
                Raylib.DrawPixel(sx + 1, sy, new Color((byte)190, (byte)193, (byte)198, (byte)(alpha - 30)));
            }
        }
    }

    private void DrawSceneNarrativeCards(int artX, int artY, int artW, int artH, int groundY)
    {
        var font = Raylib.GetFontDefault();

        // Short headline near top of art
        int shortW = 460;
        int shortX = artX + 28;
        int shortY = artY + 18;

        Raylib.DrawRectangle(shortX, shortY, shortW, 26, Palette.CardBg);
        Raylib.DrawRectangleLines(shortX, shortY, shortW, 26, Palette.CardBorder);
        Raylib.DrawTextEx(font, ShortNarrative,
            new Vector2(shortX + 12, shortY + 6),
            LayoutConstants.NarrativeShortSize, 0.8f, Palette.TextPrimary);

        // Longer reflective paragraph — anchored lower right inside the image
        int longW = 262;
        int longH = 104;
        int longX = artX + artW - longW - 26;
        int longY = artY + 92;

        Raylib.DrawRectangle(longX, longY, longW, longH, Palette.CardBg);
        Raylib.DrawRectangleLines(longX, longY, longW, longH, Palette.CardBorder);

        string[] lines = LongNarrative.Split('\n');
        int lineY = longY + 11;
        foreach (string line in lines)
        {
            Raylib.DrawTextEx(font, line, new Vector2(longX + 11, lineY),
                LayoutConstants.NarrativeLongSize, 0.75f, Palette.TextPrimary);
            lineY += 17;
        }
    }

    // =====================================================================
    // BOTTOM ACTION BAR — Strong visual weight, clear, tactile buttons
    // =====================================================================
    private void DrawActionBar()
    {
        int barY = _screenHeight - GameConstants.ActionBarHeight;
        int barH = GameConstants.ActionBarHeight;

        // Bar background
        Raylib.DrawRectangle(0, barY, _screenWidth, barH, Palette.ActionBarBg);
        Raylib.DrawLine(0, barY, _screenWidth, barY, Palette.Divider);

        var font = Raylib.GetFontDefault();

        int count = GameConstants.ActionButtonCount;
        int gap = GameConstants.ActionButtonGap;
        int paddingX = 22; // left/right margin inside the bar
        int totalGap = gap * (count - 1);
        int available = _screenWidth - paddingX * 2 - totalGap;
        int btnW = available / count;
        int btnH = barH - GameConstants.ActionBarPaddingY * 2;
        int btnY = barY + GameConstants.ActionBarPaddingY;
        int x = paddingX;

        for (int i = 0; i < count; i++)
        {
            bool selected = i == _selectedIndex;
            Color bg = selected ? Palette.ButtonSelectedBg : Palette.ButtonBg;
            Color border = selected ? Palette.ButtonSelectedBorder : Palette.ButtonBorder;

            // Button body
            Raylib.DrawRectangle(x, btnY, btnW, btnH, bg);
            Raylib.DrawRectangleLines(x, btnY, btnW, btnH, border);

            // Thin top accent when selected (gives nice "pressed" or "lit" feel)
            if (selected)
            {
                Raylib.DrawRectangle(x + 1, btnY + 1, btnW - 2, 2, Palette.ButtonTopAccent);
            }

            // Label
            string label = _choices[i];
            if (i == 1) label += "  (harder now)";

            Vector2 size = Raylib.MeasureTextEx(font, label, LayoutConstants.ActionButtonFontSize, 0.85f);
            int tx = x + (btnW - (int)size.X) / 2;
            int ty = btnY + (btnH - (int)size.Y) / 2 - 1;

            Raylib.DrawTextEx(font, label, new Vector2(tx, ty),
                LayoutConstants.ActionButtonFontSize, 0.85f,
                selected ? Palette.TextPrimary : Palette.TextDim);

            x += btnW + gap;
        }

        // Refined control hint centered below the buttons
        string hint = "← →  or  A D    select        ENTER  or  1–4    act        Q  or  ESC    quit";
        int hintWidth = (int)Raylib.MeasureTextEx(font, hint, LayoutConstants.SmallHintSize, 0.6f).X;
        Raylib.DrawTextEx(font, hint,
            new Vector2((_screenWidth - hintWidth) / 2, barY + barH - 13),
            LayoutConstants.SmallHintSize, 0.6f, Palette.TextDim);
    }

    private static int Clamp(int v) => Math.Max(0, Math.Min(100, v));
}

using System;
using System.IO;
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

    // UI font (loaded TTF for much better readability than the default bitmap font)
    private Font _uiFont;

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
    private const string LongNarrative =
        "You pushed deeper into the forest.\nThe city is far behind. First light snow\nhas begun to fall — winter is arriving\nsooner than expected. This will not be easy.";

    private string _actionMessage = "";
    private float _actionMessageTimer;

    /// <summary>
    /// Loads a high-quality TTF font for crisp, readable UI text.
    /// Falls back to Raylib's default bitmap font if no TTF is present.
    /// 
    /// Recommended: copy OpenSans.ttf from ~/repo/starflt/StarGame/Fonts/
    /// into Conscript/Fonts/ (the .csproj will copy it to the output directory).
    /// </summary>
    private Font LoadUiFont()
    {
        string baseDir = AppContext.BaseDirectory;
        string[] candidates =
        {
            Path.Combine(baseDir, "Fonts", "OpenSans.ttf"),
            Path.Combine(baseDir, "Fonts", "OpenSans-Regular.ttf"),
            Path.Combine(baseDir, "Fonts", "Inter.ttf"),
            Path.Combine(baseDir, "Fonts", "Roboto-Regular.ttf"),
        };

        foreach (string path in candidates)
        {
            if (File.Exists(path))
            {
                // 32 is the base pixel size; we control actual size via DrawTextEx fontSize param
                return Raylib.LoadFontEx(path, 32, null, 0);
            }
        }

        // No custom font found — the UI will still work, just using the default (less pretty) font.
        return Raylib.GetFontDefault();
    }

    public void Run()
    {
        Raylib.InitWindow(_screenWidth, _screenHeight, "CONSCRIPT");
        Raylib.SetTargetFPS(60);
        Raylib.SetExitKey(KeyboardKey.KEY_NULL); // we handle ESC ourselves

        _uiFont = LoadUiFont();

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
    // TOP BAR — Clean, well-spaced, three-zone layout (no more cramped segments or cutoff)
    // =====================================================================
    private void DrawTopBar()
    {
        int h = GameConstants.TopBarHeight;
        Raylib.DrawRectangle(0, 0, _screenWidth, h, Palette.HeaderBg);
        Raylib.DrawLine(0, h, _screenWidth, h, Palette.Divider);

        Font font = _uiFont;

        // We use two text rows for center and right zones for clarity + breathing room
        int row1Y = 16;   // upper line (36pt title)
        int row2Y = 40;   // lower line

        // LEFT ZONE — prominent title
        int leftX = 26;
        Raylib.DrawTextEx(font, "CONSCRIPT",
            new Vector2(leftX, row1Y),
            LayoutConstants.TitleFontSize, 0.85f, Palette.TextPrimary);

        // Elegant underline (positioned for the 36pt title)
        int titleW = (int)Raylib.MeasureTextEx(font, "CONSCRIPT", LayoutConstants.TitleFontSize, 0.85f).X;
        Raylib.DrawLine(leftX, row1Y + 28, leftX + titleW, row1Y + 28, Palette.StrongBorder);

        // CENTER ZONE — Day and War Intensity (centered in middle of screen)
        string dayLine = $"Day {_day} - {_timeOfDay}";
        string warLine = $"War Intensity: {_warIntensity}";

        int centerX = _screenWidth / 2;
        int dayW = (int)Raylib.MeasureTextEx(font, dayLine, LayoutConstants.TopInfoFontSize, 0.8f).X;
        int warW = (int)Raylib.MeasureTextEx(font, warLine, LayoutConstants.TopMetaFontSize, 0.7f).X;

        Raylib.DrawTextEx(font, dayLine,
            new Vector2(centerX - dayW / 2, row1Y),
            LayoutConstants.TopInfoFontSize, 0.8f, Palette.TextSecondary);

        Raylib.DrawTextEx(font, warLine,
            new Vector2(centerX - warW / 2, row2Y),
            LayoutConstants.TopMetaFontSize, 0.7f, Palette.TextMuted);

        // RIGHT ZONE — right-aligned, two lines
        string ageLine = $"{_age} years old";
        string seasonLine = _season;

        int rightMargin = 26;
        int ageW = (int)Raylib.MeasureTextEx(font, ageLine, LayoutConstants.TopInfoFontSize, 0.8f).X;
        int seasonW = (int)Raylib.MeasureTextEx(font, seasonLine, LayoutConstants.TopInfoFontSize, 0.8f).X;

        int rightXAge = _screenWidth - rightMargin - ageW;
        int rightXSeason = _screenWidth - rightMargin - seasonW;

        Raylib.DrawTextEx(font, ageLine,
            new Vector2(rightXAge, row1Y),
            LayoutConstants.TopInfoFontSize, 0.8f, Palette.TextSecondary);

        Raylib.DrawTextEx(font, seasonLine,
            new Vector2(rightXSeason, row2Y),
            LayoutConstants.TopInfoFontSize, 0.8f, Palette.TextPrimary);
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

        Raylib.DrawRectangle(x, y, w, h, Palette.SidebarBg);
        Raylib.DrawLine(w, y, w, y + h, Palette.Divider);

        Font font = _uiFont;
        int tx = x + GameConstants.SidebarPadding;
        int cy = y + 28;   // comfortable top padding for the STATUS section with larger fonts

        // === STATUS header ===
        Raylib.DrawTextEx(font, "STATUS",
            new Vector2(tx, cy), LayoutConstants.SidebarHeaderSize, 0.7f, Palette.TextMuted);
        cy += 16;

        // Subtle underline
        Raylib.DrawLine(tx, cy - 2, tx + 42, cy - 2, Palette.SubtleBorder);
        cy += 10;

        // === Clean vertical stat list ===
        // Numeric stats with bars (label + value on one line, bar underneath)
        DrawCleanStatLine(ref cy, tx, "Suspicion", _suspicion, Palette.Suspicion);
        DrawCleanStatLine(ref cy, tx, "Health", _health, Palette.Health);
        DrawCleanStatLine(ref cy, tx, "Morale", _morale, Palette.Morale);
        DrawCleanStatLine(ref cy, tx, "Exposure", _exposure, Palette.Exposure);

        cy += 6;

        // Simple text stats (same line for label + value to reduce clutter)
        DrawTextStatLine(ref cy, tx, "Money", $"{_money:N0}");
        DrawTextStatLine(ref cy, tx, "Documents", _documents);
        DrawTextStatLine(ref cy, tx, "Status", _status);
    }

    // Clean single-line stat row:  Label          26%  [thin colored bar]
    private void DrawCleanStatLine(ref int y, int x, string label, int value, Color barColor)
    {
        Font font = _uiFont;
        int available = GameConstants.SidebarWidth - GameConstants.SidebarPadding * 2;

        // Label (left)
        Raylib.DrawTextEx(font, label, new Vector2(x, y), LayoutConstants.StatLabelSize, 0.75f, Palette.TextSecondary);

        // Value (right of label area)
        string val = $"{value}%";
        int valW = (int)Raylib.MeasureTextEx(font, val, LayoutConstants.StatValueSize, 0.7f).X;
        int valX = x + available - valW;
        Raylib.DrawTextEx(font, val, new Vector2(valX, y), LayoutConstants.StatValueSize, 0.7f, Palette.TextPrimary);

        y += 18;

        // Thin progress bar underneath
        int barW = available;
        int barH = 5;
        Raylib.DrawRectangle(x, y, barW, barH, new Color((byte)22, (byte)24, (byte)28, (byte)255));
        float pct = Math.Clamp(value / 100f, 0f, 1f);
        if (pct > 0.01f)
        {
            Raylib.DrawRectangle(x, y, (int)(barW * pct), barH, barColor);
        }

        y += 14; // good spacing to next row
    }

    // Simple text-only row:  Label          Value
    private void DrawTextStatLine(ref int y, int x, string label, string value)
    {
        Font font = _uiFont;
        int available = GameConstants.SidebarWidth - GameConstants.SidebarPadding * 2;

        Raylib.DrawTextEx(font, label, new Vector2(x, y), LayoutConstants.StatLabelSize, 0.75f, Palette.TextMuted);

        int valW = (int)Raylib.MeasureTextEx(font, value, LayoutConstants.StatValueSize, 0.7f).X;
        Raylib.DrawTextEx(font, value, new Vector2(x + available - valW, y), LayoutConstants.StatValueSize, 0.7f, Palette.TextPrimary);

        y += 20;
    }

    // =====================================================================
    // CENTRAL SCENE — Rich, layered, cinematic night forest placeholder
    // =====================================================================
    private void DrawCentralScene()
    {
        Font font = _uiFont;

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

        // === Main narrative / flavor text box — clean, anchored to the right side of the image ===
        DrawRightSideNarrative(artX, artY, artW, artH);

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

    // Clean narrative box anchored to the right edge of the central image.
    // This is the main flavor text for the current scene.
    /// <summary>
    /// Word-wraps text to fit within maxWidth, returning the lines and the total
    /// pixel height required when drawn with the given font/size/spacing.
    /// This is what makes the narrative card size itself correctly.
    /// </summary>
    private (List<string> lines, int height) WrapTextForBox(string text, Font font, float fontSize, float spacing, int maxWidth, int lineHeight)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (new List<string>(), 0);

        // Normalize the provided multi-line string into words (respecting existing line breaks as strong breaks)
        var paragraphs = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();

        foreach (string paragraph in paragraphs)
        {
            string[] words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string current = "";

            foreach (string word in words)
            {
                string candidate = current.Length == 0 ? word : current + " " + word;
                Vector2 size = Raylib.MeasureTextEx(font, candidate, fontSize, spacing);

                if (size.X > maxWidth && current.Length > 0)
                {
                    lines.Add(current.Trim());
                    current = word;
                }
                else
                {
                    current = candidate;
                }
            }

            if (current.Length > 0)
                lines.Add(current.Trim());
        }

        int totalHeight = lines.Count * lineHeight;
        return (lines, totalHeight);
    }

    /// <summary>
    /// Draws the main scene narrative ("You pushed deeper...") in a card whose size
    /// is computed from the actual measured text. No more hard-coded boxes that clip or look wrong.
    /// </summary>
    private void DrawRightSideNarrative(int artX, int artY, int artW, int artH)
    {
        Font font = _uiFont;   // the nice readable font (falls back to default if none present)
        float fontSize = LayoutConstants.NarrativeLongSize;
        float spacing = 0.9f;  // a touch tighter for 18pt body text
        int lineHeight = (int)(fontSize * 1.42f); // generous, readable line spacing at the larger size

        int maxCardWidth = 320;                    // wider to accommodate the larger 18pt narrative text with fewer wraps
        int horizontalPadding = 18;
        int verticalPadding = 16;                  // generous padding so the bigger text feels comfortable and airy

        int textMaxWidth = maxCardWidth - horizontalPadding * 2;

        // Measure what we actually need
        var (wrappedLines, textHeight) = WrapTextForBox(
            LongNarrative,
            font,
            fontSize,
            spacing,
            textMaxWidth,
            lineHeight);

        // Final card dimensions (never smaller than a minimum nice size)
        int cardW = maxCardWidth;
        int cardH = textHeight + verticalPadding * 2;

        // Position: right side of the art area, with breathing room from the edge
        int cardX = artX + artW - cardW - 18;
        int cardY = artY + 22;

        // Draw the card
        Raylib.DrawRectangle(cardX, cardY, cardW, cardH, Palette.CardBg);
        Raylib.DrawRectangleLines(cardX, cardY, cardW, cardH, Palette.CardBorder);

        // Draw the measured lines
        int textLeft = cardX + horizontalPadding;
        int textTop = cardY + verticalPadding;

        for (int i = 0; i < wrappedLines.Count; i++)
        {
            Raylib.DrawTextEx(
                font,
                wrappedLines[i],
                new Vector2(textLeft, textTop + i * lineHeight),
                fontSize,
                spacing,
                Palette.TextPrimary);
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

        Font font = _uiFont;

        int count = GameConstants.ActionButtonCount;
        int gap = GameConstants.ActionButtonGap;
        int paddingX = 28; // generous left/right margin for breathing room
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

        // Refined control hint — plenty of space below the buttons
        string hint = "← →  or  A D    select        ENTER  or  1–4    act        Q  or  ESC    quit";
        int hintWidth = (int)Raylib.MeasureTextEx(font, hint, LayoutConstants.SmallHintSize, 0.6f).X;
        Raylib.DrawTextEx(font, hint,
            new Vector2((_screenWidth - hintWidth) / 2, barY + barH - 24),
            LayoutConstants.SmallHintSize, 0.6f, Palette.TextDim);
    }

    private static int Clamp(int v) => Math.Max(0, Math.Min(100, v));
}

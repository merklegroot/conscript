using System;
using System.IO;
using System.Numerics;
using System.Linq;
using System.Reflection;
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
    private Texture2D _backgroundTexture;   // currently active scene background (swapped on phase change)
    private Texture2D _apartmentBackground;
    private Texture2D _outsideBackground;
    private Texture2D _forestBackground;

    // Restart button (top right, always available)
    private Rectangle _restartButtonRect;
    private bool _restartHovered;

    // === Game flow ===
    private enum Phase
    {
        Opening,   // At home with family — the knock on the door
        Outside,   // In the apartment courtyard / yard immediately after climbing out the window
        Forest,    // Deep forest survival
        Death
    }

    private Phase _phase = Phase.Opening;

    // Simple day/night cycle used for time advancement
    private readonly string[] _timeSlots = { "Morning", "Afternoon", "Evening", "Night" };

    // === Top bar context (matches reference image) ===
    private int _day = 3;
    private string _timeOfDay = "Morning";
    private string _warIntensity = "Low";
    private string _location = "Family Apartment";
    private string _city = "Ulan-Ude, Republic of Buryatia";
    private string _season = "Early Autumn";
    private int _temperatureF = 34;   // default Fahrenheit ( Buryatia autumn nights are cold )

    // === Core stats (values from the reference) ===
    private int _suspicion = 26;
    private int _money = 10000;   // Starting money in Russian Rubles (₽)
    private int _health = 81;
    private int _morale = 63;
    private string _documents = "None";
    private string _status = "Fugitive - Deep Forest";
    private int _exposure = 38;

    // Custom death screen text (set before entering Phase.Death for specific endings)
    private string _deathLine1 = "You died.";
    private string _deathLine2 = "The war took you on the first day.";

    private int _selectedIndex;

    // Current choices (change per phase)
    private string[] _choices = Array.Empty<string>();

    // Opening scene narrative (the knock)
    private const string OpeningNarrative =
        "The knock is loud, final, and exactly what you have been dreading.\n\n" +
        "“Military Commissariat! Open up!”\n\n" +
        "Your mother’s hand finds yours under the table. Your little sister has gone completely silent in the next room. Your father stands frozen by the window. There is nowhere left to hide.";

    // Forest narrative (existing)
    private const string ForestNarrative =
        "You pushed deeper into the forest.\nThe city is far behind. First light snow\nhas begun to fall — winter is arriving\nsooner than expected. This will not be easy.";

    private const string OutsideNarrative =
        "You hit the ground hard behind the apartment block.\n" +
        "The window you escaped through is still lit.\n" +
        "No sirens yet — but the night is too quiet.\n" +
        "Every shadow could hide a patrol. Move.";

    private string _actionMessage = "";
    private float _actionMessageTimer;

    private void EnterPhase(Phase newPhase)
    {
        _phase = newPhase;
        _selectedIndex = 0;
        _actionMessage = "";
        _actionMessageTimer = 0;

        // Reset custom death text unless we're deliberately entering the death screen
        if (newPhase != Phase.Death)
        {
            _deathLine1 = "You died.";
            _deathLine2 = "The war took you on the first day.";
        }

        switch (newPhase)
        {
            case Phase.Opening:
                _choices = new[]
                {
                    "Open the door",
                    "Flee out the window",
                    "Bar the door and fight"
                };
                // Starting values for the very first moment
                _day = 0;
                _timeOfDay = "Evening";
                _warIntensity = "Rising";
                _location = "Family Apartment";
                _city = "Ulan-Ude, Republic of Buryatia";
                _status = "At Home";
                _season = "Early Autumn";
                _temperatureF = 34;   // tense night outside the apartment
                _suspicion = 4;
                _health = 96;
                _morale = 82;
                _exposure = 2;
                _money = 10000;   // Starting with 10,000 ₽
                break;

            case Phase.Forest:
                _choices = new[]
                {
                    "SET UP IMPROVED CAMP",
                    "GATHER WINTER SUPPLIES",
                    "FORTIFY SHELTER FOR COLD",
                    "CHECK RADIO / LISTEN FOR NEWS"
                };
                // The existing forest values
                _day = 3;
                _timeOfDay = "Morning";
                _warIntensity = "Low";
                _location = "Deep Forest";
                _city = "Ulan-Ude, Republic of Buryatia";
                _status = "Fugitive - Deep Forest";
                _season = "Early Autumn";
                _temperatureF = 19;   // colder the deeper you go
                // _money carries over from the Opening phase (starts at 10,000 ₽)
                break;

            case Phase.Death:
                _choices = new[] { "Try again" };
                break;

            case Phase.Outside:
                _choices = new[]
                {
                    "HIDE IN THE GARBAGE",
                    "HEAD FOR THE TRAIN TRACKS",
                    "GO TO UNCLE'S HOUSE",
                    "HEAD FOR THE PARK"
                };
                _day = 0;
                _timeOfDay = "Night";
                _warIntensity = "Rising";
                _location = "Apartment Courtyard";
                _city = "Ulan-Ude, Republic of Buryatia";
                _status = "On the Run";
                _season = "Early Autumn";
                _temperatureF = 27;   // clear cold night in the yard
                _suspicion = Clamp(_suspicion + 10);
                _exposure  = Clamp(_exposure + 18);
                _morale    = Clamp(_morale - 7);
                // money, health etc. carry over from the apartment
                break;
        }

        // Swap the background image for the new phase
        _backgroundTexture = _phase switch
        {
            Phase.Opening => _apartmentBackground,
            Phase.Outside => _outsideBackground,
            Phase.Forest  => _forestBackground,
            _             => _forestBackground
        };
    }

    /// <summary>
    /// Advances the time of day by the given number of slots.
    /// When we pass "Night", we roll over to the next day's "Morning".
    /// </summary>
    private void AdvanceTime(int steps = 1)
    {
        if (steps <= 0) return;

        int idx = Array.IndexOf(_timeSlots, _timeOfDay);
        if (idx < 0) idx = 0;

        int newIdx = (idx + steps) % _timeSlots.Length;
        _timeOfDay = _timeSlots[newIdx];

        if (newIdx < idx)   // wrapped around → new day
        {
            _day++;
        }

        // Temperature drifts with time of day (colder at night) — only outside the apartment
        if (_phase == Phase.Outside || _phase == Phase.Forest)
        {
            if (_timeOfDay == "Night")
                _temperatureF = Math.Max(-40, _temperatureF - 2);
            else if (_timeOfDay == "Morning")
                _temperatureF = Math.Min(60, _temperatureF + 1);
        }
    }

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

    private Texture2D LoadEmbeddedTexture(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        string[] candidates =
        {
            $"Conscript.img.{fileName}",
            $"Conscript.{fileName}",
            fileName,
            $"img.{fileName}"
        };

        foreach (string name in candidates)
        {
            using Stream? stream = assembly.GetManifestResourceStream(name);
            if (stream != null)
            {
                byte[] data = new byte[stream.Length];
                stream.ReadExactly(data);
                string ext = Path.GetExtension(fileName);
                if (string.IsNullOrEmpty(ext)) ext = ".png";

                Image image = Raylib.LoadImageFromMemory(ext, data);
                if (image.Width <= 0 || image.Height <= 0)
                {
                    Raylib.UnloadImage(image);
                    image = Raylib.GenImageColor(1, 1, Color.DARKGRAY);
                }

                Texture2D texture = Raylib.LoadTextureFromImage(image);
                Raylib.UnloadImage(image);
                return texture;
            }
        }

        string available = string.Join(", ", assembly.GetManifestResourceNames().Take(30));
        throw new FileNotFoundException($"Embedded image '{fileName}' not found. Tried names: {string.Join(", ", candidates)}. Available resources: {available}");
    }

    private void DrawSceneBackground(int artX, int artY, int artW, int artH)
    {
        if (_backgroundTexture.Id != 0)
        {
            Rectangle src = new Rectangle(0, 0, _backgroundTexture.Width, _backgroundTexture.Height);
            Rectangle dst = new Rectangle(artX, artY, artW, artH);
            Raylib.DrawTexturePro(_backgroundTexture, src, dst, Vector2.Zero, 0.0f, Color.WHITE);
        }
        else
        {
            Raylib.DrawRectangle(artX, artY, artW, artH, Palette.DeepNight);
        }
    }

    public void Run()
    {
        Raylib.InitWindow(_screenWidth, _screenHeight, "CONSCRIPT");
        Raylib.SetTargetFPS(60);
        Raylib.SetExitKey(KeyboardKey.KEY_NULL); // we handle ESC ourselves

        _uiFont = LoadUiFont();
        _apartmentBackground = LoadEmbeddedTexture("apartment-inside.png");
        _outsideBackground   = LoadEmbeddedTexture("apartment-outside.png");
        _forestBackground    = LoadEmbeddedTexture("trees.png");
        EnterPhase(Phase.Opening);  // EnterPhase will pick the correct background for the starting phase

        while (!ShouldExit && !Raylib.WindowShouldClose())
        {
            Update();
            Draw();
        }

        if (_apartmentBackground.Id != 0)
            Raylib.UnloadTexture(_apartmentBackground);
        if (_outsideBackground.Id != 0)
            Raylib.UnloadTexture(_outsideBackground);
        if (_forestBackground.Id != 0)
            Raylib.UnloadTexture(_forestBackground);

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

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_R))
        {
            RestartGame();
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

        // === Mouse support: hover to highlight, left-click to immediately activate ===
        Rectangle[] buttonRects = ComputeActionButtonRects();
        Vector2 mouse = Raylib.GetMousePosition();
        bool leftClicked = Raylib.IsMouseButtonPressed(MouseButton.MOUSE_LEFT_BUTTON);

        // Restart button (top right)
        UpdateRestartButtonLayout();
        _restartHovered = Raylib.CheckCollisionPointRec(mouse, _restartButtonRect);
        if (leftClicked && _restartHovered)
        {
            RestartGame();
            return;
        }

        for (int i = 0; i < buttonRects.Length; i++)
        {
            if (Raylib.CheckCollisionPointRec(mouse, buttonRects[i]))
            {
                _selectedIndex = i;                 // live hover highlight

                if (leftClicked)
                {
                    PerformChoice(i);
                    return;
                }
            }
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
        switch (_phase)
        {
            case Phase.Opening:
                HandleOpeningChoice(index);
                break;

            case Phase.Forest:
                HandleForestChoice(index);
                break;

            case Phase.Outside:
                HandleOutsideChoice(index);
                break;

            case Phase.Death:
                if (index == 0)
                {
                    EnterPhase(Phase.Opening);
                }
                break;
        }
    }

    private void HandleOpeningChoice(int index)
    {
        switch (index)
        {
            case 0: // Open the door — conscripted and dies in the war shortly after
                _deathLine1 = "You opened the door.";
                _deathLine2 = "Conscripted. Dead on the front three weeks later.";
                EnterPhase(Phase.Death);
                return;

            case 1: // Flee
                _actionMessage = "You climb out the window and drop into the yard behind the block.";
                _actionMessageTimer = 2.5f;
                AdvanceTime();   // the climb and landing take a moment
                EnterPhase(Phase.Outside);
                break;

            case 2: // Fight
                // Immediate death
                EnterPhase(Phase.Death);
                break;
        }
    }

    private void HandleForestChoice(int index)
    {
        // Existing forest actions
        switch (index)
        {
            case 0:
                _exposure = Clamp(_exposure - 11);
                _morale = Clamp(_morale + 8);
                _health = Clamp(_health - 3);
                _actionMessage = "Better shelter. The wind is less cruel tonight.";
                break;

            case 1:
                _money = Math.Max(0, _money - 180);
                _exposure = Clamp(_exposure + 7);
                _actionMessage = "You found dry branches and a few cans. Exhausting work.";
                break;

            case 2:
                _exposure = Clamp(_exposure - 18);
                _morale = Clamp(_morale - 6);
                _health = Clamp(_health + 5);
                _actionMessage = "The walls are tighter. The cold bites less.";
                break;

            case 3:
                _suspicion = Clamp(_suspicion + 5);
                _morale = Clamp(_morale + 4);
                _actionMessage = "Static. A name you know. Nothing about you yet.";
                break;
        }

        AdvanceTime();   // most actions advance the time of day
        _actionMessageTimer = ActionMessageDuration;
    }

    private void HandleOutsideChoice(int index)
    {
        switch (index)
        {
            case 0: // Hide in the garbage → you get caught and lose
                _deathLine1 = "They found you.";
                _deathLine2 = "Dragged from the garbage like an animal.";
                EnterPhase(Phase.Death);
                return;

            case 1: // Head for the train tracks — the real way out of the city
                _suspicion = Clamp(_suspicion + 5);
                _exposure = Clamp(_exposure - 5);
                _actionMessage = "You slip along the service road toward the railyard. The tracks are your way out.";
                AdvanceTime();
                EnterPhase(Phase.Forest);
                return;

            case 2: // Go to uncle's house — he turns you in
                _deathLine1 = "You went to your uncle.";
                _deathLine2 = "He called them before you could even sit down.";
                EnterPhase(Phase.Death);
                return;

            case 3: // Head for the park — another pocket of darkness in the city
                _exposure = Clamp(_exposure - 10);
                _morale = Clamp(_morale + 4);
                _health = Clamp(_health + 3);
                _actionMessage = "The park is quiet and mostly empty. You drink from a fountain and rest on a bench under the trees.";
                break;
        }

        AdvanceTime();
        _actionMessageTimer = ActionMessageDuration;
    }

    private void UpdateRestartButtonLayout()
    {
        const float size = 20f;
        const float margin = 26f;
        float x = _screenWidth - margin - size;
        float y = 10f;
        _restartButtonRect = new Rectangle(x, y, size, size);
    }

    private void RestartGame()
    {
        _actionMessage = "";
        _actionMessageTimer = 0f;
        _selectedIndex = 0;
        _deathLine1 = "You died.";
        _deathLine2 = "The war took you on the first day.";
        EnterPhase(Phase.Opening);
    }

    private void DrawRestartButton()
    {
        if (_restartButtonRect.Width <= 0) return;

        Color bg = _restartHovered
            ? new Color(58, 63, 74, 255)
            : new Color(32, 35, 42, 255);
        Color border = _restartHovered ? new Color(125, 130, 140, 255) : Palette.SubtleBorder;

        Raylib.DrawRectangleRec(_restartButtonRect, bg);
        Raylib.DrawRectangleLinesEx(_restartButtonRect, 1.0f, border);

        // Clockwise circular arrow symbol (Unicode)
        const string sym = "\u21BB";   // ↻
        float symSize = 13f;
        Vector2 m = Raylib.MeasureTextEx(_uiFont, sym, symSize, 0.6f);
        float sx = _restartButtonRect.X + (_restartButtonRect.Width - m.X) / 2f;
        float sy = _restartButtonRect.Y + (_restartButtonRect.Height - symSize) / 2f - 0.5f;
        Raylib.DrawTextEx(_uiFont, sym, new Vector2(sx, sy), symSize, 0.6f, Palette.TextPrimary);
    }

    private void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Palette.Bg);

        switch (_phase)
        {
            case Phase.Opening:
                DrawOpening();
                break;

            case Phase.Outside:
            case Phase.Forest:
                DrawTopBar();
                DrawLeftSidebar();
                DrawCinematicScene();
                DrawActionBar();
                break;

            case Phase.Death:
                DrawDeathScreen();
                break;
        }

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

        // CENTER ZONE — Day/Time (upper) + City • Specific Location (lower)
        string dayLine = $"Day {_day} - {_timeOfDay}";
        string locationLine = $"{_city} • {_location}";

        int centerX = _screenWidth / 2;
        int dayW = (int)Raylib.MeasureTextEx(font, dayLine, LayoutConstants.TopInfoFontSize, 0.8f).X;
        int locW = (int)Raylib.MeasureTextEx(font, locationLine, LayoutConstants.TopInfoFontSize, 0.8f).X;

        Raylib.DrawTextEx(font, dayLine,
            new Vector2(centerX - dayW / 2, row1Y),
            LayoutConstants.TopInfoFontSize, 0.8f, Palette.TextSecondary);

        Raylib.DrawTextEx(font, locationLine,
            new Vector2(centerX - locW / 2, row2Y),
            LayoutConstants.TopInfoFontSize, 0.8f, Palette.TextSecondary);

        // RIGHT ZONE — Season with icon (age is not shown; the character does not age)
        // Leave breathing room for the restart button in the top-right corner.
        string seasonLine = _season;

        float iconSize = 13f;
        float iconTextGap = 7f;

        int seasonW = (int)Raylib.MeasureTextEx(font, seasonLine, LayoutConstants.TopInfoFontSize, 0.8f).X;
        float totalWidth = iconSize + iconTextGap + seasonW;

        float rightEdge = _restartButtonRect.Width > 0
            ? _restartButtonRect.X - 10f
            : _screenWidth - 26f;
        float iconCenterX = rightEdge - totalWidth + iconSize / 2f;
        float iconCenterY = row1Y + 8f;   // vertically centered with the text

        DrawSeasonIcon(iconCenterX, iconCenterY, _season, iconSize);

        float textX = iconCenterX + iconSize / 2f + iconTextGap;
        Raylib.DrawTextEx(font, seasonLine,
            new Vector2(textX, row1Y),
            LayoutConstants.TopInfoFontSize, 0.8f, Palette.TextPrimary);

        // Temperature — right-aligned on the lower row (pairs with Season above)
        string tempLine = $"{_temperatureF}°F";
        int tempW = (int)Raylib.MeasureTextEx(font, tempLine, LayoutConstants.TopInfoFontSize, 0.8f).X;

        float thermoSize = 9f;
        float thermoGap = 5f;
        float thermoX = rightEdge - tempW - thermoGap - thermoSize / 2f;
        float thermoY = row2Y + 7f;

        // Minimal thermometer icon (tube + bulb)
        Color tcol = Palette.TextMuted;
        Raylib.DrawRectangle((int)(thermoX - 1), (int)(thermoY - 4), 3, 7, tcol);           // tube
        Raylib.DrawCircle((int)thermoX, (int)(thermoY + 5), 3.5f, tcol);                     // bulb

        float tempX = rightEdge - tempW;
        Raylib.DrawTextEx(font, tempLine,
            new Vector2(tempX, row2Y),
            LayoutConstants.TopInfoFontSize, 0.8f, Palette.TextSecondary);

        DrawRestartButton();
    }

    /// <summary>
    /// Draws a small, minimalist seasonal icon at the given center.
    /// Keeps everything vector-based so it matches the rest of the UI style.
    /// </summary>
    private void DrawSeasonIcon(float cx, float cy, string season, float size)
    {
        float s = size;

        if (season.Contains("Autumn", StringComparison.OrdinalIgnoreCase))
        {
            // Stylized autumn leaf (warm ochre)
            Color leafColor = new Color(165, 115, 65, 255);
            Color stemColor = new Color(90, 70, 45, 255);

            // Leaf body (pointed oval made from two triangles)
            Raylib.DrawTriangle(
                new Vector2(cx, cy - s * 0.55f),           // tip
                new Vector2(cx - s * 0.38f, cy + s * 0.35f),
                new Vector2(cx + s * 0.38f, cy + s * 0.35f),
                leafColor);

            // Side lobes
            Raylib.DrawTriangle(
                new Vector2(cx - s * 0.12f, cy - s * 0.1f),
                new Vector2(cx - s * 0.42f, cy + s * 0.15f),
                new Vector2(cx - s * 0.18f, cy + s * 0.38f),
                leafColor);

            Raylib.DrawTriangle(
                new Vector2(cx + s * 0.12f, cy - s * 0.1f),
                new Vector2(cx + s * 0.42f, cy + s * 0.15f),
                new Vector2(cx + s * 0.18f, cy + s * 0.38f),
                leafColor);

            // Central vein
            Raylib.DrawLineEx(
                new Vector2(cx, cy - s * 0.48f),
                new Vector2(cx, cy + s * 0.32f),
                1.2f, stemColor);

            // Short stem at bottom
            Raylib.DrawLineEx(
                new Vector2(cx, cy + s * 0.32f),
                new Vector2(cx, cy + s * 0.55f),
                1.5f, stemColor);
        }
        else if (season.Contains("Winter", StringComparison.OrdinalIgnoreCase))
        {
            // Simple 6-point snowflake (cold blue-white)
            Color snow = new Color(195, 200, 210, 255);
            float r = s * 0.48f;

            for (int i = 0; i < 6; i++)
            {
                float angle = i * MathF.PI / 3f;
                float dx = MathF.Cos(angle) * r;
                float dy = MathF.Sin(angle) * r;

                Raylib.DrawLineEx(
                    new Vector2(cx, cy),
                    new Vector2(cx + dx, cy + dy),
                    1.6f, snow);
            }

            // Small center dot
            Raylib.DrawCircleV(new Vector2(cx, cy), 1.8f, snow);
        }
        else if (season.Contains("Spring", StringComparison.OrdinalIgnoreCase))
        {
            // Placeholder: small sprouting bud / three lines
            Color bud = new Color(120, 145, 95, 255);
            Raylib.DrawCircleV(new Vector2(cx, cy), s * 0.22f, bud);

            // Three short upward shoots
            for (int i = -1; i <= 1; i++)
            {
                float angle = -MathF.PI / 2f + i * 0.35f;
                Raylib.DrawLineEx(
                    new Vector2(cx, cy - s * 0.15f),
                    new Vector2(cx + MathF.Cos(angle) * s * 0.42f,
                                cy + MathF.Sin(angle) * s * 0.42f - s * 0.15f),
                    1.4f, bud);
            }
        }
        else
        {
            // Summer or unknown — simple sun placeholder
            Color sun = new Color(180, 155, 80, 255);
            Raylib.DrawCircleV(new Vector2(cx, cy), s * 0.28f, sun);

            for (int i = 0; i < 8; i++)
            {
                float angle = i * MathF.PI / 4f;
                Raylib.DrawLineEx(
                    new Vector2(cx + MathF.Cos(angle) * s * 0.32f,
                                cy + MathF.Sin(angle) * s * 0.32f),
                    new Vector2(cx + MathF.Cos(angle) * s * 0.52f,
                                cy + MathF.Sin(angle) * s * 0.52f),
                    1.3f, sun);
            }
        }
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

        // War Intensity (moved here from top bar — high-level threat context)
        DrawTextStatLine(ref cy, tx, "War Intensity", _warIntensity);
        cy += 4;

        // === Clean vertical stat list ===
        // Numeric stats with bars (label + value on one line, bar underneath)
        DrawCleanStatLine(ref cy, tx, "Suspicion", _suspicion, Palette.Suspicion);
        DrawCleanStatLine(ref cy, tx, "Health", _health, Palette.Health);
        DrawCleanStatLine(ref cy, tx, "Morale", _morale, Palette.Morale);
        DrawCleanStatLine(ref cy, tx, "Exposure", _exposure, Palette.Exposure);

        cy += 6;

        // Simple text stats (same line for label + value to reduce clutter)
        DrawTextStatLine(ref cy, tx, "Money", $"{_money:N0} ₽");
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

    private string GetSceneNarrative()
    {
        return _phase switch
        {
            Phase.Opening => OpeningNarrative,
            Phase.Outside => OutsideNarrative,
            Phase.Forest  => ForestNarrative,
            _             => ForestNarrative
        };
    }

    // =====================================================================
    // CENTRAL SCENE — Background photo + atmospheric overlays + narrative card
    // Used for both the courtyard escape and the deep forest.
    // =====================================================================
    private void DrawCinematicScene()
    {
        Font font = _uiFont;

        int left = GameConstants.SceneLeft;
        int top = GameConstants.SceneTop;
        int w = GameConstants.SceneWidth;
        int h = GameConstants.SceneHeight;

        // Outer dark stage
        Raylib.DrawRectangle(left, top, w, h, Palette.SceneBg);

        // Inner breathing room for the art
        int artX = left + GameConstants.ScenePadding;
        int artY = top + GameConstants.ScenePadding;
        int artW = w - GameConstants.ScenePadding * 2;
        int artH = h - GameConstants.ScenePadding * 2;

        DrawSceneBackground(artX, artY, artW, artH);

        // Light atmospheric snow (fits both a cold night in the yard and the forest)
        int groundY = artY + (int)(artH * 0.68f);
        DrawAtmosphericSnow(artX, artY, artW, groundY, 48);

        // === Inner elegant frame + vignette for cinematic feel ===
        Raylib.DrawRectangleLines(artX + 2, artY + 2, artW - 4, artH - 4, Palette.SubtleBorder);

        // Stronger vignette on the edges
        Raylib.DrawRectangle(artX, artY, artW, 18, new Color(0, 0, 0, 70));
        Raylib.DrawRectangle(artX, artY + artH - 22, artW, 22, new Color(0, 0, 0, 80));
        Raylib.DrawRectangle(artX, artY, 22, artH, new Color(0, 0, 0, 55));
        Raylib.DrawRectangle(artX + artW - 22, artY, 22, artH, new Color(0, 0, 0, 55));

        // === Main narrative / flavor text box — clean, anchored to the right side of the image ===
        DrawRightSideNarrative(artX, artY, artW, artH, GetSceneNarrative());

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

    // =====================================================================
    // OPENING SCENE — Home apartment, the knock on the door
    // =====================================================================
    private void DrawOpening()
    {
        // We reuse the polished top bar, left stats, action bar, and right narrative card.
        // The central art is now the real apartment photo.

        DrawTopBar();
        DrawLeftSidebar();

        // Central area — apartment
        int left = GameConstants.SceneLeft;
        int top = GameConstants.SceneTop;
        int w = GameConstants.SceneWidth;
        int h = GameConstants.SceneHeight;

        Raylib.DrawRectangle(left, top, w, h, Palette.SceneBg);

        int artX = left + GameConstants.ScenePadding;
        int artY = top + GameConstants.ScenePadding;
        int artW = w - GameConstants.ScenePadding * 2;
        int artH = h - GameConstants.ScenePadding * 2;

        DrawSceneBackground(artX, artY, artW, artH);

        // The right-side narrative card
        DrawRightSideNarrative(artX, artY, artW, artH, OpeningNarrative);

        // Bottom action bar (3 choices for the opening)
        DrawActionBar();

        // Toast for "not implemented" messages
        if (_actionMessageTimer > 0f && !string.IsNullOrEmpty(_actionMessage))
        {
            Font f = _uiFont;
            float alpha = MathF.Min(1f, _actionMessageTimer / 0.8f);
            int toastW = 520;
            int toastX = artX + (artW - toastW) / 2;
            int toastY = artY + 50;

            var bg = new Color((byte)12, (byte)14, (byte)18, (byte)(alpha * 240));
            Raylib.DrawRectangle(toastX, toastY, toastW, 30, bg);
            Raylib.DrawRectangleLines(toastX, toastY, toastW, 30, new Color((byte)70, (byte)75, (byte)85, (byte)(alpha * 200)));

            var c = new Color((byte)Palette.ActionFlash.R, (byte)Palette.ActionFlash.G, (byte)Palette.ActionFlash.B, (byte)(alpha * 255));
            Raylib.DrawTextEx(f, _actionMessage, new Vector2(toastX + 16, toastY + 7), 15, 0.8f, c);
        }
    }

    // =====================================================================
    // DEATH SCREEN — simple, brutal, final
    // =====================================================================
    private void DrawDeathScreen()
    {
        // Dark, oppressive full-screen death
        Raylib.DrawRectangle(0, 0, _screenWidth, _screenHeight, new Color(5, 5, 6, 255));

        Font f = _uiFont;

        int w1 = (int)Raylib.MeasureTextEx(f, _deathLine1, 42, 0.9f).X;
        int w2 = (int)Raylib.MeasureTextEx(f, _deathLine2, 24, 0.85f).X;

        Raylib.DrawTextEx(f, _deathLine1,
            new Vector2((_screenWidth - w1) / 2, _screenHeight / 2 - 60),
            42, 0.9f, new Color(160, 70, 65, 255));

        Raylib.DrawTextEx(f, _deathLine2,
            new Vector2((_screenWidth - w2) / 2, _screenHeight / 2 - 10),
            24, 0.85f, Palette.TextSecondary);

        // The single "Try again" button is drawn by DrawActionBar (we set _choices to ["Try again"])
        DrawActionBar();

        DrawRestartButton();
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
    private void DrawRightSideNarrative(int artX, int artY, int artW, int artH, string narrativeText)
    {
        Font font = _uiFont;
        float fontSize = LayoutConstants.NarrativeLongSize;
        float spacing = 0.9f;
        int lineHeight = (int)(fontSize * 1.42f);

        int maxCardWidth = 320;
        int horizontalPadding = 18;
        int verticalPadding = 16;

        int textMaxWidth = maxCardWidth - horizontalPadding * 2;

        var (wrappedLines, textHeight) = WrapTextForBox(
            narrativeText,
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
    /// <summary>
    /// Computes the on-screen rectangles for the current action buttons.
    /// Used by both drawing and mouse hit-testing so the layout stays in one place.
    /// </summary>
    private Rectangle[] ComputeActionButtonRects()
    {
        int barY = _screenHeight - GameConstants.ActionBarHeight;
        int barH = GameConstants.ActionBarHeight;

        int count = _choices.Length;
        if (count == 0) count = 1;

        int gap = GameConstants.ActionButtonGap;
        int paddingX = 28;
        int totalGap = gap * (count - 1);
        int available = _screenWidth - paddingX * 2 - totalGap;
        int btnW = available / count;
        int btnH = barH - GameConstants.ActionBarPaddingY * 2;
        int btnY = barY + GameConstants.ActionBarPaddingY;
        int x = paddingX;

        var rects = new Rectangle[count];
        for (int i = 0; i < count; i++)
        {
            rects[i] = new Rectangle(x, btnY, btnW, btnH);
            x += btnW + gap;
        }
        return rects;
    }

    // =====================================================================
    private void DrawActionBar()
    {
        int barY = _screenHeight - GameConstants.ActionBarHeight;
        int barH = GameConstants.ActionBarHeight;

        // Bar background
        Raylib.DrawRectangle(0, barY, _screenWidth, barH, Palette.ActionBarBg);
        Raylib.DrawLine(0, barY, _screenWidth, barY, Palette.Divider);

        Font font = _uiFont;

        Rectangle[] rects = ComputeActionButtonRects();

        for (int i = 0; i < rects.Length; i++)
        {
            Rectangle r = rects[i];
            bool selected = i == _selectedIndex;
            Color bg = selected ? Palette.ButtonSelectedBg : Palette.ButtonBg;
            Color border = selected ? Palette.ButtonSelectedBorder : Palette.ButtonBorder;

            Raylib.DrawRectangleRec(r, bg);
            Raylib.DrawRectangleLinesEx(r, 1, border);

            if (selected)
            {
                Raylib.DrawRectangle((int)r.X + 1, (int)r.Y + 1, (int)r.Width - 2, 2, Palette.ButtonTopAccent);
            }

            string label = _choices[i];
            Vector2 size = Raylib.MeasureTextEx(font, label, LayoutConstants.ActionButtonFontSize, 0.85f);
            int tx = (int)(r.X + (r.Width - size.X) / 2);
            int ty = (int)(r.Y + (r.Height - size.Y) / 2) - 1;

            Raylib.DrawTextEx(font, label, new Vector2(tx, ty),
                LayoutConstants.ActionButtonFontSize, 0.85f,
                selected ? Palette.TextPrimary : Palette.TextDim);
        }
    }

    private static int Clamp(int v) => Math.Max(0, Math.Min(100, v));
}

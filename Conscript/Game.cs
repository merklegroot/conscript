using System;
using System.Collections.Generic;
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
    private Texture2D _storeBackground;
    private Texture2D _regionMapTexture;

    // Geographic bounds for region-map.png — keep in sync with img/region-map.bounds.json
    // (regenerate via: python3 scripts/generate_region_map.py)
    private const double RegionMapMinLon = 22.0;
    private const double RegionMapMaxLon = 175.0;
    private const double RegionMapMinLat = 26.0;
    private const double RegionMapMaxLat = 82.0;
    private const double UlanUdeLon = 107.584;
    private const double UlanUdeLat = 51.834;
    private const double ForestCampLon = 107.35;
    private const double ForestCampLat = 51.95;

    // Item names (inventory strings)
    private const string ItemBottledWater = "Bottled Water";
    private const string ItemEmptyBottle = "Empty Bottle of Water";
    private const string ItemTrashBags = "Trash Bags";
    private const string ItemDuctTape = "Duct Tape";
    private const string BuildTrashBagTent = "Trash Bag Tent";

    // Store item icons (embedded PNGs keyed by catalog / backpack item name)
    private readonly Dictionary<string, Texture2D> _itemIcons = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> ItemIconFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Knife"]                  = "items.knife.png",
        ["Lighter"]                = "items.lighter.png",
        ["Phone"]                  = "items.phone.png",
        [ItemBottledWater]         = "items.bottled-water.png",
        [ItemEmptyBottle]          = "items.empty-bottle.png",
        ["Loaf of Bread"]          = "items.loaf-of-bread.png",
        ["Canned Soup"]            = "items.canned-soup.png",
        [ItemTrashBags]            = "items.trash-bags.png",
        [ItemDuctTape]             = "items.duct-tape.png",
    };

    // Restart + debug buttons (top right, always available)
    private Rectangle _restartButtonRect;
    private Rectangle _debugStartButtonRect;
    private bool _restartHovered;
    private bool _debugStartHovered;

    // === Game flow ===
    private enum Phase
    {
        Opening,   // At home with family — the knock on the door
        Outside,   // In the apartment courtyard / yard immediately after climbing out the window
        Store,     // Inside a late-night convenience store / kiosk
        Forest,    // Deep forest survival
        Death
    }

    private Phase _phase = Phase.Opening;

    // Day/night cycle — eight turns per day (~3 hours each); day increments at Morning.
    private readonly string[] _timeSlots =
    {
        "Morning",
        "Late Morning",
        "Midday",
        "Afternoon",
        "Dusk",
        "Evening",
        "Night",
        "Late Night"
    };

    private const int EnergyDrainPerTimeSlot = 4;   // 8 slots × 4 ≈ former 4 slots × 8

    // Player-facing time text (clock + mood); index matches _timeSlots.
    private static readonly string[] TimeOfDayDisplay =
    {
        "6:00 AM, early morning",
        "9:00 AM, late morning",
        "12:00 PM, midday",
        "3:00 PM, afternoon",
        "6:00 PM, dusk",
        "8:00 PM, evening",
        "11:00 PM, night",
        "2:00 AM, late night"
    };

    // === Top bar context (matches reference image) ===
    private int _day = 3;
    private string _timeOfDay = "Morning";
    private string _location = "Family Apartment";
    private string _city = "Ulan-Ude, Republic of Buryatia";
    private string _season = "Early Autumn";
    private int _temperatureF = 34;   // default Fahrenheit ( Buryatia autumn nights are cold )

    // === Core stats (values from the reference) ===
    private int _money = 10000;   // Starting money in Russian Rubles (₽)
    private int _health = 81;
    private int _energy = 70;    // how rested you are (higher = better; low energy will eventually force sleep)
    private int _satiation = 63;   // how fed you are (higher = better)
    private int _hydration = 72;   // how hydrated you are (higher = better)
    private string _status = "Fugitive - Deep Forest";
    private int _comfort = 62;   // protection from the elements (higher = better)

    // Environment-driven stat changes (persistent while in that location)
    private int _envHealthDelta;
    private int _envEnergyDelta;
    private int _envSatiationDelta;
    private int _envHydrationDelta;
    private int _envComfortDelta;

    // Action-driven stat changes (temporary feedback)
    private int _actionHealthDelta;
    private int _actionEnergyDelta;
    private int _actionSatiationDelta;
    private int _actionHydrationDelta;
    private int _actionComfortDelta;
    private float _actionDeltaTimer;
    private const float ActionDeltaDisplayDuration = 2f;
    // Sergei fled wearing his winter jacket (on his body, not in the backpack grid).

    // Backpack inventory grid (prototype: 8 slots = 2×4)
    private string?[] _backpack = new string?[] { "Knife", "Lighter", "Phone", null, null, null, null, null };

    // Item interaction dialog (simple modal for now)
    private bool _showItemDialog;
    private int _dialogItemIndex = -1;
    private string _dialogItemName = "";
    private Rectangle _dialogCloseRect;
    private bool _dialogCloseHovered;
    private Rectangle _dialogActionRect;
    private bool _dialogActionHovered;
    private Rectangle _dialogPanelRect;

    // Convenience store buy menu (modal)
    private bool _showStoreBuyMenu;
    private string _storeBuyFeedback = "";
    private float _storeBuyFeedbackTimer;
    private Rectangle[] _storeBuyItemRects = new Rectangle[5];  // populated during DrawStoreBuyMenu
    private Rectangle _storeBuyPanelRect;
    private Rectangle _storeBuyCloseRect;
    private bool _storeBuyCloseHovered;

    // Build & craft dialog (modal)
    private bool _showBuildDialog;
    private bool _hasTrashBagTent;
    private Rectangle _buildSidebarButtonRect;
    private bool _buildSidebarButtonHovered;
    private Rectangle _buildPanelRect;
    private Rectangle _buildCloseRect;
    private bool _buildCloseHovered;
    private Rectangle _buildTentRowRect;
    private Rectangle _buildTentButtonRect;
    private bool _buildTentButtonHovered;
    private string _buildFeedback = "";
    private float _buildFeedbackTimer;
    private const float BuildFeedbackDuration = 2.2f;
    private const int TrashBagTentComfortBonus = 8;

    // Region map — sidebar thumbnail opens expanded view
    private bool _showRegionMap;
    private Rectangle _regionMapClickRect;
    private bool _regionMapThumbHovered;
    private Rectangle _regionMapPanelRect;
    private Rectangle _regionMapViewRect;
    private Rectangle _regionMapDrawRect;
    private Rectangle _regionMapCloseRect;
    private bool _regionMapCloseHovered;
    private Rectangle _mapZoomInRect;
    private Rectangle _mapZoomOutRect;
    private bool _mapZoomInHovered;
    private bool _mapZoomOutHovered;
    private int _mapZoomLevelIndex;
    private double _mapViewCenterLon;
    private double _mapViewCenterLat;
    private bool _mapPanning;
    private Vector2 _mapPanStartMouse;
    private double _mapPanStartCenterLon;
    private double _mapPanStartCenterLat;
    private float _expandedMapViewAspect;

    private static readonly float[] MapZoomLevels = { 0.25f, 0.5f, 0.75f, 1f, 2f, 3f, 4f, 6f, 8f, 12f, 18f };

    // Detail dialog viewport (width / height). Sidebar uses MapGeoAspect (~3.9) instead.
    private const float ExpandedMapAspect = 0.78f;

    // Cached backpack slot rectangles (updated during DrawBackpack every frame)
    private Rectangle[] _backpackSlotRects = new Rectangle[8];

    // Items available in the convenience store kiosk
    private readonly (string name, int price, int satiationDelta, int hydrationDelta, int healthDelta)[] _storeCatalog = new[]
    {
        ("Bottled Water",  65,   0, +18, +2),
        ("Loaf of Bread", 140, +22,  +2, +3),
        ("Canned Soup",  195, +28,  +8, +5),
        ("Trash Bags",    85,   0,   0,  0),
        ("Duct Tape",    120,   0,   0,  0),
    };

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

    private const string StoreNarrative =
        "The fluorescent lights are brutal after the dark yard.\n" +
        "A security camera stares from the ceiling with a dead red eye.\n" +
        "The clerk is glued to his phone behind the counter.\n" +
        "You have never felt more visible in your life.";

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
                _location = "Family Apartment";
                _city = "Ulan-Ude, Republic of Buryatia";
                _status = "At Home";
                _season = "Early Autumn";
                _temperatureF = 34;   // tense night outside the apartment
                _health = 96;
                _energy = 85;   // tense evening, but still rested from a day at home
                _satiation = 78;   // just ate at home
                _hydration = 80;
                _comfort = 98;   // warm and dry inside the apartment
                _money = 10000;   // Starting with 10,000 ₽

                // Reset backpack to starting gear (knife, lighter, phone)
                _backpack = new string?[] { "Knife", "Lighter", "Phone", null, null, null, null, null };
                _hasTrashBagTent = false;
                ClearEnvDeltas();
                ClearActionDeltas();
                break;

            case Phase.Forest:
                _choices = new[] { "GO BACK TO TOWN", "WAIT" };
                ClearEnvDeltas();
                RefreshOutdoorComfortEnvironment();
                // The existing forest values
                _day = 3;
                _timeOfDay = "Morning";
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
                    "HEAD FOR THE FOREST",
                    "GO TO UNCLE'S HOUSE",
                    "CONVENIENCE STORE",
                    "WAIT"
                };
                _day = 0;
                _timeOfDay = "Night";
                _location = "Apartment Courtyard";
                _city = "Ulan-Ude, Republic of Buryatia";
                _status = "On the Run";
                _season = "Early Autumn";
                _temperatureF = 27;   // clear cold night in the yard
                ApplyEnvironmentOutside();
                break;

            case Phase.Store:
                _choices = new[]
                {
                    "BROWSE SHELVES",
                    "LEAVE THE WAY YOU CAME",
                    "WAIT"
                };
                ApplyEnvironmentHeatedBuilding();
                _day = 0;
                _timeOfDay = "Night";
                _location = "Late-Night Kiosk";
                _city = "Ulan-Ude, Republic of Buryatia";
                _status = "On the Run";
                _season = "Early Autumn";
                _temperatureF = 24;   // slightly warmer inside
                // other stats carry over
                break;
        }

        // Swap the background image for the new phase
        _backgroundTexture = _phase switch
        {
            Phase.Opening => _apartmentBackground,
            Phase.Outside => _outsideBackground,
            Phase.Store   => _storeBackground,
            Phase.Forest  => _forestBackground,
            _             => _forestBackground
        };
    }

    /// <summary>
    /// Advances the time of day by the given number of slots.
    /// Wrapping from Late Night to Morning starts a new day.
    /// </summary>
    private void AdvanceTime(int steps = 1)
    {
        if (steps <= 0) return;

        int idx = GetTimeSlotIndex();

        int newIdx = (idx + steps) % _timeSlots.Length;
        _timeOfDay = _timeSlots[newIdx];

        if (newIdx < idx)   // wrapped around → new day
        {
            _day++;
        }

        // Actions and time passing wear you down
        ModifyStatFromAction(ref _energy, ref _actionEnergyDelta, -EnergyDrainPerTimeSlot * steps);

        // Temperature drifts with time of day (colder at night) — only outside the apartment
        if (_phase == Phase.Outside || _phase == Phase.Forest)
        {
            if (IsNightTimeSlot())
                _temperatureF = Math.Max(-40, _temperatureF - 2);
            else if (IsMorningTimeSlot())
                _temperatureF = Math.Min(60, _temperatureF + 1);

            if (_phase == Phase.Outside)
                RefreshOutdoorComfortEnvironment();
        }
    }

    private int GetTimeSlotIndex()
    {
        int idx = Array.IndexOf(_timeSlots, _timeOfDay);
        return idx < 0 ? 0 : idx;
    }

    private bool IsNightTimeSlot() =>
        _timeOfDay is "Night" or "Late Night";

    private bool IsMorningTimeSlot() =>
        _timeOfDay is "Morning" or "Late Morning";

    private string GetTimeOfDayDisplay()
    {
        int idx = GetTimeSlotIndex();
        return idx < TimeOfDayDisplay.Length ? TimeOfDayDisplay[idx] : _timeOfDay;
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

        // Comprehensive character set for full UI support:
        // - Basic Latin + common punctuation (including ' " ° … – — etc.)
        // - Rouble symbol ₽
        // - Full Cyrillic (Russian alphabet, including Ё/ё)
        // - Common symbols used in the game (↻ restart icon, etc.)
        // We must use LoadFontEx with an explicit glyph list; passing null/0 only loads
        // a tiny default set (ASCII ~95 chars), which is why ' , ₽ and Cyrillic were missing.
        const string chars =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
            " !\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~°©®™…–—•·‘’“”«»₽" +
            "абвгдеёжзийклмнопрстуфхцчшщъыьэюяАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ" +
            "\u21BB" + // ↻ (clockwise arrow, used for restart button)
            "\u25B2\u25BC"; // ▲▼ (stat trend arrows)

        int[] codepoints = new int[chars.Length];
        for (int i = 0; i < chars.Length; i++)
            codepoints[i] = chars[i];

        foreach (string path in candidates)
        {
            if (File.Exists(path))
            {
                // 40 is the base pixel size; we control actual size via DrawTextEx fontSize param
                return Raylib.LoadFontEx(path, 40, codepoints, codepoints.Length);
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

    private void LoadItemIcons()
    {
        foreach (var (itemName, fileName) in ItemIconFiles)
            _itemIcons[itemName] = LoadEmbeddedTexture(fileName);
    }

    private void UnloadItemIcons()
    {
        foreach (Texture2D tex in _itemIcons.Values)
        {
            if (tex.Id != 0)
                Raylib.UnloadTexture(tex);
        }
        _itemIcons.Clear();
    }

    private void DrawItemIcon(string itemName, Rectangle dest, Color tint)
    {
        if (!_itemIcons.TryGetValue(itemName, out Texture2D tex) || tex.Id == 0)
            return;

        Rectangle src = new Rectangle(0, 0, tex.Width, tex.Height);
        Raylib.DrawTexturePro(tex, src, dest, Vector2.Zero, 0f, tint);
    }

    private bool IsOutdoorPhase() =>
        _phase is Phase.Outside or Phase.Forest;

    /// <summary>
    /// Multiplicative tint for outdoor background photos by time of day.
    /// </summary>
    private Color GetOutdoorTimeOfDayTint() =>
        GetTimeSlotIndex() switch
        {
            0 => new Color(195, 208, 228, 255),   // Morning — cool dawn
            1 => new Color(215, 220, 232, 255),   // Late Morning
            2 => new Color(255, 252, 242, 255),   // Midday — brightest
            3 => new Color(255, 248, 235, 255),   // Afternoon
            4 => new Color(235, 200, 160, 255),   // Dusk
            5 => new Color(218, 178, 138, 255),   // Evening
            6 => new Color(130, 138, 165, 255),   // Night
            7 => new Color(88, 98, 128, 255),     // Late Night — darkest
            _ => Color.WHITE
        };

    /// <summary>
    /// Extra color wash on top of the tinted photo (mostly for dusk/night depth).
    /// </summary>
    private Color GetOutdoorTimeOfDayOverlay() =>
        GetTimeSlotIndex() switch
        {
            0 => new Color(180, 200, 230, 18),
            1 => new Color(160, 185, 220, 10),
            2 => new Color(0, 0, 0, 0),
            3 => new Color(0, 0, 0, 0),
            4 => new Color(35, 22, 8, 30),
            5 => new Color(40, 25, 10, 45),
            6 => new Color(8, 12, 28, 55),
            7 => new Color(8, 12, 28, 72),
            _ => new Color(0, 0, 0, 0)
        };

    private void DrawSceneBackground(int artX, int artY, int artW, int artH)
    {
        if (_backgroundTexture.Id != 0)
        {
            Color tint = IsOutdoorPhase() ? GetOutdoorTimeOfDayTint() : Color.WHITE;
            Rectangle src = new Rectangle(0, 0, _backgroundTexture.Width, _backgroundTexture.Height);
            Rectangle dst = new Rectangle(artX, artY, artW, artH);
            Raylib.DrawTexturePro(_backgroundTexture, src, dst, Vector2.Zero, 0.0f, tint);

            if (IsOutdoorPhase())
            {
                Color overlay = GetOutdoorTimeOfDayOverlay();
                if (overlay.A > 0)
                    Raylib.DrawRectangle(artX, artY, artW, artH, overlay);
            }
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
        _storeBackground     = LoadEmbeddedTexture("store.png");  // dedicated store interior photo (bright fluorescent kiosk)
        _regionMapTexture    = LoadEmbeddedTexture("region-map.png");
        LoadItemIcons();
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
        if (_storeBackground.Id != 0)
            Raylib.UnloadTexture(_storeBackground);
        if (_regionMapTexture.Id != 0)
            Raylib.UnloadTexture(_regionMapTexture);
        UnloadItemIcons();

        Raylib.CloseWindow();
    }

    private void Update()
    {
        float dt = Raylib.GetFrameTime();

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE) || Raylib.IsKeyPressed(KeyboardKey.KEY_Q))
        {
            if (_showItemDialog)
            {
                CloseItemDialog();
                return;
            }
            if (_showStoreBuyMenu)
            {
                _showStoreBuyMenu = false;
                _storeBuyFeedback = "";
                return;
            }
            if (_showRegionMap)
            {
                CloseRegionMap();
                return;
            }
            if (_showBuildDialog)
            {
                CloseBuildDialog();
                return;
            }
            _shouldExit = true;
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_R))
        {
            RestartGame();
            return;
        }

        // Horizontal navigation for bottom action buttons
        if (!_showRegionMap && !_showItemDialog && !_showStoreBuyMenu && !_showBuildDialog)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_RIGHT) || Raylib.IsKeyPressed(KeyboardKey.KEY_D))
            {
                _selectedIndex = (_selectedIndex + 1) % _choices.Length;
            }
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_LEFT) || Raylib.IsKeyPressed(KeyboardKey.KEY_A))
            {
                _selectedIndex = (_selectedIndex - 1 + _choices.Length) % _choices.Length;
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ENTER) || Raylib.IsKeyPressed(KeyboardKey.KEY_SPACE))
        {
            if (_showRegionMap)
            {
                CloseRegionMap();
            }
            else if (_showItemDialog)
            {
                CloseItemDialog();
            }
            else if (_showBuildDialog)
            {
                CloseBuildDialog();
            }
            else
            {
                PerformChoice(_selectedIndex);
            }
        }

        // === Mouse support: hover to highlight, left-click to immediately activate ===
        Rectangle[] buttonRects = ComputeActionButtonRects();
        Vector2 mouse = Raylib.GetMousePosition();
        bool leftClicked = Raylib.IsMouseButtonPressed(MouseButton.MOUSE_LEFT_BUTTON);

        // Top-right utility buttons
        UpdateTopRightButtonsLayout();
        _restartHovered = Raylib.CheckCollisionPointRec(mouse, _restartButtonRect);
        _debugStartHovered = Raylib.CheckCollisionPointRec(mouse, _debugStartButtonRect);
        if (leftClicked && _restartHovered)
        {
            RestartGame();
            return;
        }
        if (leftClicked && _debugStartHovered)
        {
            DebugStartGame();
            return;
        }

        // === Expanded region map (modal) ===
        if (_showRegionMap)
        {
            SyncExpandedMapLayout();
            ComputeMapZoomButtonRects(_regionMapDrawRect, out _mapZoomInRect, out _mapZoomOutRect);

            _regionMapCloseHovered = Raylib.CheckCollisionPointRec(mouse, _regionMapCloseRect);
            _mapZoomInHovered = Raylib.CheckCollisionPointRec(mouse, _mapZoomInRect);
            _mapZoomOutHovered = Raylib.CheckCollisionPointRec(mouse, _mapZoomOutRect);
            bool overMap = _regionMapDrawRect.Width > 0 &&
                Raylib.CheckCollisionPointRec(mouse, _regionMapDrawRect);

            if (leftClicked && _mapZoomInHovered && _mapZoomLevelIndex < MapZoomLevels.Length - 1)
            {
                ChangeMapZoom(1, _regionMapDrawRect);
                return;
            }

            if (leftClicked && _mapZoomOutHovered && _mapZoomLevelIndex > 0)
            {
                ChangeMapZoom(-1, _regionMapDrawRect);
                return;
            }

            if (leftClicked && overMap && !_regionMapCloseHovered && !_mapZoomInHovered && !_mapZoomOutHovered)
            {
                _mapPanning = true;
                _mapPanStartMouse = mouse;
                _mapPanStartCenterLon = _mapViewCenterLon;
                _mapPanStartCenterLat = _mapViewCenterLat;
            }

            if (_mapPanning)
            {
                if (!Raylib.IsMouseButtonDown(MouseButton.MOUSE_LEFT_BUTTON))
                    _mapPanning = false;
                else
                {
                    GetMapViewBounds(out double vMinLon, out double vMaxLon, out double vMinLat, out double vMaxLat);
                    double lonSpan = vMaxLon - vMinLon;
                    double latSpan = vMaxLat - vMinLat;
                    Vector2 delta = mouse - _mapPanStartMouse;
                    _mapViewCenterLon = _mapPanStartCenterLon - delta.X / _regionMapDrawRect.Width * lonSpan;
                    _mapViewCenterLat = _mapPanStartCenterLat + delta.Y / _regionMapDrawRect.Height * latSpan;
                    ClampMapViewCenter();
                }
            }

            if (leftClicked && _regionMapCloseHovered)
            {
                CloseRegionMap();
                return;
            }

            if (leftClicked && !Raylib.CheckCollisionPointRec(mouse, _regionMapPanelRect))
            {
                CloseRegionMap();
                return;
            }
        }
        else
        {
            _mapPanning = false;
        }

        // === Build dialog (modal) ===
        if (_showBuildDialog)
        {
            _buildCloseHovered = Raylib.CheckCollisionPointRec(mouse, _buildCloseRect);
            bool canBuildTent = CanBuildTrashBagTent(out _);
            _buildTentButtonHovered = canBuildTent &&
                Raylib.CheckCollisionPointRec(mouse, _buildTentButtonRect);

            if (leftClicked && _buildTentButtonHovered)
            {
                TryBuildTrashBagTent();
                return;
            }

            if (leftClicked && Raylib.CheckCollisionPointRec(mouse, _buildTentRowRect))
            {
                TryBuildTrashBagTent();
                return;
            }

            if (leftClicked && _buildCloseHovered)
            {
                CloseBuildDialog();
                return;
            }

            if (leftClicked && !Raylib.CheckCollisionPointRec(mouse, _buildPanelRect))
            {
                CloseBuildDialog();
                return;
            }
        }

        // === Item dialog (highest priority when visible) ===
        if (_showItemDialog)
        {
            bool canDrink = CanDrinkItem(_dialogItemName);
            _dialogActionHovered = canDrink && Raylib.CheckCollisionPointRec(mouse, _dialogActionRect);
            _dialogCloseHovered = Raylib.CheckCollisionPointRec(mouse, _dialogCloseRect);

            if (leftClicked && _dialogActionHovered)
            {
                TryDrinkBottledWater();
                return;
            }
            if (leftClicked && _dialogCloseHovered)
            {
                CloseItemDialog();
                return;
            }
            // Clicking the dark overlay outside the panel closes the dialog
            if (leftClicked && !Raylib.CheckCollisionPointRec(mouse, _dialogPanelRect))
            {
                CloseItemDialog();
                return;
            }
        }

        if (_showStoreBuyMenu)
        {
            _storeBuyCloseHovered = Raylib.CheckCollisionPointRec(mouse, _storeBuyCloseRect);
        }

        if (!_showItemDialog && !_showStoreBuyMenu && !_showRegionMap && !_showBuildDialog)
        {
            _regionMapThumbHovered = _regionMapClickRect.Width > 0 &&
                Raylib.CheckCollisionPointRec(mouse, _regionMapClickRect);
            _buildSidebarButtonHovered = _buildSidebarButtonRect.Width > 0 &&
                Raylib.CheckCollisionPointRec(mouse, _buildSidebarButtonRect);

            if (leftClicked && _buildSidebarButtonHovered)
            {
                OpenBuildDialog();
                return;
            }

            if (leftClicked && _regionMapThumbHovered)
            {
                OpenRegionMap();
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

            // === Backpack item click (opens simple interaction dialog) ===
            for (int i = 0; i < _backpackSlotRects.Length; i++)
            {
                if (!string.IsNullOrEmpty(_backpack[i]) &&
                    Raylib.CheckCollisionPointRec(mouse, _backpackSlotRects[i]))
                {
                    if (leftClicked)
                    {
                        OpenItemDialog(i);
                        return;
                    }
                }
            }
        }

        // === Store buy menu input (when open) ===
        if (_showStoreBuyMenu)
        {
            // Mouse clicks on item rows
            for (int i = 0; i < _storeCatalog.Length; i++)
            {
                if (Raylib.CheckCollisionPointRec(mouse, _storeBuyItemRects[i]))
                {
                    if (leftClicked)
                    {
                        TryBuyStoreItem(i);
                        return;
                    }
                }
            }

            // Close button
            if (leftClicked && Raylib.CheckCollisionPointRec(mouse, _storeBuyCloseRect))
            {
                _showStoreBuyMenu = false;
                _storeBuyFeedback = "";
                return;
            }

            // Click on the overlay (outside the panel) closes the menu
            if (leftClicked && !Raylib.CheckCollisionPointRec(mouse, _storeBuyPanelRect))
            {
                _showStoreBuyMenu = false;
                _storeBuyFeedback = "";
                return;
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

        if (_actionDeltaTimer > 0f)
        {
            _actionDeltaTimer -= dt;
            if (_actionDeltaTimer <= 0f)
                ClearActionDeltas();
        }

        // Store buy menu feedback timer
        if (_showStoreBuyMenu && _storeBuyFeedbackTimer > 0f)
        {
            _storeBuyFeedbackTimer -= dt;
            if (_storeBuyFeedbackTimer <= 0f)
            {
                _storeBuyFeedback = "";
            }
        }

        if (_showBuildDialog && _buildFeedbackTimer > 0f)
        {
            _buildFeedbackTimer -= dt;
            if (_buildFeedbackTimer <= 0f)
                _buildFeedback = "";
        }

        // === Update mouse cursor to indicate clickable elements ===
        bool overClickable = false;

        // Top-right utility buttons (always available)
        if (Raylib.CheckCollisionPointRec(mouse, _restartButtonRect) ||
            Raylib.CheckCollisionPointRec(mouse, _debugStartButtonRect))
            overClickable = true;

        if (!_showItemDialog && !_showStoreBuyMenu && !_showRegionMap && !_showBuildDialog)
        {
            if (_regionMapThumbHovered || _buildSidebarButtonHovered)
                overClickable = true;

            // Bottom action buttons
            for (int i = 0; i < buttonRects.Length; i++)
            {
                if (Raylib.CheckCollisionPointRec(mouse, buttonRects[i]))
                {
                    overClickable = true;
                    break;
                }
            }

            // Backpack slots that contain items
            for (int i = 0; i < _backpackSlotRects.Length; i++)
            {
                if (!string.IsNullOrEmpty(_backpack[i]) &&
                    Raylib.CheckCollisionPointRec(mouse, _backpackSlotRects[i]))
                {
                    overClickable = true;
                    break;
                }
            }
        }

        if (_showRegionMap)
        {
            if (_regionMapCloseHovered || _mapZoomInHovered || _mapZoomOutHovered ||
                Raylib.CheckCollisionPointRec(mouse, _regionMapPanelRect))
            {
                overClickable = true;
            }
        }

        // Item dialog: action + close buttons, or overlay dismiss
        if (_showItemDialog)
        {
            if (Raylib.CheckCollisionPointRec(mouse, _dialogCloseRect) ||
                Raylib.CheckCollisionPointRec(mouse, _dialogActionRect) ||
                !Raylib.CheckCollisionPointRec(mouse, _dialogPanelRect))
            {
                overClickable = true;
            }
        }

        // Store buy menu: rows + close button + overlay
        if (_showStoreBuyMenu)
        {
            if (Raylib.CheckCollisionPointRec(mouse, _storeBuyCloseRect) ||
                Raylib.CheckCollisionPointRec(mouse, _storeBuyPanelRect))
            {
                overClickable = true;
            }
            else
            {
                // Clicking the overlay closes the menu
                overClickable = true;
            }

            for (int i = 0; i < _storeBuyItemRects.Length; i++)
            {
                if (Raylib.CheckCollisionPointRec(mouse, _storeBuyItemRects[i]))
                {
                    overClickable = true;
                    break;
                }
            }
        }

        // Build dialog: close button + overlay
        if (_showBuildDialog)
        {
            if (_buildCloseHovered || _buildTentButtonHovered ||
                Raylib.CheckCollisionPointRec(mouse, _buildTentRowRect) ||
                !Raylib.CheckCollisionPointRec(mouse, _buildPanelRect))
                overClickable = true;
        }

        Raylib.SetMouseCursor(overClickable
            ? MouseCursor.MOUSE_CURSOR_POINTING_HAND
            : MouseCursor.MOUSE_CURSOR_DEFAULT);
    }

    private void PerformChoice(int index)
    {
        ClearActionDeltas();
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

            case Phase.Store:
                HandleStoreChoice(index);
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
        switch (index)
        {
            case 0:
                EnterPhase(Phase.Outside);
                break;
            case 1:
                PerformIdle();
                break;
        }
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

            case 1: // Head for the forest
                ModifyStatFromAction(ref _comfort, ref _actionComfortDelta, -5);   // out in the cold, moving toward the tree line
                _actionMessage = "You slip away from the blocks and into the dark pines at the edge of town.";
                AdvanceTime();
                ApplyEnvironmentOnAction();
                EnterPhase(Phase.Forest);
                return;

            case 2: // Go to uncle's house — he turns you in
                _deathLine1 = "You went to your uncle.";
                _deathLine2 = "He called them before you could even sit down.";
                EnterPhase(Phase.Death);
                return;

            case 3: // Convenience store — step inside the bright kiosk
                ApplyEnvironmentOnAction();
                _actionMessage = "You push through the heavy glass door into the harsh light.";
                _actionMessageTimer = 1.8f;
                EnterPhase(Phase.Store);
                return;

            case 4: // Wait in the courtyard
                PerformIdle();
                return;
        }

        AdvanceTime();
        ApplyEnvironmentOnAction();
        _actionMessageTimer = ActionMessageDuration;
    }

    private void HandleStoreChoice(int index)
    {
        switch (index)
        {
            case 0: // Browse shelves → open the buy menu
                _showStoreBuyMenu = true;
                _storeBuyFeedback = "";
                _storeBuyFeedbackTimer = 0;
                return;   // do not advance time or close the store phase yet

            case 1: // Leave the way you came
                _actionMessage = "You push back out into the cold dark yard.";
                AdvanceTime();
                EnterPhase(Phase.Outside);
                return;

            case 2: // Wait inside the kiosk
                PerformIdle();
                return;
        }

        AdvanceTime();
        _actionMessageTimer = ActionMessageDuration;
    }

    private void PerformIdle()
    {
        switch (_phase)
        {
            case Phase.Outside:
                _actionMessage = "You press yourself into the shadows and listen. Nothing moves.";
                ApplyEnvironmentOnAction();
                break;
            case Phase.Store:
                _actionMessage = "You linger by the shelves, pretending to read labels.";
                break;
            case Phase.Forest:
                _actionMessage = "You stay low and motionless. The forest is quiet.";
                break;
            default:
                return;
        }

        AdvanceTime();
        // Waiting costs time but recovers a little energy (net gain after AdvanceTime's drain).
        ModifyStatFromAction(ref _energy, ref _actionEnergyDelta, 12);
        _actionMessageTimer = ActionMessageDuration;
    }

    private void UpdateTopRightButtonsLayout()
    {
        const float size = 20f;
        const float gap = 6f;
        const float margin = 26f;
        float x = _screenWidth - margin - size;
        _restartButtonRect = new Rectangle(x, 10f, size, size);
        _debugStartButtonRect = new Rectangle(x, 10f + size + gap, size, size);
    }

    private void RestartGame()
    {
        _actionMessage = "";
        _actionMessageTimer = 0f;
        _selectedIndex = 0;
        _showItemDialog = false;
        _showStoreBuyMenu = false;
        CloseRegionMap();
        CloseBuildDialog();
        _hasTrashBagTent = false;
        _buildFeedback = "";
        _storeBuyFeedback = "";
        _deathLine1 = "You died.";
        _deathLine2 = "The war took you on the first day.";
        EnterPhase(Phase.Opening);
    }

    /// <summary>
    /// Jump to a reproducible debug snapshot: late-night kiosk after fleeing the apartment.
    /// Resets stats, money, and backpack to a clean starting point for store testing.
    /// </summary>
    private void DebugStartGame()
    {
        _showItemDialog = false;
        _showStoreBuyMenu = false;
        CloseRegionMap();
        CloseBuildDialog();
        _hasTrashBagTent = false;
        _buildFeedback = "";
        _storeBuyFeedback = "";
        _storeBuyFeedbackTimer = 0f;
        _deathLine1 = "You died.";
        _deathLine2 = "The war took you on the first day.";

        _health = 96;
        _energy = 58;       // exhausted after fleeing the apartment
        _satiation = 69;    // ate at home, then lost some fleeing the courtyard
        _hydration = 76;
        _comfort = 80;      // warm kiosk after the cold yard
        _money = 10000;
        _backpack = new string?[] { "Knife", "Lighter", "Phone", null, null, null, null, null };
        ClearEnvDeltas();
        ClearActionDeltas();

        EnterPhase(Phase.Store);
    }

    private void OpenItemDialog(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _backpack.Length) return;
        string? item = _backpack[slotIndex];
        if (string.IsNullOrEmpty(item)) return;

        _dialogItemIndex = slotIndex;
        _dialogItemName = item;
        _showItemDialog = true;
        _dialogCloseHovered = false;
        _dialogActionHovered = false;
    }

    private void CloseItemDialog()
    {
        _showItemDialog = false;
        _dialogItemIndex = -1;
        _dialogItemName = "";
        _dialogActionHovered = false;
    }

    private void OpenBuildDialog()
    {
        _showBuildDialog = true;
        _buildCloseHovered = false;
        _buildTentButtonHovered = false;
    }

    private void CloseBuildDialog()
    {
        _showBuildDialog = false;
        _buildCloseHovered = false;
        _buildTentButtonHovered = false;
    }

    private static bool IsOutdoorsPhase(Phase phase) =>
        phase is Phase.Outside or Phase.Forest;

    private bool HasBackpackItem(string itemName) =>
        _backpack.Any(i => string.Equals(i, itemName, StringComparison.OrdinalIgnoreCase));

    private bool TryConsumeBackpackItem(string itemName)
    {
        for (int i = 0; i < _backpack.Length; i++)
        {
            if (string.Equals(_backpack[i], itemName, StringComparison.OrdinalIgnoreCase))
            {
                _backpack[i] = null;
                return true;
            }
        }

        return false;
    }

    private bool CanBuildTrashBagTent(out string reason)
    {
        if (_hasTrashBagTent)
        {
            reason = "Already built.";
            return false;
        }

        if (!IsOutdoorsPhase(_phase))
        {
            reason = "Must be outdoors.";
            return false;
        }

        if (!HasBackpackItem(ItemTrashBags))
        {
            reason = "Need trash bags.";
            return false;
        }

        if (!HasBackpackItem(ItemDuctTape))
        {
            reason = "Need duct tape.";
            return false;
        }

        reason = "";
        return true;
    }

    private void TryBuildTrashBagTent()
    {
        if (!CanBuildTrashBagTent(out string reason))
        {
            _buildFeedback = reason;
            _buildFeedbackTimer = BuildFeedbackDuration;
            return;
        }

        TryConsumeBackpackItem(ItemTrashBags);
        TryConsumeBackpackItem(ItemDuctTape);
        _hasTrashBagTent = true;
        RefreshOutdoorComfortEnvironment();

        _buildFeedback = "You rig a crude shelter from plastic and tape.";
        _buildFeedbackTimer = BuildFeedbackDuration;
        _actionMessage = "Trash bag tent pitched. A little warmer out here.";
        _actionMessageTimer = ActionMessageDuration;
    }

    private static bool CanDrinkItem(string itemName) =>
        string.Equals(itemName, ItemBottledWater, StringComparison.OrdinalIgnoreCase);

    private void TryDrinkBottledWater()
    {
        if (_dialogItemIndex < 0 || _dialogItemIndex >= _backpack.Length) return;
        if (!CanDrinkItem(_backpack[_dialogItemIndex] ?? "")) return;

        _backpack[_dialogItemIndex] = ItemEmptyBottle;
        ClearActionDeltas();
        SetStatFromAction(ref _hydration, ref _actionHydrationDelta, 100);
        _actionMessage = "You drink the water. Fully hydrated.";
        _actionMessageTimer = ActionMessageDuration;
        CloseItemDialog();
    }

    private bool TryAddToBackpack(string item)
    {
        for (int i = 0; i < _backpack.Length; i++)
        {
            if (string.IsNullOrEmpty(_backpack[i]))
            {
                _backpack[i] = item;
                return true;
            }
        }
        return false; // backpack full
    }

    private void TryBuyStoreItem(int index)
    {
        if (index < 0 || index >= _storeCatalog.Length) return;

        var (name, price, satiationDelta, hydrationDelta, healthDelta) = _storeCatalog[index];

        if (_money < price)
        {
            _storeBuyFeedback = "Not enough money.";
            _storeBuyFeedbackTimer = 1.6f;
            return;
        }

        if (!TryAddToBackpack(name))
        {
            _storeBuyFeedback = "Backpack is full.";
            _storeBuyFeedbackTimer = 1.6f;
            return;
        }

        _money -= price;
        ClearActionDeltas();
        ModifyStatFromAction(ref _satiation, ref _actionSatiationDelta, satiationDelta);
        ModifyStatFromAction(ref _hydration, ref _actionHydrationDelta, hydrationDelta);
        ModifyStatFromAction(ref _health, ref _actionHealthDelta, healthDelta);

        _storeBuyFeedback = $"Bought {name}";
        _storeBuyFeedbackTimer = 1.2f;
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
        float symSize = 16f;
        Vector2 m = Raylib.MeasureTextEx(_uiFont, sym, symSize, 0.6f);
        float sx = _restartButtonRect.X + (_restartButtonRect.Width - m.X) / 2f;
        float sy = _restartButtonRect.Y + (_restartButtonRect.Height - symSize) / 2f - 0.5f;
        Raylib.DrawTextEx(_uiFont, sym, new Vector2(sx, sy), symSize, 0.6f, Palette.TextPrimary);
    }

    private void DrawDebugStartButton()
    {
        if (_debugStartButtonRect.Width <= 0) return;

        Color bg = _debugStartHovered
            ? new Color(58, 63, 74, 255)
            : new Color(32, 35, 42, 255);
        Color border = _debugStartHovered ? new Color(125, 130, 140, 255) : Palette.SubtleBorder;

        Raylib.DrawRectangleRec(_debugStartButtonRect, bg);
        Raylib.DrawRectangleLinesEx(_debugStartButtonRect, 1.0f, border);

        const string label = "DBG";
        float labelSize = 10f;
        Vector2 m = Raylib.MeasureTextEx(_uiFont, label, labelSize, 0.5f);
        float lx = _debugStartButtonRect.X + (_debugStartButtonRect.Width - m.X) / 2f;
        float ly = _debugStartButtonRect.Y + (_debugStartButtonRect.Height - labelSize) / 2f - 0.5f;
        Raylib.DrawTextEx(_uiFont, label, new Vector2(lx, ly), labelSize, 0.5f, Palette.TextSecondary);
    }

    private void DrawTopRightButtons()
    {
        DrawRestartButton();
        DrawDebugStartButton();
    }

    // =====================================================================
    // ITEM DIALOG (modal) — use / examine / close per item
    // =====================================================================
    private void DrawItemDialog()
    {
        int screenW = _screenWidth;
        int screenH = _screenHeight;

        // Dark overlay
        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 170));

        bool canDrink = CanDrinkItem(_dialogItemName);

        // Centered dialog panel
        int panelW = 380;
        int panelH = 240;
        int panelX = (screenW - panelW) / 2;
        int panelY = (screenH - panelH) / 2 - 20;

        _dialogPanelRect = new Rectangle(panelX, panelY, panelW, panelH);

        // Card background + border (matches existing card style)
        Raylib.DrawRectangle(panelX, panelY, panelW, panelH, Palette.CardBg);
        Raylib.DrawRectangleLines(panelX, panelY, panelW, panelH, Palette.CardBorder);

        Font font = _uiFont;

        // Item icon
        const int iconSize = 56;
        int iconX = panelX + (panelW - iconSize) / 2;
        int iconY = panelY + 16;
        Raylib.DrawRectangle(iconX - 2, iconY - 2, iconSize + 4, iconSize + 4, new Color(22, 20, 17, 255));
        Raylib.DrawRectangleLines(iconX - 2, iconY - 2, iconSize + 4, iconSize + 4, Palette.SubtleBorder);
        DrawItemIcon(_dialogItemName, new Rectangle(iconX, iconY, iconSize, iconSize), Color.WHITE);

        // Item name as title
        string title = _dialogItemName.ToUpperInvariant();
        int titleSize = 28;
        int titleW = (int)Raylib.MeasureTextEx(font, title, titleSize, 0.8f).X;
        Raylib.DrawTextEx(font, title,
            new Vector2(panelX + (panelW - titleW) / 2, panelY + 82),
            titleSize, 0.8f, Palette.TextPrimary);

        // Subtle separator
        Raylib.DrawLine(panelX + 40, panelY + 112, panelX + panelW - 40, panelY + 112, Palette.SubtleBorder);

        string body = canDrink
            ? "Drink it to fully restore hydration."
            : string.Equals(_dialogItemName, ItemEmptyBottle, StringComparison.OrdinalIgnoreCase)
                ? "An empty plastic bottle. Nothing left to drink."
                : "No special actions defined for this item yet.";
        int bodySize = 20;
        int bodyW = (int)Raylib.MeasureTextEx(font, body, bodySize, 0.6f).X;
        Raylib.DrawTextEx(font, body,
            new Vector2(panelX + (panelW - bodyW) / 2, panelY + 128),
            bodySize, 0.6f, Palette.TextSecondary);

        int btnW = canDrink ? 108 : 120;
        int btnH = 36;
        int btnY = panelY + panelH - 52;

        if (canDrink)
        {
            int gap = 14;
            int totalW = btnW * 2 + gap;
            int startX = panelX + (panelW - totalW) / 2;
            _dialogActionRect = new Rectangle(startX, btnY, btnW, btnH);
            _dialogCloseRect = new Rectangle(startX + btnW + gap, btnY, btnW, btnH);

            DrawDialogButton(_dialogActionRect, "DRINK", _dialogActionHovered, font);
            DrawDialogButton(_dialogCloseRect, "CLOSE", _dialogCloseHovered, font);
        }
        else
        {
            _dialogActionRect = new Rectangle(0, 0, 0, 0);
            int btnX = panelX + (panelW - btnW) / 2;
            _dialogCloseRect = new Rectangle(btnX, btnY, btnW, btnH);
            DrawDialogButton(_dialogCloseRect, "CLOSE", _dialogCloseHovered, font);
        }
    }

    private void DrawDialogButton(Rectangle rect, string label, bool hovered, Font font)
    {
        Color btnBg = hovered ? Palette.ButtonSelectedBg : Palette.ButtonBg;
        Color btnBorder = hovered ? Palette.ButtonSelectedBorder : Palette.ButtonBorder;

        Raylib.DrawRectangleRec(rect, btnBg);
        Raylib.DrawRectangleLinesEx(rect, 1.5f, btnBorder);
        Raylib.DrawRectangle((int)rect.X + 2, (int)rect.Y + 2, (int)rect.Width - 4, 2, Palette.ButtonTopAccent);

        int labelSize = 22;
        int labelW = (int)Raylib.MeasureTextEx(font, label, labelSize, 0.7f).X;
        Raylib.DrawTextEx(font, label,
            new Vector2(rect.X + (rect.Width - labelW) / 2, rect.Y + 9),
            labelSize, 0.7f, Palette.TextPrimary);
    }

    // =====================================================================
    // BUILD DIALOG (modal) — crafting and construction
    // =====================================================================
    private void DrawBuildDialog()
    {
        int screenW = _screenWidth;
        int screenH = _screenHeight;

        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 170));

        int panelW = 460;
        int panelH = 320;
        int panelX = (screenW - panelW) / 2;
        int panelY = (screenH - panelH) / 2 - 10;

        _buildPanelRect = new Rectangle(panelX, panelY, panelW, panelH);

        Raylib.DrawRectangle(panelX, panelY, panelW, panelH, Palette.CardBg);
        Raylib.DrawRectangleLines(panelX, panelY, panelW, panelH, Palette.CardBorder);

        Font font = _uiFont;

        Raylib.DrawTextEx(font, "BUILD & CRAFT",
            new Vector2(panelX + 22, panelY + 18), 25, 0.75f, Palette.TextPrimary);

        Raylib.DrawLine(panelX + 22, panelY + 46, panelX + panelW - 22, panelY + 46, Palette.SubtleBorder);

        string subtitle = "Construct shelter and tools from what you carry.";
        Raylib.DrawTextEx(font, subtitle,
            new Vector2(panelX + 22, panelY + 58), 18, 0.6f, Palette.TextSecondary);

        int rowY = panelY + 88;
        int rowH = 56;
        int rowX = panelX + 22;
        int rowW = panelW - 44;
        _buildTentRowRect = new Rectangle(rowX, rowY, rowW, rowH);

        bool canBuild = CanBuildTrashBagTent(out string blockReason);
        bool built = _hasTrashBagTent;
        bool outdoors = IsOutdoorsPhase(_phase);
        bool hasBags = HasBackpackItem(ItemTrashBags);
        bool hasTape = HasBackpackItem(ItemDuctTape);

        Color rowBg = _buildTentButtonHovered
            ? Palette.ButtonSelectedBg
            : new Color(16, 18, 22, 255);
        Raylib.DrawRectangleRec(_buildTentRowRect, rowBg);
        Raylib.DrawRectangleLinesEx(_buildTentRowRect, 1f, Palette.SubtleBorder);

        const int iconSize = 28;
        int iconY = rowY + (rowH - iconSize) / 2;
        DrawItemIcon(ItemTrashBags, new Rectangle(rowX + 10, iconY, iconSize, iconSize),
            hasBags || built ? Color.WHITE : new Color(255, 255, 255, 90));
        DrawItemIcon(ItemDuctTape, new Rectangle(rowX + 10 + iconSize + 4, iconY, iconSize, iconSize),
            hasTape || built ? Color.WHITE : new Color(255, 255, 255, 90));

        int textX = rowX + 10 + iconSize * 2 + 14;
        Raylib.DrawTextEx(font, BuildTrashBagTent,
            new Vector2(textX, rowY + 10), 20, 0.65f,
            built ? Palette.TextDim : Palette.TextPrimary);

        string reqLine = built
            ? "Shelter pitched — +comfort outdoors"
            : "Trash Bags + Duct Tape · Outdoors only";
        Raylib.DrawTextEx(font, reqLine,
            new Vector2(textX, rowY + 30), 14, 0.5f, Palette.TextDim);

        if (!built && !outdoors)
        {
            Raylib.DrawTextEx(font, "Leave the building first",
                new Vector2(textX, rowY + 42), 13, 0.45f, new Color(180, 120, 100, 255));
        }
        else if (!built && outdoors && (!hasBags || !hasTape))
        {
            string missing = !hasBags && !hasTape ? "Missing both materials"
                : !hasBags ? "Missing trash bags" : "Missing duct tape";
            Raylib.DrawTextEx(font, missing,
                new Vector2(textX, rowY + 42), 13, 0.45f, new Color(180, 120, 100, 255));
        }

        int btnW = 72;
        int btnH = 30;
        int btnX = rowX + rowW - btnW - 10;
        int btnY = rowY + (rowH - btnH) / 2;
        _buildTentButtonRect = new Rectangle(btnX, btnY, btnW, btnH);

        if (built)
        {
            string done = "BUILT";
            int doneW = (int)Raylib.MeasureTextEx(font, done, 15, 0.5f).X;
            Raylib.DrawTextEx(font, done,
                new Vector2(btnX + (btnW - doneW) / 2f, btnY + 8), 15, 0.5f, Palette.Positive);
        }
        else
        {
            if (canBuild)
                DrawDialogButton(_buildTentButtonRect, "BUILD", _buildTentButtonHovered, font);
            else
            {
                Raylib.DrawRectangleRec(_buildTentButtonRect, new Color(24, 26, 30, 255));
                Raylib.DrawRectangleLinesEx(_buildTentButtonRect, 1f, Palette.SubtleBorder);
                int labelSize = 18;
                int labelW = (int)Raylib.MeasureTextEx(font, "BUILD", labelSize, 0.55f).X;
                Raylib.DrawTextEx(font, "BUILD",
                    new Vector2(btnX + (btnW - labelW) / 2f, btnY + 7),
                    labelSize, 0.55f, Palette.TextDim);
            }
        }

        if (!string.IsNullOrEmpty(_buildFeedback))
        {
            int fbSize = 16;
            int fbW = (int)Raylib.MeasureTextEx(font, _buildFeedback, fbSize, 0.55f).X;
            Color fbColor = _buildFeedback.Contains("crude shelter", StringComparison.OrdinalIgnoreCase)
                ? Palette.Positive
                : new Color(200, 130, 110, 255);
            Raylib.DrawTextEx(font, _buildFeedback,
                new Vector2(panelX + (panelW - fbW) / 2, panelY + panelH - 78),
                fbSize, 0.55f, fbColor);
        }
        else if (!built && !canBuild && !string.IsNullOrEmpty(blockReason))
        {
            int hintSize = 14;
            int hintW = (int)Raylib.MeasureTextEx(font, blockReason, hintSize, 0.5f).X;
            Raylib.DrawTextEx(font, blockReason,
                new Vector2(panelX + (panelW - hintW) / 2, panelY + panelH - 78),
                hintSize, 0.5f, Palette.TextDim);
        }

        int closeW = 120;
        int closeH = 36;
        int closeX = panelX + (panelW - closeW) / 2;
        int closeY = panelY + panelH - closeH - 16;
        _buildCloseRect = new Rectangle(closeX, closeY, closeW, closeH);
        DrawDialogButton(_buildCloseRect, "CLOSE", _buildCloseHovered, font);
    }

    // =====================================================================
    // STORE BUY MENU (modal shopping interface)
    // =====================================================================
    private void DrawStoreBuyMenu()
    {
        int screenW = _screenWidth;
        int screenH = _screenHeight;

        // Dark overlay
        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 160));

        // Larger panel for the list
        int panelW = 460;
        int panelH = 340;
        int panelX = (screenW - panelW) / 2;
        int panelY = (screenH - panelH) / 2 - 10;

        _storeBuyPanelRect = new Rectangle(panelX, panelY, panelW, panelH);

        Raylib.DrawRectangle(panelX, panelY, panelW, panelH, Palette.CardBg);
        Raylib.DrawRectangleLines(panelX, panelY, panelW, panelH, Palette.CardBorder);

        Font font = _uiFont;

        // Title
        string title = "SHELVES";
        int titleSize = 28;
        int titleW = (int)Raylib.MeasureTextEx(font, title, titleSize, 0.8f).X;
        Raylib.DrawTextEx(font, title,
            new Vector2(panelX + (panelW - titleW) / 2, panelY + 18),
            titleSize, 0.8f, Palette.TextPrimary);

        // Current money
        string moneyStr = $"{_money:N0} ₽";
        int moneyW = (int)Raylib.MeasureTextEx(font, moneyStr, 20, 0.6f).X;
        Raylib.DrawTextEx(font, moneyStr,
            new Vector2(panelX + panelW - 30 - moneyW, panelY + 20),
            20, 0.6f, Palette.TextSecondary);

        // Separator
        Raylib.DrawLine(panelX + 30, panelY + 48, panelX + panelW - 30, panelY + 48, Palette.SubtleBorder);

        // Item list
        int rowStartY = panelY + 60;
        int rowHeight = 44;
        const int iconSize = 32;

        for (int i = 0; i < _storeCatalog.Length; i++)
        {
            var (name, price, _, _, _) = _storeCatalog[i];

            int rowY = rowStartY + i * rowHeight;

            bool canAfford = _money >= price;
            bool hasSpace = _backpack.Any(s => string.IsNullOrEmpty(s));

            bool rowHovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), _storeBuyItemRects[i]);

            // Row background
            if (rowHovered && canAfford && hasSpace)
                Raylib.DrawRectangle(panelX + 20, rowY, panelW - 40, rowHeight - 4, new Color(48, 46, 40, 180));

            // Store the rect for input
            _storeBuyItemRects[i] = new Rectangle(panelX + 20, rowY, panelW - 40, rowHeight - 4);

            Color tint = (canAfford && hasSpace) ? Color.WHITE : new Color(120, 118, 112, 255);
            int iconX = panelX + 28;
            int iconY = rowY + (rowHeight - 4 - iconSize) / 2;
            Raylib.DrawRectangle(iconX - 1, iconY - 1, iconSize + 2, iconSize + 2, new Color(18, 17, 15, 255));
            DrawItemIcon(name, new Rectangle(iconX, iconY, iconSize, iconSize), tint);

            // Item name
            Color nameColor = (canAfford && hasSpace) ? Palette.TextPrimary : Palette.TextMuted;
            Raylib.DrawTextEx(font, name, new Vector2(panelX + 68, rowY + 11), 21, 0.6f, nameColor);

            // Price (right aligned)
            string priceStr = $"{price} ₽";
            int pW = (int)Raylib.MeasureTextEx(font, priceStr, 20, 0.6f).X;
            Color priceColor = canAfford ? new Color(185, 160, 90, 255) : Palette.TextMuted;
            Raylib.DrawTextEx(font, priceStr,
                new Vector2(panelX + panelW - 32 - pW, rowY + 12), 20, 0.6f, priceColor);
        }

        // Feedback line at bottom of list area
        if (_storeBuyFeedbackTimer > 0f && !string.IsNullOrEmpty(_storeBuyFeedback))
        {
            int fbW = (int)Raylib.MeasureTextEx(font, _storeBuyFeedback, 19, 0.5f).X;
            Raylib.DrawTextEx(font, _storeBuyFeedback,
                new Vector2(panelX + (panelW - fbW) / 2, panelY + panelH - 68),
                19, 0.5f, Palette.TextSecondary);
        }

        // Close button
        int btnW = 100;
        int btnH = 32;
        int btnX = panelX + (panelW - btnW) / 2;
        int btnY = panelY + panelH - 44;

        _storeBuyCloseRect = new Rectangle(btnX, btnY, btnW, btnH);

        Color btnBg = _storeBuyCloseHovered ? Palette.ButtonSelectedBg : Palette.ButtonBg;
        Color btnBorder = _storeBuyCloseHovered ? Palette.ButtonSelectedBorder : Palette.ButtonBorder;

        Raylib.DrawRectangleRec(_storeBuyCloseRect, btnBg);
        Raylib.DrawRectangleLinesEx(_storeBuyCloseRect, 1.5f, btnBorder);
        Raylib.DrawRectangle(btnX + 2, btnY + 2, btnW - 4, 2, Palette.ButtonTopAccent);

        string closeText = "CLOSE";
        int closeSize = 20;
        int closeW = (int)Raylib.MeasureTextEx(font, closeText, closeSize, 0.7f).X;
        Raylib.DrawTextEx(font, closeText,
            new Vector2(btnX + (btnW - closeW) / 2, btnY + 7),
            closeSize, 0.7f, Palette.TextPrimary);
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
            case Phase.Store:
            case Phase.Forest:
                DrawTopBar();
                DrawLeftSidebar();
                DrawRightSidebar();
                DrawCinematicScene();
                DrawActionBar();
                break;

            case Phase.Death:
                DrawDeathScreen();
                break;
        }

        if (_showItemDialog)
        {
            DrawItemDialog();
        }

        if (_showStoreBuyMenu)
        {
            DrawStoreBuyMenu();
        }

        if (_showRegionMap)
        {
            DrawRegionMapModal();
        }

        if (_showBuildDialog)
        {
            DrawBuildDialog();
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
        int row1Y = 14;   // upper line (45pt title)
        int row2Y = 48;   // lower line

        // LEFT ZONE — prominent title
        int leftX = 26;
        Raylib.DrawTextEx(font, "CONSCRIPT",
            new Vector2(leftX, row1Y),
            LayoutConstants.TitleFontSize, 0.85f, Palette.TextPrimary);

        // Elegant underline (positioned for the 45pt title)
        int titleW = (int)Raylib.MeasureTextEx(font, "CONSCRIPT", LayoutConstants.TitleFontSize, 0.85f).X;
        Raylib.DrawLine(leftX, row1Y + 34, leftX + titleW, row1Y + 34, Palette.StrongBorder);

        // CENTER ZONE — Day/Time (upper) + City • Specific Location (lower)
        string dayLine = $"Day {_day} — {GetTimeOfDayDisplay()}";
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

        float iconSize = 16f;
        float iconTextGap = 9f;

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

        float thermoSize = 11f;
        float thermoGap = 6f;
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

        DrawTopRightButtons();
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
        cy += 20;

        // Subtle underline
        Raylib.DrawLine(tx, cy - 2, tx + 42, cy - 2, Palette.SubtleBorder);
        cy += 12;

        // === Clean vertical stat list ===
        // Numeric stats with bars (label + value on one line, bar underneath)
        DrawCleanStatLine(ref cy, tx, "Health", _health, _envHealthDelta, _actionHealthDelta, Palette.Health);
        DrawCleanStatLine(ref cy, tx, "Energy", _energy, _envEnergyDelta, _actionEnergyDelta, Palette.Energy);
        DrawCleanStatLine(ref cy, tx, "Satiation", _satiation, _envSatiationDelta, _actionSatiationDelta, Palette.Satiation);
        DrawCleanStatLine(ref cy, tx, "Hydration", _hydration, _envHydrationDelta, _actionHydrationDelta, Palette.Hydration);
        DrawCleanStatLine(ref cy, tx, "Comfort", _comfort, _envComfortDelta, _actionComfortDelta, Palette.Comfort);

        cy += 6;

        // Simple text stats (same line for label + value to reduce clutter)
        DrawTextStatLine(ref cy, tx, "Money", $"{_money:N0} ₽");
        DrawTextStatLine(ref cy, tx, "Status", _status);

        // Backpack grid (visual inventory)
        cy += 20;
        DrawBackpack(cy, tx);
    }

    // =====================================================================
    // RIGHT PANEL — Region map
    // =====================================================================
    private void DrawRightSidebar()
    {
        int x = GameConstants.RightPanelLeft;
        int y = GameConstants.TopBarHeight;
        int w = GameConstants.RightPanelWidth;
        int h = _screenHeight - y - GameConstants.ActionBarHeight;

        Raylib.DrawRectangle(x, y, w, h, Palette.SidebarBg);
        Raylib.DrawLine(x, y, x, y + h, Palette.Divider);

        int tx = x + GameConstants.SidebarPadding;
        int cy = y + 28;
        cy = DrawWorldMap(cy, tx);
        cy += 16;
        DrawBuildSidebarButton(cy, tx);
    }

    private void DrawBuildSidebarButton(int y, int x)
    {
        Font font = _uiFont;
        int available = GameConstants.RightPanelWidth - GameConstants.SidebarPadding * 2;
        const int btnH = 36;
        _buildSidebarButtonRect = new Rectangle(x, y, available, btnH);
        DrawDialogButton(_buildSidebarButtonRect, "BUILD", _buildSidebarButtonHovered, font);
    }

    // Clean single-line stat row:  [←←] Label [→→]  26%  [thin colored bar]
    private const int StatLeftArrowSlotW = 42;
    private const int StatLabelColumnW = 92;
    private const int StatRightArrowSlotW = 42;
    private const int StatArrowSpacing = 14;

    private void DrawCleanStatLine(ref int y, int x, string label, int value, int envDelta, int actionDelta, Color barColor)
    {
        Font font = _uiFont;
        int available = GameConstants.SidebarWidth - GameConstants.SidebarPadding * 2;
        int arrowY = y + 13;

        int labelX = x + StatLeftArrowSlotW + 4;
        int rightSlotX = labelX + StatLabelColumnW;

        // Action arrows show action + environment combined; after the timer, only environment remains
        bool showActionFeedback = _actionDeltaTimer > 0f;
        int leftTotal = 0;
        int rightTotal = 0;
        if (showActionFeedback)
        {
            if (actionDelta < 0) leftTotal += actionDelta;
            if (envDelta < 0) leftTotal += envDelta;
            if (actionDelta > 0) rightTotal += actionDelta;
            if (envDelta > 0) rightTotal += envDelta;
        }
        else
        {
            leftTotal = envDelta < 0 ? envDelta : 0;
            rightTotal = envDelta > 0 ? envDelta : 0;
        }

        bool blink = showActionFeedback && (leftTotal != 0 || rightTotal != 0);
        DrawStatArrowIndicators(x, arrowY, rightSlotX, leftTotal, rightTotal, blink);

        Raylib.DrawTextEx(font, label, new Vector2(labelX, y), LayoutConstants.StatLabelSize, 0.75f, Palette.TextSecondary);

        // Value (right aligned)
        string val = $"{value}%";
        int valW = (int)Raylib.MeasureTextEx(font, val, LayoutConstants.StatValueSize, 0.7f).X;
        int valX = x + available - valW;
        Raylib.DrawTextEx(font, val, new Vector2(valX, y), LayoutConstants.StatValueSize, 0.7f, Palette.TextPrimary);

        y += 24;

        // Thin progress bar underneath the label (not under arrow slots)
        int barX = labelX;
        int barW = StatLabelColumnW;
        int barH = 5;
        Raylib.DrawRectangle(barX, y, barW, barH, new Color((byte)22, (byte)24, (byte)28, (byte)255));
        float pct = Math.Clamp(value / 100f, 0f, 1f);
        if (pct > 0.01f)
        {
            Raylib.DrawRectangle(barX, y, (int)(barW * pct), barH, barColor);
        }

        y += 18; // good spacing to next row
    }

    private void DrawStatArrowIndicators(int x, int arrowY, int rightSlotX, int leftTotal, int rightTotal, bool blink)
    {
        Color negative = Palette.Negative;
        Color positive = Palette.Positive;
        if (blink)
        {
            byte alpha = (byte)(110 + 145 * (0.5f + 0.5f * MathF.Sin((float)Raylib.GetTime() * 10f)));
            negative = new Color(negative.R, negative.G, negative.B, alpha);
            positive = new Color(positive.R, positive.G, positive.B, alpha);
        }

        if (leftTotal < 0)
        {
            int count = StatArrowCount(leftTotal);
            int slotRight = x + StatLeftArrowSlotW - 4;
            int startX = slotRight - (count - 1) * StatArrowSpacing;
            for (int i = 0; i < count; i++)
                DrawChevronLeft(startX + i * StatArrowSpacing, arrowY, negative);
        }

        if (rightTotal > 0)
        {
            int count = StatArrowCount(rightTotal);
            int startX = rightSlotX + 6;
            for (int i = 0; i < count; i++)
                DrawChevronRight(startX + i * StatArrowSpacing, arrowY, positive);
        }
    }

    private static void DrawChevronLeft(int cx, int cy, Color color)
    {
        const float size = 6f;
        const float thickness = 2.5f;
        Raylib.DrawLineEx(new Vector2(cx + size * 0.35f, cy - size), new Vector2(cx - size, cy), thickness, color);
        Raylib.DrawLineEx(new Vector2(cx - size, cy), new Vector2(cx + size * 0.35f, cy + size), thickness, color);
    }

    private static void DrawChevronRight(int cx, int cy, Color color)
    {
        const float size = 6f;
        const float thickness = 2.5f;
        Raylib.DrawLineEx(new Vector2(cx - size * 0.35f, cy - size), new Vector2(cx + size, cy), thickness, color);
        Raylib.DrawLineEx(new Vector2(cx + size, cy), new Vector2(cx - size * 0.35f, cy + size), thickness, color);
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
    // BACKPACK — simple visual grid with a "fabric pack" border treatment
    // =====================================================================

    private int DrawBackpack(int startY, int x)
    {
        Font font = _uiFont;
        int available = GameConstants.SidebarWidth - GameConstants.SidebarPadding * 2;

        // Header + capacity
        Raylib.DrawTextEx(font, "BACKPACK",
            new Vector2(x, startY), LayoutConstants.SidebarHeaderSize, 0.7f, Palette.TextMuted);

        int filled = _backpack.Count(i => !string.IsNullOrEmpty(i));
        string cap = $"{filled}/8";
        int capW = (int)Raylib.MeasureTextEx(font, cap, 14, 0.5f).X;
        Raylib.DrawTextEx(font, cap,
            new Vector2(x + available - capW, startY + 1), 14, 0.5f, Palette.TextDim);

        startY += 18;

        // Subtle underline
        Raylib.DrawLine(x, startY - 2, x + 42, startY - 2, Palette.SubtleBorder);
        startY += 8;

        // === Visual backpack body ===
        const int cols = 4;
        const int rows = 2;
        const int slot = 46;
        const int gap = 5;

        int gridW = cols * slot + (cols - 1) * gap;
        int gridX = x + (available - gridW) / 2;

        int flapH = 15;
        int bodyTopPad = flapH + 6;
        int bodyBotPad = 6;
        int gridH = rows * slot + (rows - 1) * gap;
        int bodyH = bodyTopPad + gridH + bodyBotPad;

        int packY = startY;
        int packW = available;

        // Main fabric body (dark olive-drab canvas)
        var fabric = new Color(40, 38, 33, 255);
        Raylib.DrawRectangle(x, packY, packW, bodyH, fabric);

        // Reinforced outer border (looks stitched)
        var seamDark = new Color(22, 20, 17, 255);
        Raylib.DrawRectangleLines(x, packY, packW, bodyH, seamDark);
        Raylib.DrawRectangle(x, packY + bodyH - 3, packW, 3, seamDark); // bottom reinforcement

        // Top flap (suggests the lid/pocket flap of a real backpack)
        var flap = new Color(52, 48, 42, 255);
        Raylib.DrawRectangle(x + 4, packY + 2, packW - 8, flapH, flap);
        Raylib.DrawRectangleLines(x + 4, packY + 2, packW - 8, flapH, new Color(30, 28, 24, 255));

        // Small metal rivets/buckles on the flap corners
        int rivetY = packY + 2 + flapH / 2;
        Raylib.DrawCircle(x + 16, rivetY, 3.2f, new Color(85, 80, 70, 255));
        Raylib.DrawCircle(x + packW - 16, rivetY, 3.2f, new Color(85, 80, 70, 255));

        // Horizontal seam line under the flap
        int contentTop = packY + flapH + 4;
        Raylib.DrawLine(x + 10, contentTop - 1, x + packW - 10, contentTop - 1, new Color(28, 26, 22, 255));

        // Draw the item grid (the actual "pockets") and cache the rects for input
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int sx = gridX + c * (slot + gap);
                int sy = contentTop + r * (slot + gap);
                int idx = r * cols + c;

                _backpackSlotRects[idx] = new Rectangle(sx, sy, slot, slot);

                string? item = _backpack[idx];
                bool occupied = !string.IsNullOrEmpty(item);

                // Pocket background
                var pocket = occupied
                    ? new Color(58, 50, 40, 255)
                    : new Color(18, 17, 15, 255);
                Raylib.DrawRectangle(sx, sy, slot, slot, pocket);

                // Inner border (pocket stitching)
                var pocketBorder = occupied ? new Color(75, 62, 48, 255) : Palette.SubtleBorder;
                Raylib.DrawRectangleLines(sx + 1, sy + 1, slot - 2, slot - 2, pocketBorder);

                if (occupied)
                {
                    if (_itemIcons.ContainsKey(item!))
                    {
                        DrawItemIcon(item!, new Rectangle(sx + 2, sy + 2, slot - 4, slot - 4), Color.WHITE);
                    }
                    else
                    {
                        // Fallback for items without icons yet (starting gear)
                        string label = item!.Length > 5 ? item.Substring(0, 5) : item;
                        float fz = 10f;
                        Raylib.DrawTextEx(font, label.ToUpperInvariant(),
                            new Vector2(sx + 4, sy + 8), fz, 0.35f, Palette.TextPrimary);
                    }
                }
                else
                {
                    // Very subtle empty indicator (small centered dot)
                    int cx = sx + slot / 2;
                    int cy = sy + slot / 2;
                    Raylib.DrawPixel(cx, cy, new Color(55, 50, 45, 140));
                }
            }
        }

        return packY + bodyH;
    }

    private void OpenRegionMap()
    {
        (double lon, double lat) = GetMapPlayerGeoPosition();
        _mapViewCenterLon = lon;
        _mapViewCenterLat = lat;
        _mapZoomLevelIndex = 6;   // start at 4× — local area; zoom out for wider context
        _mapPanning = false;
        _showRegionMap = true;
        SyncExpandedMapLayout();
    }

    private void CloseRegionMap()
    {
        _showRegionMap = false;
        _regionMapCloseHovered = false;
        _mapPanning = false;
        _mapZoomLevelIndex = 0;
    }

    private float CurrentMapZoom => MapZoomLevels[_mapZoomLevelIndex];

    private static string FormatMapZoom(float zoom) =>
        zoom >= 1f ? $"{zoom:F0}×" : $"{zoom:0.##}×";

    private void ComputeExpandedMapLayout(out Rectangle panelRect, out Rectangle mapRect)
    {
        int screenW = _screenWidth;
        int screenH = _screenHeight;

        const int marginX = 12;
        const int marginY = 8;
        const int chromeTop = 44;
        const int chromeBottom = 44;
        const int panelPadX = 16;

        int panelH = screenH - marginY * 2;
        int mapAreaH = panelH - chromeTop - chromeBottom;
        float maxMapW = screenW - marginX * 2 - panelPadX * 2;

        // Height-first: tall portrait viewport, distinct from the wide sidebar thumbnail.
        int mapH = mapAreaH;
        int mapW = (int)Math.Min(maxMapW, Math.Max(1, MathF.Round(mapH * ExpandedMapAspect)));
        int panelW = mapW + panelPadX * 2;
        int panelX = (screenW - panelW) / 2;
        int panelY = marginY;

        panelRect = new Rectangle(panelX, panelY, panelW, panelH);
        mapRect = new Rectangle(panelX + panelPadX, panelY + chromeTop, mapW, mapH);
    }

    private void SyncExpandedMapLayout()
    {
        ComputeExpandedMapLayout(out _, out Rectangle mapRect);
        _regionMapViewRect = mapRect;
        _regionMapDrawRect = mapRect;
        _expandedMapViewAspect = mapRect.Width / mapRect.Height;
    }

    private float MapGeoAspect =>
        (float)((RegionMapMaxLon - RegionMapMinLon) / (RegionMapMaxLat - RegionMapMinLat));

    private Rectangle GetSidebarMapDrawRect(Rectangle mapArea)
    {
        float drawW = mapArea.Width;
        float drawH = drawW / MapGeoAspect;
        if (drawH > mapArea.Height)
        {
            drawH = mapArea.Height;
            drawW = drawH * MapGeoAspect;
        }

        return new Rectangle(
            mapArea.X + (mapArea.Width - drawW) * 0.5f,
            mapArea.Y + (mapArea.Height - drawH) * 0.5f,
            drawW,
            drawH);
    }

    private void GetMapViewBounds(out double minLon, out double maxLon, out double minLat, out double maxLat)
    {
        double fullLatSpan = RegionMapMaxLat - RegionMapMinLat;
        double viewLatSpan = fullLatSpan / CurrentMapZoom;
        double viewLonSpan = viewLatSpan * _expandedMapViewAspect;

        minLon = _mapViewCenterLon - viewLonSpan / 2;
        maxLon = _mapViewCenterLon + viewLonSpan / 2;
        minLat = _mapViewCenterLat - viewLatSpan / 2;
        maxLat = _mapViewCenterLat + viewLatSpan / 2;
    }

    private void ClampMapViewCenter()
    {
        double fullLatSpan = RegionMapMaxLat - RegionMapMinLat;
        double fullLonSpan = RegionMapMaxLon - RegionMapMinLon;
        double halfLat = fullLatSpan / CurrentMapZoom / 2;
        double halfLon = halfLat * _expandedMapViewAspect;

        double latMin = RegionMapMinLat + Math.Min(halfLat, fullLatSpan / 2);
        double latMax = RegionMapMaxLat - Math.Min(halfLat, fullLatSpan / 2);
        _mapViewCenterLat = SafeClamp(_mapViewCenterLat, latMin, latMax);

        double lonMin = RegionMapMinLon + Math.Min(halfLon, fullLonSpan / 2);
        double lonMax = RegionMapMaxLon - Math.Min(halfLon, fullLonSpan / 2);
        _mapViewCenterLon = SafeClamp(_mapViewCenterLon, lonMin, lonMax);
    }

    private void ChangeMapZoom(int direction, Rectangle mapRect)
    {
        int next = _mapZoomLevelIndex + direction;
        if (next < 0 || next >= MapZoomLevels.Length)
            return;

        Vector2 focus = new(mapRect.X + mapRect.Width * 0.5f, mapRect.Y + mapRect.Height * 0.5f);
        GetMapViewBounds(out double vMinLon, out double vMaxLon, out double vMinLat, out double vMaxLat);
        float nx = (focus.X - mapRect.X) / mapRect.Width;
        float ny = (focus.Y - mapRect.Y) / mapRect.Height;
        double focusLon = vMinLon + nx * (vMaxLon - vMinLon);
        double focusLat = vMaxLat - ny * (vMaxLat - vMinLat);

        _mapZoomLevelIndex = next;

        double newLatSpan = (RegionMapMaxLat - RegionMapMinLat) / CurrentMapZoom;
        double newLonSpan = newLatSpan * _expandedMapViewAspect;
        _mapViewCenterLon = focusLon + (0.5 - nx) * newLonSpan;
        _mapViewCenterLat = focusLat + (ny - 0.5) * newLatSpan;
        ClampMapViewCenter();
    }

    private static void ComputeMapZoomButtonRects(Rectangle mapRect, out Rectangle zoomIn, out Rectangle zoomOut)
    {
        const int size = 34;
        int x = (int)mapRect.X + (int)mapRect.Width - size - 10;
        int y = (int)mapRect.Y + 10;
        zoomIn = new Rectangle(x, y, size, size);
        zoomOut = new Rectangle(x, y + size + 6, size, size);
    }

    // =====================================================================
    // WORLD MAP — real geography (Natural Earth via scripts/generate_region_map.py)
    // =====================================================================
    private int DrawWorldMap(int startY, int x)
    {
        Font font = _uiFont;
        int available = GameConstants.RightPanelWidth - GameConstants.SidebarPadding * 2;
        const int mapH = 100;
        int sectionTop = startY;

        Raylib.DrawTextEx(font, "REGION",
            new Vector2(x, startY), LayoutConstants.SidebarHeaderSize, 0.7f, Palette.TextMuted);

        string expandHint = "Click to expand";
        int hintSize = 11;
        int hintW = (int)Raylib.MeasureTextEx(font, expandHint, hintSize, 0.35f).X;
        Raylib.DrawTextEx(font, expandHint,
            new Vector2(x + available - hintW, startY + 2), hintSize, 0.35f, Palette.TextDim);

        startY += 18;
        Raylib.DrawLine(x, startY - 2, x + 42, startY - 2, Palette.SubtleBorder);
        startY += 10;

        Rectangle mapRect = new Rectangle(x, startY, available, mapH);
        _regionMapClickRect = new Rectangle(x, sectionTop, available, startY + mapH - sectionTop);

        DrawRegionMapInRect(GetSidebarMapDrawRect(mapRect), markerRadius: 3.5f, labelFontSize: 10f);

        if (_regionMapThumbHovered)
        {
            Raylib.DrawRectangleLinesEx(mapRect, 1.5f, Palette.ButtonSelectedBorder);
        }

        return startY + mapH;
    }

    private void DrawRegionMapModal()
    {
        int screenW = _screenWidth;
        int screenH = _screenHeight;

        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 175));

        Font font = _uiFont;

        ComputeExpandedMapLayout(out Rectangle panelRect, out Rectangle mapRect);
        int panelX = (int)panelRect.X;
        int panelY = (int)panelRect.Y;
        int panelW = (int)panelRect.Width;
        int panelH = (int)panelRect.Height;

        _regionMapPanelRect = panelRect;
        SyncExpandedMapLayout();

        Raylib.DrawRectangle(panelX, panelY, panelW, panelH, Palette.CardBg);
        Raylib.DrawRectangleLines(panelX, panelY, panelW, panelH, Palette.CardBorder);

        Raylib.DrawRectangleRec(mapRect, new Color(10, 12, 16, 255));

        string title = "REGION";
        Raylib.DrawTextEx(font, title,
            new Vector2(panelX + 22, panelY + 16), 22, 0.75f, Palette.TextMuted);

        string subtitle = "Russia — zoom out and pan to explore";
        Raylib.DrawTextEx(font, subtitle,
            new Vector2(panelX + 22, panelY + 38), 16, 0.6f, Palette.TextSecondary);

        string controls = "Use + / - to zoom · drag to pan";
        Raylib.DrawTextEx(font, controls,
            new Vector2(panelX + panelW - 22 - (int)Raylib.MeasureTextEx(font, controls, 13, 0.45f).X, panelY + 20),
            13, 0.45f, Palette.TextDim);

        GetMapViewBounds(out double vMinLon, out double vMaxLon, out double vMinLat, out double vMaxLat);
        DrawRegionMapInRect(_regionMapDrawRect, markerRadius: 8f, labelFontSize: 18f, vMinLon, vMaxLon, vMinLat, vMaxLat);

        ComputeMapZoomButtonRects(_regionMapDrawRect, out _mapZoomInRect, out _mapZoomOutRect);
        DrawMapZoomButton(_mapZoomInRect, "+", _mapZoomInHovered, _mapZoomLevelIndex < MapZoomLevels.Length - 1);
        DrawMapZoomButton(_mapZoomOutRect, "-", _mapZoomOutHovered, _mapZoomLevelIndex > 0);

        (double lon, double lat) = GetMapPlayerGeoPosition();
        string coords = $"{lat:F2}°N, {lon:F2}°E · {FormatMapZoom(CurrentMapZoom)}";
        Raylib.DrawTextEx(font, coords,
            new Vector2(panelX + 22, panelY + panelH - 34),
            14, 0.5f, Palette.TextDim);

        int btnW = 120;
        int btnH = 36;
        int btnX = panelX + (panelW - btnW) / 2;
        int btnY = panelY + panelH - btnH - 10;
        _regionMapCloseRect = new Rectangle(btnX, btnY, btnW, btnH);
        DrawDialogButton(_regionMapCloseRect, "CLOSE", _regionMapCloseHovered, font);
    }

    private void DrawMapZoomButton(Rectangle rect, string label, bool hovered, bool enabled)
    {
        Font font = _uiFont;
        Color bg = !enabled
            ? new Color(20, 22, 26, 200)
            : hovered ? Palette.ButtonSelectedBg : new Color(28, 30, 36, 230);
        Color border = !enabled
            ? Palette.SubtleBorder
            : hovered ? Palette.ButtonSelectedBorder : Palette.ButtonBorder;
        Color text = enabled ? Palette.TextPrimary : Palette.TextDim;

        Raylib.DrawRectangleRec(rect, bg);
        Raylib.DrawRectangleLinesEx(rect, 1.5f, border);

        int labelSize = 28;
        int labelW = (int)Raylib.MeasureTextEx(font, label, labelSize, 0.7f).X;
        Raylib.DrawTextEx(font, label,
            new Vector2(rect.X + (rect.Width - labelW) / 2f, rect.Y + 4),
            labelSize, 0.7f, text);
    }

    private void DrawRegionMapInRect(
        Rectangle mapRect,
        float markerRadius,
        float labelFontSize,
        double viewMinLon = RegionMapMinLon,
        double viewMaxLon = RegionMapMaxLon,
        double viewMinLat = RegionMapMinLat,
        double viewMaxLat = RegionMapMaxLat)
    {
        Font font = _uiFont;

        Raylib.DrawRectangleRec(mapRect, new Color(12, 14, 18, 255));

        if (_regionMapTexture.Id != 0)
        {
            double fullLonSpan = RegionMapMaxLon - RegionMapMinLon;
            double fullLatSpan = RegionMapMaxLat - RegionMapMinLat;
            float texW = _regionMapTexture.Width;
            float texH = _regionMapTexture.Height;

            double viewLonSpan = viewMaxLon - viewMinLon;
            double viewLatSpan = viewMaxLat - viewMinLat;

            Raylib.BeginScissorMode((int)mapRect.X, (int)mapRect.Y, (int)mapRect.Width, (int)mapRect.Height);

            if (viewLonSpan > 1e-9 && viewLatSpan > 1e-9)
            {
                double geoMinLon = Math.Max(viewMinLon, RegionMapMinLon);
                double geoMaxLon = Math.Min(viewMaxLon, RegionMapMaxLon);
                double geoMinLat = Math.Max(viewMinLat, RegionMapMinLat);
                double geoMaxLat = Math.Min(viewMaxLat, RegionMapMaxLat);

                if (geoMinLon < geoMaxLon && geoMinLat < geoMaxLat)
                {
                    float destX = mapRect.X + (float)((geoMinLon - viewMinLon) / viewLonSpan * mapRect.Width);
                    float destY = mapRect.Y + (float)((viewMaxLat - geoMaxLat) / viewLatSpan * mapRect.Height);
                    float destW = (float)((geoMaxLon - geoMinLon) / viewLonSpan * mapRect.Width);
                    float destH = (float)((geoMaxLat - geoMinLat) / viewLatSpan * mapRect.Height);
                    Rectangle dest = new Rectangle(destX, destY, destW, destH);

                    Rectangle src = new Rectangle(
                        (float)((geoMinLon - RegionMapMinLon) / fullLonSpan * texW),
                        (float)((RegionMapMaxLat - geoMaxLat) / fullLatSpan * texH),
                        (float)((geoMaxLon - geoMinLon) / fullLonSpan * texW),
                        (float)((geoMaxLat - geoMinLat) / fullLatSpan * texH));

                    Raylib.DrawTexturePro(_regionMapTexture, src, dest, Vector2.Zero, 0f, Color.WHITE);
                }
            }

            Raylib.EndScissorMode();
        }

        Raylib.DrawRectangleLinesEx(mapRect, 1f, Palette.SubtleBorder);

        (double lon, double lat) = GetMapPlayerGeoPosition();
        Vector2 player = GeoToMapPixel(mapRect, lon, lat, viewMinLon, viewMaxLon, viewMinLat, viewMaxLat);

        int px = (int)player.X;
        int py = (int)player.Y;
        float glowR = markerRadius + 2.5f;
        Raylib.DrawCircle(px, py, glowR, new Color(195, 175, 105, 50));
        Raylib.DrawCircle(px, py, markerRadius, Palette.ActionFlash);
        Raylib.DrawCircleLines(px, py, (int)(markerRadius + 1.5f), Palette.TextPrimary);

        string markerLabel = _phase == Phase.Forest ? "You" : "Ulan-Ude";
        int labelW = (int)Raylib.MeasureTextEx(font, markerLabel, labelFontSize, 0.35f).X;
        Raylib.DrawTextEx(font, markerLabel,
            new Vector2(player.X - labelW / 2f, player.Y + markerRadius + 4),
            labelFontSize, 0.35f, Palette.TextPrimary);
    }

    private (double lon, double lat) GetMapPlayerGeoPosition() =>
        _phase == Phase.Forest
            ? (ForestCampLon, ForestCampLat)
            : (UlanUdeLon, UlanUdeLat);

    private Vector2 GeoToMapPixel(
        Rectangle mapRect,
        double lon,
        double lat,
        double viewMinLon = RegionMapMinLon,
        double viewMaxLon = RegionMapMaxLon,
        double viewMinLat = RegionMapMinLat,
        double viewMaxLat = RegionMapMaxLat)
    {
        double nx = (lon - viewMinLon) / (viewMaxLon - viewMinLon);
        double ny = (viewMaxLat - lat) / (viewMaxLat - viewMinLat);
        nx = Math.Clamp(nx, 0, 1);
        ny = Math.Clamp(ny, 0, 1);
        return new Vector2(mapRect.X + (float)(nx * mapRect.Width), mapRect.Y + (float)(ny * mapRect.Height));
    }

    private string GetSceneNarrative()
    {
        return _phase switch
        {
            Phase.Opening => OpeningNarrative,
            Phase.Outside => OutsideNarrative,
            Phase.Store   => StoreNarrative,
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
            Raylib.DrawTextEx(font, _actionMessage, new Vector2(toastX + 14, toastY + 6), 16, 0.7f, c);
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
        DrawRightSidebar();

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
            Raylib.DrawTextEx(f, _actionMessage, new Vector2(toastX + 16, toastY + 7), 19, 0.8f, c);
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

        int w1 = (int)Raylib.MeasureTextEx(f, _deathLine1, 52, 0.9f).X;
        int w2 = (int)Raylib.MeasureTextEx(f, _deathLine2, 30, 0.85f).X;

        Raylib.DrawTextEx(f, _deathLine1,
            new Vector2((_screenWidth - w1) / 2, _screenHeight / 2 - 60),
            52, 0.9f, new Color(160, 70, 65, 255));

        Raylib.DrawTextEx(f, _deathLine2,
            new Vector2((_screenWidth - w2) / 2, _screenHeight / 2 - 10),
            30, 0.85f, Palette.TextSecondary);

        // The single "Try again" button is drawn by DrawActionBar (we set _choices to ["Try again"])
        DrawActionBar();

        DrawTopRightButtons();
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

    /// <summary>Math.Clamp throws when min &gt; max due to floating-point error at full zoom.</summary>
    private static double SafeClamp(double value, double min, double max) =>
        min >= max ? (min + max) / 2 : Math.Clamp(value, min, max);

    private void ClearEnvDeltas()
    {
        _envHealthDelta = 0;
        _envEnergyDelta = 0;
        _envSatiationDelta = 0;
        _envHydrationDelta = 0;
        _envComfortDelta = 0;
    }

    private void ClearNonComfortEnvDeltas()
    {
        _envHealthDelta = 0;
        _envEnergyDelta = 0;
        _envSatiationDelta = 0;
        _envHydrationDelta = 0;
    }

    private void ClearActionDeltas()
    {
        _actionHealthDelta = 0;
        _actionEnergyDelta = 0;
        _actionSatiationDelta = 0;
        _actionHydrationDelta = 0;
        _actionComfortDelta = 0;
        _actionDeltaTimer = 0f;
    }

    private void MarkActionChanged()
    {
        _actionDeltaTimer = ActionDeltaDisplayDuration;
    }

    private void ModifyStatFromAction(ref int stat, ref int actionDelta, int amount)
    {
        if (amount == 0) return;
        actionDelta += amount;
        stat = Clamp(stat + amount);
        MarkActionChanged();
    }

    private void SetStatFromAction(ref int stat, ref int actionDelta, int value)
    {
        int clamped = Clamp(value);
        int change = clamped - stat;
        if (change == 0) return;
        actionDelta += change;
        stat = clamped;
        MarkActionChanged();
    }

    private void ApplyEnvironmentOnAction()
    {
        if (!IsOutdoorsPhase(_phase)) return;
        ModifyStatFromAction(ref _comfort, ref _actionComfortDelta, OutdoorComfortPerActionPenalty());
    }

    private void ApplyEnvironmentOutside()
    {
        ClearNonComfortEnvDeltas();
        RefreshOutdoorComfortEnvironment();
    }

    private void ApplyEnvironmentHeatedBuilding()
    {
        ClearNonComfortEnvDeltas();
        SetEnvironmentComfort(HeatedBuildingComfortBonus);
    }

    private const int HeatedBuildingComfortBonus = 4;   // 1 green arrow — warmed up a little indoors

    private void SetEnvironmentComfort(int targetDelta)
    {
        int diff = targetDelta - _envComfortDelta;
        if (diff == 0) return;
        _envComfortDelta = targetDelta;
        _comfort = Clamp(_comfort + diff);
    }

    /// <summary>
    /// Steady outdoor discomfort while wearing a winter coat (maps to 1–3 arrows).
    /// </summary>
    private static int OutdoorComfortPenaltyForTemp(int tempF)
    {
        if (tempF >= 40) return -2;   // 1 arrow — cool air, mostly fine
        if (tempF >= 22) return -4;   // 1 arrow — chilly courtyard in a winter coat (~27°F)
        if (tempF >= 12) return -8;   // 2 arrows — cold night in the open
        if (tempF >= 0) return -12;   // 2 arrows — biting cold
        return -18;                   // 3 arrows — brutal / hypothermia risk
    }

    private static int OutdoorComfortPerActionPenalty(int tempF) =>
        tempF >= 22 ? -1 : tempF >= 12 ? -2 : -3;

    private int OutdoorComfortPerActionPenalty() =>
        OutdoorComfortPerActionPenalty(_temperatureF);

    private int OutdoorShelterComfortBonus() =>
        _hasTrashBagTent && IsOutdoorsPhase(_phase) ? TrashBagTentComfortBonus : 0;

    private void RefreshOutdoorComfortEnvironment()
    {
        if (!IsOutdoorsPhase(_phase)) return;
        SetEnvironmentComfort(OutdoorComfortPenaltyForTemp(_temperatureF) + OutdoorShelterComfortBonus());
    }

    private static int StatArrowCount(int delta)
    {
        int abs = Math.Abs(delta);
        if (abs == 0) return 0;
        if (abs <= 4) return 1;
        if (abs <= 10) return 2;
        return 3;
    }
}

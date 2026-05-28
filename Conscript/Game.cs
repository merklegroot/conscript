using System;
using System.Collections.Generic;
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
    // --- Screen & lifecycle ---
    private const float ActionMessageDuration = 3.5f;

    private readonly int _screenWidth = GameConstants.ScreenWidth;
    private readonly int _screenHeight = GameConstants.ScreenHeight;

    private readonly Random _rng = new();

    private bool _shouldExit;
    public bool ShouldExit => _shouldExit;

    // --- Scene textures & UI font ---
    private Font _uiFont;
    private Font _uiFontItalic;
    private Texture2D _backgroundTexture;   // currently active scene background (swapped on phase change)
    private Texture2D _apartmentBackground;
    private Texture2D _outsideBackground;
    private Texture2D _townBackground;
    private Texture2D _industrialDistrictBackground;
    private Texture2D _commercialDistrictBackground;
    private Texture2D _forestEntryBackground;
    private Texture2D _forestBackground;
    private Texture2D _forestStreamBackground;
    private Texture2D _storeBackground;
    private Texture2D _cafeBackground;
    private Texture2D _deliveryTruckBackground;
    private Texture2D _warehouseBackground;
    private Texture2D _warehouseAmbushBackground;
    private Texture2D _cafeOwnerPortraitTexture;
    private Texture2D _tentBackground;
    private Texture2D _regionMapTexture;
    private Texture2D _trashBagTentTexture;
    private Texture2D _titleLogoTexture;

    // --- Survival tuning (choices, energy, dropped items) ---
    private const string BuildTrashBagTent = "Trash Bag Tent";
    private const string CraftMolotov = "Molotov";
    private const string CraftLitMolotov = "Lit Molotov";
    private const string ChoiceEnterTent = "ENTER TENT";
    private const string ChoiceExitTent = "EXIT TENT";
    private const string ChoiceDisassembleTent = "DISASSEMBLE TENT";
    private const string ChoiceSleep = "SLEEP";
    private const string ChoiceHunt = "HUNT";
    private const string ChoiceForage = "FORAGE";
    private const int ForageOptionCount = 2;
    private static readonly string[] ForageOptionItems = [GameItems.Firewood, GameItems.Rocks];
    private static readonly string[] ForageOptionDescriptions =
    [
        "Fallen branches and dry wood in the undergrowth.",
        "Loose stone from the forest floor and stream bed."
    ];
    private const string ChoiceFollowStream = "FOLLOW THE STREAM";
    private const string ChoiceEnterDeepForest = "ENTER DEEP FOREST";
    private const string ChoiceBackToForestEntry = "BACK TO FOREST ENTRY";
    private const string ChoiceGoIntoTown = "GO INTO TOWN";
    private const string ChoiceBackToCourtyard = "BACK TO THE COURTYARD";
    private const string ChoiceGoBackToTown = "GO BACK TO TOWN";
    private const string ChoiceIndustrialDistrict = "INDUSTRIAL DISTRICT";
    private const string ChoiceCommercialDistrict = "COMMERCIAL DISTRICT";
    private const string ChoiceBackToTownCenter = "BACK TO TOWN";
    private const string ChoiceHeadForForest = "HEAD FOR THE FOREST";
    private const string ChoiceHideInGarbage = "HIDE IN THE GARBAGE";
    private const string ChoiceGoToUnclesHouse = "GO TO UNCLE'S HOUSE";
    private const string ChoiceConvenienceStore = "CONVENIENCE STORE";
    private const string ChoiceBrowseShelves = "BROWSE SHELVES";
    private const string ChoiceLeaveStore = "LEAVE THE WAY YOU CAME";
    private const string ChoiceCafe = "КАФЕ";
    private const string ChoiceTalkToOwner = "TALK TO THE OWNER";
    private const string ChoiceLeaveCafe = "LEAVE THE WAY YOU CAME";
    private const string ChoiceDriveToWarehouse = "DRIVE TO THE WAREHOUSE";
    private const string ChoiceGetOutOfTruck = "GET OUT OF THE TRUCK";
    private const string ChoiceFight = "FIGHT";
    private const string ChoiceWait = "WAIT";
    private const string ChoiceTryAgain = "Try again";
    private const string ChoiceOpenDoor = "Open the door";
    private const string ChoiceFleeOutWindow = "Flee out the window";
    private const string ChoiceBarDoorAndFight = "Bar the door and fight";

    private const int EnergyCostHunt = 6; // in addition to time passing drain
    private const int EnergyCostForage = 4;
    private const int EnergyCostFillBottle = 2;

    // Resting: sleeping should be a meaningful time-skip with a strong energy restore.
    private const int TentSleepTimeSteps = 3;      // ~9 hours (8 slots/day ≈ 3 hours each)
    private const int TentSleepSatiationCost = -8;
    private const int TentSleepHydrationCost = -8;
    private const int TentSleepHealthGain = 2;

    // Items left on the ground in a room (location = phase)
    private const int DroppedItemLifetimeTurns = 5;
    private const int MaxDroppedItemsPerRoom = 6;
    private const int ConcealmentPenaltyPerDroppedItem = 7;
    private const int ConcealmentPenaltyForTent = 18;
    private const int DroppedItemSceneIconSize = 54;
    private const int DroppedItemScenePlatePad = 5;

    private readonly Dictionary<string, Texture2D> _itemIcons = new(StringComparer.OrdinalIgnoreCase);

    // --- Top-right utility buttons (restart, debug start, controller) ---
    private Rectangle _restartButtonRect;
    private Rectangle _debugStartButtonRect;
    private Rectangle _controllerButtonRect;
    private bool _restartHovered;
    private bool _debugStartHovered;
    private bool _controllerHovered;

    // Controller debug overlay (opened from top-right gamepad button)
    private bool _showControllerDebug;
    private int _controllerDebugPadIndex;
    private Rectangle _controllerDebugCloseRect;
    private bool _controllerDebugCloseHovered;
    private Rectangle _controllerDebugPrevRect;
    private Rectangle _controllerDebugNextRect;
    private bool _controllerDebugPrevHovered;
    private bool _controllerDebugNextHovered;
    private readonly Rectangle[] _controllerDebugTabRects = new Rectangle[GamepadDebugLayout.MaxGamepadsToShow];
    private readonly bool[] _controllerDebugTabHovered = new bool[GamepadDebugLayout.MaxGamepadsToShow];

    // === Game flow ===
    internal enum Phase
    {
        Opening,   // At home with family — the knock on the door
        Outside,   // In the apartment courtyard / yard immediately after climbing out the window
        Town,      // Central streets between the courtyard and the town districts
        IndustrialDistrict,  // Warehouses and yards on the west side of town
        CommercialDistrict,  // Shops on the east side; forest access is south from here
        Store,     // Inside a late-night convenience store / kiosk
        Cafe,      // Workers' café off an industrial side street (Кафе)
        DeliveryTruck, // Behind the wheel on Boris's warehouse run
        WarehouseTruck,   // Warehouse 14 loading bay — still inside the truck cab
        WarehouseAmbush,  // Outside the cab — met by bratdvas; Boris betrayed you
        ForestEntry,  // Edge of the pines just beyond the apartment blocks
        ForestStream, // Forest stream — between the forest entry and deep forest
        Forest,       // Deep forest survival
        Tent,         // Inside the trash-bag shelter
        Death
    }

    private Phase _phase = Phase.Opening;
    private Phase _phaseOutdoorBeforeTent = Phase.ForestEntry;
    private Phase _phaseBeforeStore = Phase.Town;
    private Phase _phaseBeforeCafe = Phase.IndustrialDistrict;
    private bool _borisDeliveryJobActive;
    private CafeOwnerDialog.Stage _cafeOwnerDialogStage = CafeOwnerDialog.Stage.Main;

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

    // Passive drain rises through the day (Morning 2 → Late Night 9 per slot).
    private const int EnergyDrainBasePerTimeSlot = 2;
    private const int EnergyDrainIncreasePerSlot = 1;
    private const int EnergyCostTravel = 4;        // longer moves (yard ↔ forest, store exit)
    private const int EnergyCostTravelShort = 2;   // tent flap, nearby kiosk

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
    private string _status = "Fugitive";
    private int _comfort = 62;   // protection from the elements (higher = better)
    private int _concealment = 35;   // how hard you are to find (higher = better); location-driven for now

    // --- Stat deltas (environment vs. last action) ---
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

    // --- Backpack & ground items ---
    // Backpack inventory grid (prototype: 8 slots = 2×4)
    private string?[] _backpack = new string?[] { "Knife", "Lighter", "Phone", null, null, null, null, null };
    // Remaining uses per slot (null = full/default for that item type)
    private int?[] _backpackItemCharges = new int?[8];

    // Items dropped in the current scene (per-room, expire after several turns)
    private readonly List<DroppedItem> _droppedItems = new();
    private readonly List<Rectangle> _droppedItemClickRects = new();
    private readonly List<int> _droppedItemVisibleIndices = new(); // parallel to click rects → _droppedItems index
    private int _hoveredDroppedItemListIndex = -1; // index into visible/click lists

    // --- Modal overlays (dialogs & menus) ---
    // Item interaction dialog (simple modal for now)
    private bool _showItemDialog;
    private int _dialogItemIndex = -1;
    private int _dialogDroppedItemIndex = -1;
    private string _dialogItemName = "";
    private Rectangle _dialogCloseRect;
    private bool _dialogCloseHovered;
    private Rectangle _dialogActionRect;
    private bool _dialogActionHovered;
    private Rectangle _dialogSecondaryActionRect;
    private bool _dialogSecondaryActionHovered;
    private Rectangle _dialogDropRect;
    private bool _dialogDropHovered;
    private Rectangle _dialogPanelRect;

    // Convenience store buy menu (modal) — list left, item detail + buy right
    private bool _showStoreBuyMenu;
    private int _storeBuyHighlightedIndex;  // list cursor (keyboard, controller, or mouse hover)
    private int _storeBuyDetailIndex = -1;  // item shown in the right panel (click or keyboard/controller nav)
    private string _storeBuyFeedback = "";
    private float _storeBuyFeedbackTimer;
    private Rectangle[] _storeBuyItemRects = new Rectangle[5];  // populated during DrawStoreBuyMenu
    private Rectangle _storeBuyPanelRect;
    private Rectangle _storeBuyCloseRect;
    private bool _storeBuyCloseHovered;
    private Rectangle _storeBuyPurchaseRect;
    private bool _storeBuyPurchaseHovered;

    // Delivery truck glove compartment (modal — take items, no price)
    private bool _showGloveBoxMenu;
    private int _gloveBoxHighlightedIndex;
    private int _gloveBoxDetailIndex = -1;
    private string _gloveBoxFeedback = "";
    private float _gloveBoxFeedbackTimer;
    private readonly Rectangle[] _gloveBoxItemRects = new Rectangle[GloveCompartmentCatalog.EntryCount];
    private Rectangle _gloveBoxPanelRect;
    private Rectangle _gloveBoxCloseRect;
    private bool _gloveBoxCloseHovered;
    private Rectangle _gloveBoxPickupRect;
    private bool _gloveBoxPickupHovered;
    private readonly bool[] _gloveBoxLootTaken = new bool[GloveCompartmentCatalog.EntryCount];
    private readonly int[] _gloveBoxVisibleCatalogIndices = new int[GloveCompartmentCatalog.EntryCount];
    private int _gloveBoxVisibleCount;

    // Build & craft dialog (modal)
    private bool _showBuildDialog;
    private bool _hasTrashBagTent;
    private Phase? _tentBuiltInPhase;
    private Rectangle _buildSidebarButtonRect;
    private bool _buildSidebarButtonHovered;
    private Rectangle _huntSidebarButtonRect;
    private bool _huntSidebarButtonHovered;
    private Rectangle _forageSidebarButtonRect;
    private bool _forageSidebarButtonHovered;

    // Stats help (left sidebar info icon + modal)
    private Rectangle _statsHelpIconRect;
    private bool _statsHelpIconHovered;
    private bool _showStatsHelp;
    private Rectangle _statsHelpPanelRect;
    private Rectangle _statsHelpCloseRect;
    private bool _statsHelpCloseHovered;

    // Quit (right panel + confirmation)
    private Rectangle _quitSidebarButtonRect;
    private bool _quitSidebarButtonHovered;
    private bool _showQuitConfirm;
    private Rectangle _quitConfirmPanelRect;
    private Rectangle _quitConfirmYesRect;
    private Rectangle _quitConfirmNoRect;
    private bool _quitConfirmYesHovered;
    private bool _quitConfirmNoHovered;
    private int _quitConfirmSelectedButton; // 0 = cancel, 1 = quit

    private Rectangle _buildPanelRect;
    private Rectangle _buildCloseRect;
    private bool _buildCloseHovered;
    private Rectangle _buildTentRowRect;
    private Rectangle _buildTentButtonRect;
    private bool _buildTentButtonHovered;
    private Rectangle _buildMolotovRowRect;
    private Rectangle _buildMolotovButtonRect;
    private bool _buildMolotovButtonHovered;
    private Rectangle _buildLitMolotovRowRect;
    private Rectangle _buildLitMolotovButtonRect;
    private bool _buildLitMolotovButtonHovered;
    private string _buildFeedback = "";
    private float _buildFeedbackTimer;
    private const float BuildFeedbackDuration = 2.2f;
    private bool _showForageDialog;
    private Rectangle _foragePanelRect;
    private Rectangle _forageCloseRect;
    private bool _forageCloseHovered;
    private readonly Rectangle[] _forageOptionRowRects = new Rectangle[ForageOptionCount];
    private readonly bool[] _forageOptionHovered = new bool[ForageOptionCount];
    private int _forageHighlightedIndex;
    private bool _showCafeOwnerDialog;
    private Rectangle _cafeOwnerPanelRect;
    private Rectangle _cafeOwnerCloseRect;
    private bool _cafeOwnerCloseHovered;
    private readonly Rectangle[] _cafeOwnerOptionRowRects = new Rectangle[CafeOwnerDialog.MainOptionCount];
    private readonly bool[] _cafeOwnerOptionHovered = new bool[CafeOwnerDialog.MainOptionCount];
    private int _cafeOwnerHighlightedIndex;
    private int _cafeOwnerSelectedOption = -1;
    private const int TrashBagTentComfortBonus = 8;
    private const int TentInteriorComfortBonus = 14;
    private Rectangle _trashBagTentClickRect;
    private bool _trashBagTentHovered;
    private Rectangle _gloveCompartmentClickRect;
    private bool _gloveCompartmentHovered;

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

    // Detail dialog viewport (width / height). Sidebar uses RegionMapGeo.LonLatAspect (~3.9) instead.
    private const float ExpandedMapAspect = 0.78f;

    // Cached backpack slot rectangles (updated during DrawBackpack every frame)
    private Rectangle[] _backpackSlotRects = new Rectangle[8];

    private const string DefaultDeathLine1 = "You died.";
    private const string DefaultDeathLine2 = "The war took you on the first day.";

    // Custom death screen text (set via EnterDeath or ResetDeathLines before Phase.Death)
    private string _deathLine1 = DefaultDeathLine1;
    private string _deathLine2 = DefaultDeathLine2;

    private int _selectedIndex;

    // Current choices (change per phase)
    private string[] _choices = [];

    // Opening scene narrative (the knock)
    private const string OpeningNarrative =
        "*KNOCK* *KNOCK*\n\n" +
        "“Military Commissariat!\nOpen up!”\n\n" +
        "Your family looks on at you in silence.\n\n" +
        "There's nowhere left to hide.";

    private const string ForestEntryNarrative =
        "The pines begin just past the last apartment block.\n" +
        "Streetlights still bleed through the branches, but the city noise is thinning.\n" +
        "You are not safe yet — only hidden.";

    private const string ForestNarrative =
        "You pushed deeper into the forest.\nThe city is far behind. First light snow has begun to fall — winter is arriving sooner than expected. This will not be easy.";

    private const string ForestStreamNarrative =
        "A narrow stream cuts through the pines.\n" +
        "The water is painfully cold, but it is the first clean water you have seen since you fled.\n" +
        "Tracks along the bank suggest animals come here to drink.";

    private const string OutsideNarrative =
        "You hit the ground hard behind the apartment block.\n" +
        "The window you escaped through is still lit.\n" +
        "No sirens yet — but the night is too quiet.\n" +
        "Every shadow could hide a patrol. Move.";

    private const string TownNarrative =
        "The streets around the courtyard are empty under the streetlights.\n" +
        "Industrial blocks lie to the west; shopfronts and neon to the east.\n" +
        "A late-night kiosk glows on the corner — you could slip inside.\n" +
        "You keep to the shadows and move quickly.";

    private const string IndustrialDistrictNarrative =
        "Warehouses and fenced lots line the side streets.\n" +
        "A distant rail yard clanks in the cold.\n" +
        "A dingy café still glows on the corner — the owner runs more than tea.\n" +
        "Few other windows are lit — this is the edge of town.";

    private const string CafeNarrative =
        "Steam and cheap tea mask the smell of cigarettes and diesel.\n" +
        "Boris watches the room like he owns everyone in it.\n" +
        "He might help you disappear — or sell you out for pocket change.";

    private const string DeliveryTruckNarrative =
        "You sit in the cab of an old ZIL with the engine ticking.\n" +
        $"Boris wants this load at {CafeOwnerDialog.WarehouseName} — loading bay three, west yards.\n" +
        "The key is warm in your hand. The warehouse is waiting.";

    private const string WarehouseTruckNarrative =
        "The truck idles at loading bay three behind a corrugated hangar.\n" +
        "Floodlights cut through the rain. The roll-up door is half open — someone is expected.\n" +
        "Boris said fifty thousand when the cargo is inside. No one is watching the street.";

    private const string WarehouseAmbushNarrative =
        "You step down onto wet concrete.\n" +
        "Two men are waiting in the shadows by the half-open door — bratdvas.\n" +
        "One of them smiles like you did him a favor.\n" +
        "Boris betrayed you. This wasn't a delivery.";

    private const string CommercialDistrictNarrative =
        "Shopfronts line the side streets under harsh neon.\n" +
        "Foot traffic is thin, but every window might hide a watcher.\n" +
        "South of here, the pines begin at the edge of the blocks.";

    private const string StoreNarrative =
        "The fluorescent lights are brutal after the dark yard.\n" +
        "A security camera stares from the ceiling with a dead red eye.\n" +
        "The clerk is glued to his phone behind the counter.\n" +
        "You have never felt more visible in your life.";

    private const string TentNarrative =
        "Your crude tent made from trash bags and duct tape\n"
        + "provides at least some protection from the elements.";

    private string _actionMessage = "";
    private float _actionMessageTimer;

    // --- Phase transitions ---
    private void EnterPhase(Phase newPhase)
    {
        // Lit molotovs are not safe to travel with. If Sergei tries to change rooms while carrying one,
        // it goes off in his hands.
        if (newPhase != _phase &&
            newPhase != Phase.Death &&
            _phase != Phase.Death &&
            HasBackpackItem(GameItems.LitMolotov))
        {
            EnterDeath("It explodes in your hands.", "You die before you even feel the heat.");
            return;
        }

        _phase = newPhase;
        _selectedIndex = 0;
        _actionMessage = "";
        _actionMessageTimer = 0;

        // Reset custom death text unless we're deliberately entering the death screen
        if (newPhase != Phase.Death)
            ResetDeathLines();

        switch (newPhase)
        {
            case Phase.Opening:
                _choices = new[]
                {
                    ChoiceOpenDoor,
                    ChoiceFleeOutWindow,
                    ChoiceBarDoorAndFight
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
                _backpackItemCharges = new int?[8];
                _hasTrashBagTent = false;
                _tentBuiltInPhase = null;
                ClearEnvDeltas();
                ClearActionDeltas();
                break;

            case Phase.ForestEntry:
                _day = 0;
                _timeOfDay = "Night";
                _location = "Forest Entry";
                _city = "Ulan-Ude, Republic of Buryatia";
                _status = "On the Run";
                _season = "Early Autumn";
                _temperatureF = 22;
                ClearEnvDeltas();
                RefreshOutdoorComfortEnvironment();
                RefreshOutdoorActionChoices();
                break;

            case Phase.Forest:
                ClearEnvDeltas();
                RefreshOutdoorComfortEnvironment();
                _day = 3;
                _timeOfDay = "Morning";
                _location = "Deep Forest";
                _city = "Ulan-Ude, Republic of Buryatia";
                _status = "Fugitive";
                _season = "Early Autumn";
                _temperatureF = 19;   // colder the deeper you go
                // _money carries over from the Opening phase (starts at 10,000 ₽)
                RefreshOutdoorActionChoices();
                break;

            case Phase.ForestStream:
                ClearEnvDeltas();
                RefreshOutdoorComfortEnvironment();
                _day = 3;
                _timeOfDay = "Morning";
                _location = "Forest Stream";
                _city = "Ulan-Ude, Republic of Buryatia";
                _status = "Fugitive";
                _season = "Early Autumn";
                _temperatureF = 17;
                RefreshOutdoorActionChoices();
                break;

            case Phase.Death:
                _choices = new[] { ChoiceTryAgain };
                break;

            case Phase.Outside:
                _day = 0;
                _timeOfDay = "Night";
                _location = "Apartment Courtyard";
                _city = "Ulan-Ude, Republic of Buryatia";
                _status = "On the Run";
                _season = "Early Autumn";
                _temperatureF = 27;   // clear cold night in the yard
                ApplyEnvironmentOutside();
                RefreshOutdoorActionChoices();
                break;

            case Phase.Town:
                _day = 0;
                _timeOfDay = "Night";
                _location = "Town";
                _city = "Ulan-Ude, Republic of Buryatia";
                _status = "On the Run";
                _season = "Early Autumn";
                _temperatureF = 26;
                ApplyEnvironmentOutside();
                RefreshOutdoorActionChoices();
                break;

            case Phase.IndustrialDistrict:
                _day = 0;
                _timeOfDay = "Night";
                _location = "Industrial District";
                _city = "Ulan-Ude, Republic of Buryatia";
                _status = "On the Run";
                _season = "Early Autumn";
                _temperatureF = 24;
                ApplyEnvironmentOutside();
                RefreshOutdoorActionChoices();
                break;

            case Phase.CommercialDistrict:
                _day = 0;
                _timeOfDay = "Night";
                _location = "Commercial District";
                _city = "Ulan-Ude, Republic of Buryatia";
                _status = "On the Run";
                _season = "Early Autumn";
                _temperatureF = 26;
                ApplyEnvironmentOutside();
                RefreshOutdoorActionChoices();
                break;

            case Phase.Store:
                _choices = new[]
                {
                    ChoiceBrowseShelves,
                    ChoiceLeaveStore,
                    ChoiceWait
                };
                ApplyEnvironmentHeatedBuilding();
                _day = 0;
                _timeOfDay = "Night";
                _location = "Convenience Store";
                _city = "Ulan-Ude, Republic of Buryatia";
                _status = "On the Run";
                _season = "Early Autumn";
                _temperatureF = 24;   // slightly warmer inside
                // other stats carry over
                break;

            case Phase.Cafe:
                _choices = new[]
                {
                    ChoiceTalkToOwner,
                    ChoiceLeaveCafe,
                    ChoiceWait
                };
                ApplyEnvironmentHeatedBuilding();
                _day = 0;
                _timeOfDay = "Night";
                _location = "Кафе";
                _city = "Ulan-Ude, Republic of Buryatia";
                _status = "On the Run";
                _season = "Early Autumn";
                _temperatureF = 28;
                break;

            case Phase.DeliveryTruck:
                _choices = new[] { ChoiceDriveToWarehouse, ChoiceWait };
                ApplyEnvironmentOutside();
                _day = 0;
                _timeOfDay = "Night";
                _location = "Delivery Truck";
                _city = "Ulan-Ude, Republic of Buryatia";
                _status = "On the Run";
                _season = "Early Autumn";
                _temperatureF = 22;
                break;

            case Phase.WarehouseTruck:
                _choices = new[] { ChoiceGetOutOfTruck, ChoiceWait };
                ApplyEnvironmentOutside();
                _day = 0;
                _timeOfDay = "Night";
                _location = $"{CafeOwnerDialog.WarehouseName} — Bay 3";
                _city = "Ulan-Ude, Republic of Buryatia";
                _status = "On the Run";
                _season = "Early Autumn";
                _temperatureF = 21;
                break;

            case Phase.WarehouseAmbush:
                _choices = new[] { ChoiceFight, ChoiceWait };
                _selectedIndex = 0;
                ApplyEnvironmentOutside();
                _day = 0;
                _timeOfDay = "Night";
                _location = $"{CafeOwnerDialog.WarehouseName} — Bay 3";
                _city = "Ulan-Ude, Republic of Buryatia";
                _status = "On the Run";
                _season = "Early Autumn";
                _temperatureF = 21;
                break;

            case Phase.Tent:
                _choices = new[] { ChoiceExitTent, ChoiceDisassembleTent, ChoiceSleep, ChoiceWait };
                ApplyEnvironmentTentInterior();
                _location = "Trash Bag Tent";
                break;
        }

        // Swap the background image for the new phase
        _backgroundTexture = _phase switch
        {
            Phase.Opening      => _apartmentBackground,
            Phase.Outside      => _outsideBackground,
            Phase.Town         => _townBackground,
            Phase.IndustrialDistrict => _industrialDistrictBackground,
            Phase.CommercialDistrict => _commercialDistrictBackground,
            Phase.Store        => _storeBackground,
            Phase.Cafe         => _cafeBackground,
            Phase.DeliveryTruck => _deliveryTruckBackground,
            Phase.WarehouseTruck    => _warehouseBackground,
            Phase.WarehouseAmbush   => _warehouseAmbushBackground,
            Phase.ForestEntry  => _forestEntryBackground,
            Phase.Forest       => _forestBackground,
            Phase.ForestStream => _forestStreamBackground,
            Phase.Tent         => _tentBackground,
            _                  => _forestBackground
        };

        RefreshConcealment();
        if (newPhase == Phase.Opening)
            ClearDroppedItems();
    }

    /// <summary>
    /// Move between forest areas without resetting day, stats, or inventory.
    /// </summary>
    private void EnterForestArea(Phase area)
    {
        if (area is not (Phase.ForestEntry or Phase.Forest or Phase.ForestStream))
            return;

        _phase = area;
        _selectedIndex = 0;
        _actionMessage = "";
        _actionMessageTimer = 0;

        switch (area)
        {
            case Phase.ForestEntry:
                _location = "Forest Entry";
                _temperatureF = 22;
                _backgroundTexture = _forestEntryBackground;
                break;
            case Phase.Forest:
                _location = "Deep Forest";
                _temperatureF = 19;
                _backgroundTexture = _forestBackground;
                if (_status == "On the Run")
                {
                    _day = 3;
                    _timeOfDay = "Morning";
                    _status = "Fugitive";
                }
                break;
            case Phase.ForestStream:
                _location = "Forest Stream";
                _temperatureF = 17;
                _backgroundTexture = _forestStreamBackground;
                break;
        }

        ClearEnvDeltas();
        RefreshOutdoorComfortEnvironment();
        RefreshConcealment();
        RefreshOutdoorActionChoices();
    }

    // --- Time of day ---
    /// <summary>
    /// Advances the time of day by the given number of slots.
    /// Wrapping from Late Night to Morning starts a new day.
    /// </summary>
    private int GetEnergyDrainForTimeSlot(int slotIndex) =>
        EnergyDrainBasePerTimeSlot + slotIndex * EnergyDrainIncreasePerSlot;

    private void ApplyTravelEnergyCost(int cost = EnergyCostTravel)
    {
        if (cost > 0)
            ModifyStatFromAction(ref _energy, ref _actionEnergyDelta, -cost);
    }

    private void AdvanceTime(int steps = 1)
    {
        if (steps <= 0) return;

        int idx = GetTimeSlotIndex();

        for (int s = 0; s < steps; s++)
        {
            int newIdx = (idx + 1) % _timeSlots.Length;
            if (newIdx < idx)
                _day++;
            idx = newIdx;
            _timeOfDay = _timeSlots[idx];
            ModifyStatFromAction(ref _energy, ref _actionEnergyDelta, -GetEnergyDrainForTimeSlot(idx));
        }

        // Temperature drifts with time of day (colder at night) — only outside the apartment
        if (_phase == Phase.Outside || GamePhase.IsTownDistrict(_phase) || GamePhase.IsForestSurvival(_phase))
        {
            if (IsNightTimeSlot())
                _temperatureF = Math.Max(-40, _temperatureF - 2);
            else if (IsMorningTimeSlot())
                _temperatureF = Math.Min(60, _temperatureF + 1);

            if (_phase is Phase.Outside || GamePhase.IsTownDistrict(_phase))
                RefreshOutdoorComfortEnvironment();
        }

        RefreshConcealment();
        TickDroppedItemsInCurrentRoom(steps);
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

    // --- Embedded textures & item icons ---
    private void LoadItemIcons()
    {
        foreach (var (itemName, fileName) in GameItems.IconFiles)
            _itemIcons[itemName] = EmbeddedTextureLoader.Load(fileName);
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

    private int GetBackpackSlotCharges(int slotIndex, string itemName)
    {
        if (slotIndex >= 0 && slotIndex < _backpackItemCharges.Length && _backpackItemCharges[slotIndex] is int stored)
            return stored;
        return GameItems.GetMaxCharges(itemName);
    }

    private string GetBottledWaterDialogText(int slotIndex)
    {
        int remaining = slotIndex >= 0
            ? GetBackpackSlotCharges(slotIndex, GameItems.BottledWater)
            : GameItems.BottledWaterMaxSips;
        string baseText = remaining >= GameItems.BottledWaterMaxSips
            ? $"A full bottle — {GameItems.BottledWaterMaxSips} sips. Each sip restores hydration."
            : remaining == 1
                ? "One sip left. Drink it before the bottle is empty."
                : $"{remaining} sips left. Each sip restores some hydration.";
        if (_phase == Phase.ForestStream && remaining < GameItems.BottledWaterMaxSips)
            baseText += " You can top it off at the stream.";
        return baseText;
    }

    private string GetCannedSoupDialogText(int slotIndex)
    {
        int remaining = slotIndex >= 0
            ? GetBackpackSlotCharges(slotIndex, GameItems.CannedSoup)
            : GameItems.CannedSoupMaxServings;
        return remaining >= GameItems.CannedSoupMaxServings
            ? $"A sealed can — {GameItems.CannedSoupMaxServings} servings. Each serving restores some stats."
            : remaining == 1
                ? "One serving left. Eat it before you toss the can."
                : $"{remaining} servings left. Each serving restores some stats.";
    }

    private int GetItemChargesForDisplay(string itemName, int slotIndex, int? chargesOverride)
    {
        if (chargesOverride is int c)
            return c;
        if (slotIndex >= 0)
            return GetBackpackSlotCharges(slotIndex, itemName);
        return GameItems.GetMaxCharges(itemName);
    }

    private void DrawItemIcon(string itemName, Rectangle dest, Color tint, int slotIndex = -1, int? chargesOverride = null)
    {
        if (!_itemIcons.TryGetValue(itemName, out Texture2D tex) || tex.Id == 0)
            return;

        int maxCharges = GameItems.GetMaxCharges(itemName);
        if (maxCharges > 0)
        {
            int remaining = GetItemChargesForDisplay(itemName, slotIndex, chargesOverride);
            if (remaining > 0 && remaining < maxCharges)
            {
                ItemIconDrawing.DrawPartialCharge(tex, dest, tint, remaining, maxCharges);
                return;
            }
        }

        Rectangle src = new Rectangle(0, 0, tex.Width, tex.Height);
        Raylib.DrawTexturePro(tex, src, dest, Vector2.Zero, 0f, tint);
    }

    /// <summary>Outdoor scenes and the trash-bag tent interior (light leaks through the plastic).</summary>
    private bool SceneUsesTimeOfDayLighting() =>
        GamePhase.IsOutdoor(_phase) || _phase == Phase.Tent || _phase == Phase.DeliveryTruck
        || _phase is Phase.WarehouseTruck or Phase.WarehouseAmbush;

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
            Color tint = SceneUsesTimeOfDayLighting() ? GetOutdoorTimeOfDayTint() : Color.WHITE;
            Rectangle src = new Rectangle(0, 0, _backgroundTexture.Width, _backgroundTexture.Height);
            Rectangle dst = new Rectangle(artX, artY, artW, artH);
            Raylib.DrawTexturePro(_backgroundTexture, src, dst, Vector2.Zero, 0.0f, tint);

            if (SceneUsesTimeOfDayLighting())
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

    // --- Lifecycle (Run / shutdown) ---
    public void Run()
    {
        Raylib.InitWindow(_screenWidth, _screenHeight, "CONSCRIPT");
        Raylib.SetTargetFPS(60);
        Raylib.SetExitKey(KeyboardKey.KEY_NULL); // we handle ESC ourselves

        InputManager.Initialize();

        _uiFont = UiFontLoader.Load();
        _uiFontItalic = UiFontLoader.LoadItalic();
        _apartmentBackground = EmbeddedTextureLoader.Load("apartment-inside.png");
        _outsideBackground   = EmbeddedTextureLoader.Load("apartment-outside.png");
        _townBackground        = EmbeddedTextureLoader.Load("town.png");
        _industrialDistrictBackground = LoadTextureOrFallback("industrial.png", _townBackground);
        _commercialDistrictBackground = LoadTextureOrFallback("commercial.png", _townBackground);
        _forestEntryBackground  = EmbeddedTextureLoader.Load("forest-entry.png");
        _forestBackground       = EmbeddedTextureLoader.Load("trees.png");
        _forestStreamBackground = EmbeddedTextureLoader.Load("forest-stream.png");
        _storeBackground        = EmbeddedTextureLoader.Load("store.png");  // dedicated store interior photo (bright fluorescent kiosk)
        _cafeBackground         = LoadTextureOrFallback("cafe.png", _storeBackground);
        _deliveryTruckBackground = LoadTextureOrFallback("delivery-truck-cab.png", _industrialDistrictBackground);
        _warehouseBackground = LoadTextureOrFallback("warehouse-14.png", _industrialDistrictBackground);
        _warehouseAmbushBackground = LoadTextureOrFallback("warehouse-14-ambush.png", _warehouseBackground);
        _cafeOwnerPortraitTexture = EmbeddedTextureLoader.Load("cafe-owner-portrait.png");
        _tentBackground      = EmbeddedTextureLoader.Load("tent-interior.png");
        _regionMapTexture    = EmbeddedTextureLoader.Load("region-map.png");
        _trashBagTentTexture = EmbeddedTextureLoader.Load("trash-bag-tent.png");
        _titleLogoTexture    = EmbeddedTextureLoader.Load("conscript-title.png");
        LoadItemIcons();
        EnterPhase(Phase.Opening);  // EnterPhase will pick the correct background for the starting phase

        while (!ShouldExit && !Raylib.WindowShouldClose())
        {
            Update();
            Draw();
        }

        UnloadSceneTextures();
        UnloadItemIcons();

        Raylib.CloseWindow();
    }

    private static void UnloadTextureIfLoaded(ref Texture2D texture)
    {
        if (texture.Id != 0)
            Raylib.UnloadTexture(texture);
        texture = default;
    }

    private static Texture2D LoadTextureOrFallback(string fileName, Texture2D fallback)
    {
        try
        {
            return EmbeddedTextureLoader.Load(fileName);
        }
        catch (FileNotFoundException)
        {
            return fallback;
        }
    }

    private void UnloadSceneTextures()
    {
        UnloadTextureIfLoaded(ref _apartmentBackground);
        UnloadTextureIfLoaded(ref _outsideBackground);
        UnloadTextureIfLoaded(ref _townBackground);
        UnloadTextureIfLoaded(ref _industrialDistrictBackground);
        UnloadTextureIfLoaded(ref _commercialDistrictBackground);
        UnloadTextureIfLoaded(ref _forestEntryBackground);
        UnloadTextureIfLoaded(ref _forestBackground);
        UnloadTextureIfLoaded(ref _forestStreamBackground);
        UnloadTextureIfLoaded(ref _storeBackground);
        UnloadTextureIfLoaded(ref _cafeBackground);
        UnloadTextureIfLoaded(ref _deliveryTruckBackground);
        UnloadTextureIfLoaded(ref _warehouseBackground);
        UnloadTextureIfLoaded(ref _warehouseAmbushBackground);
        UnloadTextureIfLoaded(ref _cafeOwnerPortraitTexture);
        UnloadTextureIfLoaded(ref _tentBackground);
        UnloadTextureIfLoaded(ref _regionMapTexture);
        UnloadTextureIfLoaded(ref _trashBagTentTexture);
        UnloadTextureIfLoaded(ref _titleLogoTexture);
    }

    // --- Main loop ---
    private void Update()
    {
        float dt = Raylib.GetFrameTime();
        InputManager.RefreshGamepad();

        if (InputManager.IsCancelPressed() || Raylib.IsKeyPressed(KeyboardKey.KEY_Q))
        {
            if (_showItemDialog)
            {
                CloseItemDialog();
                return;
            }
            if (_showStoreBuyMenu)
            {
                CloseStoreBuyMenu();
                return;
            }
            if (_showGloveBoxMenu)
            {
                CloseGloveBoxMenu();
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
            if (_showForageDialog)
            {
                CloseForageDialog();
                return;
            }
            if (_showCafeOwnerDialog)
            {
                CloseCafeOwnerDialog();
                return;
            }
            if (_showControllerDebug)
            {
                CloseControllerDebug();
                return;
            }
            if (_showQuitConfirm)
            {
                CloseQuitConfirm();
                return;
            }
            if (_showStatsHelp)
            {
                CloseStatsHelp();
                return;
            }
            if (InputManager.IsCancelPressed())
            {
                OpenQuitConfirm();
                return;
            }
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_Q))
                _shouldExit = true;
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_R))
        {
            RestartGame();
            return;
        }

        if (_showQuitConfirm)
        {
            if (InputManager.IsHorizontalNavLeftPressed())
                _quitConfirmSelectedButton = 0;
            if (InputManager.IsHorizontalNavRightPressed())
                _quitConfirmSelectedButton = 1;
            if (InputManager.IsConfirmPressed())
            {
                if (_quitConfirmSelectedButton == 1)
                    _shouldExit = true;
                else
                    CloseQuitConfirm();
                return;
            }
        }

        if (_showStatsHelp && InputManager.IsConfirmPressed())
        {
            CloseStatsHelp();
            return;
        }

        if (_showStoreBuyMenu)
        {
            if (InputManager.IsVerticalNavUpPressed())
            {
                _storeBuyHighlightedIndex = (_storeBuyHighlightedIndex - 1 + StoreCatalog.Entries.Length) % StoreCatalog.Entries.Length;
                _storeBuyDetailIndex = _storeBuyHighlightedIndex;
            }
            if (InputManager.IsVerticalNavDownPressed())
            {
                _storeBuyHighlightedIndex = (_storeBuyHighlightedIndex + 1) % StoreCatalog.Entries.Length;
                _storeBuyDetailIndex = _storeBuyHighlightedIndex;
            }
        }

        if (_showGloveBoxMenu)
        {
            RefreshGloveBoxVisibleList();

            if (InputManager.IsVerticalNavUpPressed())
            {
                if (_gloveBoxVisibleCount > 0)
                {
                    _gloveBoxHighlightedIndex = (_gloveBoxHighlightedIndex - 1 + _gloveBoxVisibleCount) % _gloveBoxVisibleCount;
                    _gloveBoxDetailIndex = _gloveBoxHighlightedIndex;
                }
            }
            if (InputManager.IsVerticalNavDownPressed())
            {
                if (_gloveBoxVisibleCount > 0)
                {
                    _gloveBoxHighlightedIndex = (_gloveBoxHighlightedIndex + 1) % _gloveBoxVisibleCount;
                    _gloveBoxDetailIndex = _gloveBoxHighlightedIndex;
                }
            }
        }

        if (_showForageDialog)
        {
            if (InputManager.IsVerticalNavUpPressed())
                _forageHighlightedIndex = (_forageHighlightedIndex - 1 + ForageOptionCount) % ForageOptionCount;
            if (InputManager.IsVerticalNavDownPressed())
                _forageHighlightedIndex = (_forageHighlightedIndex + 1) % ForageOptionCount;
        }

        if (_showCafeOwnerDialog)
        {
            int cafeDialogOptions = CafeOwnerDialog.GetOptionCount(_cafeOwnerDialogStage);
            if (InputManager.IsVerticalNavUpPressed())
                _cafeOwnerHighlightedIndex = (_cafeOwnerHighlightedIndex - 1 + cafeDialogOptions) % cafeDialogOptions;
            if (InputManager.IsVerticalNavDownPressed())
                _cafeOwnerHighlightedIndex = (_cafeOwnerHighlightedIndex + 1) % cafeDialogOptions;
        }

        // Horizontal navigation for bottom action buttons
        if (!BlocksActionBarNavigation() && _choices.Length > 0)
        {
            if (InputManager.IsHorizontalNavRightPressed())
                _selectedIndex = (_selectedIndex + 1) % _choices.Length;
            if (InputManager.IsHorizontalNavLeftPressed())
                _selectedIndex = (_selectedIndex - 1 + _choices.Length) % _choices.Length;
        }

        if (InputManager.IsConfirmPressed())
        {
            if (_showControllerDebug)
            {
                CloseControllerDebug();
            }
            else if (_showRegionMap)
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
            else if (_showForageDialog)
            {
                TryPerformForage(_forageHighlightedIndex);
            }
            else if (_showCafeOwnerDialog)
            {
                SelectCafeOwnerOption(_cafeOwnerHighlightedIndex);
            }
            else if (_showStoreBuyMenu)
            {
                if (_storeBuyPurchaseHovered && _storeBuyDetailIndex >= 0)
                    TryBuyStoreItem(_storeBuyDetailIndex);
                else if (_storeBuyDetailIndex == _storeBuyHighlightedIndex && _storeBuyDetailIndex >= 0)
                    TryBuyStoreItem(_storeBuyDetailIndex);
                else
                    _storeBuyDetailIndex = _storeBuyHighlightedIndex;
            }
            else if (_showGloveBoxMenu)
            {
                int catalogIndex = GetGloveBoxCatalogIndexFromVisibleIndex(_gloveBoxDetailIndex);
                int highlightedCatalogIndex = GetGloveBoxCatalogIndexFromVisibleIndex(_gloveBoxHighlightedIndex);

                if (_gloveBoxPickupHovered && catalogIndex >= 0)
                    TryTakeGloveBoxItem(catalogIndex);
                else if (_gloveBoxDetailIndex == _gloveBoxHighlightedIndex && highlightedCatalogIndex >= 0)
                    TryTakeGloveBoxItem(highlightedCatalogIndex);
                else
                    _gloveBoxDetailIndex = _gloveBoxHighlightedIndex;
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
        _controllerHovered = Raylib.CheckCollisionPointRec(mouse, _controllerButtonRect);
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
        if (leftClicked && _controllerHovered)
        {
            if (_showControllerDebug)
                CloseControllerDebug();
            else
                OpenControllerDebug();
            return;
        }

        // === Stats help (modal) ===
        if (_showStatsHelp)
        {
            _statsHelpCloseHovered = Raylib.CheckCollisionPointRec(mouse, _statsHelpCloseRect);

            if (leftClicked && _statsHelpCloseHovered)
            {
                CloseStatsHelp();
                return;
            }

            if (leftClicked && !Raylib.CheckCollisionPointRec(mouse, _statsHelpPanelRect))
            {
                CloseStatsHelp();
                return;
            }

            return;
        }

        // === Quit confirmation (modal) ===
        if (_showQuitConfirm)
        {
            bool mouseOverYes = Raylib.CheckCollisionPointRec(mouse, _quitConfirmYesRect);
            bool mouseOverNo = Raylib.CheckCollisionPointRec(mouse, _quitConfirmNoRect);
            if (mouseOverYes)
                _quitConfirmSelectedButton = 1;
            else if (mouseOverNo)
                _quitConfirmSelectedButton = 0;

            _quitConfirmYesHovered = mouseOverYes || _quitConfirmSelectedButton == 1;
            _quitConfirmNoHovered = mouseOverNo || _quitConfirmSelectedButton == 0;

            if (leftClicked && _quitConfirmYesHovered)
            {
                _shouldExit = true;
                return;
            }
            if (leftClicked && (_quitConfirmNoHovered ||
                !Raylib.CheckCollisionPointRec(mouse, _quitConfirmPanelRect)))
            {
                CloseQuitConfirm();
                return;
            }
            return;
        }

        // === Controller debug (modal) ===
        if (_showControllerDebug)
        {
            var panelRect = new Rectangle(36, 28, _screenWidth - 72, _screenHeight - 56);
            _controllerDebugCloseHovered = Raylib.CheckCollisionPointRec(mouse, _controllerDebugCloseRect);
            _controllerDebugPrevHovered = Raylib.CheckCollisionPointRec(mouse, _controllerDebugPrevRect);
            _controllerDebugNextHovered = Raylib.CheckCollisionPointRec(mouse, _controllerDebugNextRect);
            for (int i = 0; i < GamepadDebugLayout.MaxGamepadsToShow; i++)
                _controllerDebugTabHovered[i] = Raylib.CheckCollisionPointRec(mouse, _controllerDebugTabRects[i]);

            if (InputManager.IsHorizontalNavLeftPressed())
                CycleControllerDebugPad(-1);
            if (InputManager.IsHorizontalNavRightPressed())
                CycleControllerDebugPad(1);

            if (leftClicked && _controllerDebugCloseHovered)
            {
                CloseControllerDebug();
                return;
            }
            if (leftClicked && _controllerDebugPrevHovered)
            {
                CycleControllerDebugPad(-1);
                return;
            }
            if (leftClicked && _controllerDebugNextHovered)
            {
                CycleControllerDebugPad(1);
                return;
            }
            for (int i = 0; i < GamepadDebugLayout.MaxGamepadsToShow; i++)
            {
                if (leftClicked && _controllerDebugTabHovered[i])
                {
                    _controllerDebugPadIndex = i;
                    return;
                }
            }
            if (leftClicked && !Raylib.CheckCollisionPointRec(mouse, panelRect) &&
                !Raylib.CheckCollisionPointRec(mouse, _restartButtonRect) &&
                !Raylib.CheckCollisionPointRec(mouse, _debugStartButtonRect) &&
                !Raylib.CheckCollisionPointRec(mouse, _controllerButtonRect))
            {
                CloseControllerDebug();
                return;
            }
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
            bool canDisassembleTent = CanDisassembleTrashBagTent(out _);
            bool canCraftMolotov = CanCraftMolotov(out _);
            bool canCraftLitMolotov = CanCraftLitMolotov(out _);
            _buildTentButtonHovered =
                (_hasTrashBagTent ? canDisassembleTent : canBuildTent) &&
                Raylib.CheckCollisionPointRec(mouse, _buildTentButtonRect);
            _buildMolotovButtonHovered =
                canCraftMolotov &&
                Raylib.CheckCollisionPointRec(mouse, _buildMolotovButtonRect);
            _buildLitMolotovButtonHovered =
                canCraftLitMolotov &&
                Raylib.CheckCollisionPointRec(mouse, _buildLitMolotovButtonRect);

            if (leftClicked && _buildTentButtonHovered)
            {
                if (_hasTrashBagTent)
                    TryDisassembleTrashBagTent();
                else
                    TryBuildTrashBagTent();
                return;
            }

            if (leftClicked && _buildMolotovButtonHovered)
            {
                TryCraftMolotov();
                return;
            }

            if (leftClicked && _buildLitMolotovButtonHovered)
            {
                TryCraftLitMolotov();
                return;
            }

            if (leftClicked && Raylib.CheckCollisionPointRec(mouse, _buildTentRowRect))
            {
                if (_hasTrashBagTent)
                    TryDisassembleTrashBagTent();
                else if (canBuildTent)
                    TryBuildTrashBagTent();
                return;
            }

            if (leftClicked && Raylib.CheckCollisionPointRec(mouse, _buildMolotovRowRect))
            {
                if (canCraftMolotov)
                    TryCraftMolotov();
                return;
            }

            if (leftClicked && Raylib.CheckCollisionPointRec(mouse, _buildLitMolotovRowRect))
            {
                if (canCraftLitMolotov)
                    TryCraftLitMolotov();
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

        // === Forage dialog (modal) ===
        if (_showForageDialog)
        {
            _forageCloseHovered = Raylib.CheckCollisionPointRec(mouse, _forageCloseRect);
            for (int i = 0; i < ForageOptionCount; i++)
            {
                _forageOptionHovered[i] = Raylib.CheckCollisionPointRec(mouse, _forageOptionRowRects[i]);
                if (_forageOptionHovered[i])
                    _forageHighlightedIndex = i;
            }

            if (leftClicked)
            {
                for (int i = 0; i < ForageOptionCount; i++)
                {
                    if (_forageOptionHovered[i])
                    {
                        TryPerformForage(i);
                        return;
                    }
                }

                if (_forageCloseHovered)
                {
                    CloseForageDialog();
                    return;
                }

                if (!Raylib.CheckCollisionPointRec(mouse, _foragePanelRect))
                {
                    CloseForageDialog();
                    return;
                }
            }
        }

        // === Café owner dialog (modal) ===
        if (_showCafeOwnerDialog)
        {
            int cafeDialogOptions = CafeOwnerDialog.GetOptionCount(_cafeOwnerDialogStage);
            _cafeOwnerCloseHovered = Raylib.CheckCollisionPointRec(mouse, _cafeOwnerCloseRect);
            for (int i = 0; i < cafeDialogOptions; i++)
            {
                _cafeOwnerOptionHovered[i] = Raylib.CheckCollisionPointRec(mouse, _cafeOwnerOptionRowRects[i]);
                if (_cafeOwnerOptionHovered[i])
                    _cafeOwnerHighlightedIndex = i;
            }

            if (leftClicked)
            {
                for (int i = 0; i < cafeDialogOptions; i++)
                {
                    if (_cafeOwnerOptionHovered[i])
                    {
                        SelectCafeOwnerOption(i);
                        return;
                    }
                }

                if (_cafeOwnerCloseHovered)
                {
                    CloseCafeOwnerDialog();
                    return;
                }

                if (!Raylib.CheckCollisionPointRec(mouse, _cafeOwnerPanelRect))
                {
                    CloseCafeOwnerDialog();
                    return;
                }
            }
        }

        // === Item dialog (highest priority when visible) ===
        if (_showItemDialog)
        {
            bool isGround = IsDroppedItemDialog;
            bool canDrink = !isGround && CanDrinkFromDialogSlot(_dialogItemIndex);
            bool canFill = !isGround && CanFillBottleAtStream(_dialogItemIndex);
            DialogItemAction eatAction = isGround
                ? DialogItemAction.None
                : GetDialogItemAction(_dialogItemName, _dialogItemIndex);
            bool canAct = !isGround && (canDrink || canFill || eatAction == DialogItemAction.EatSoup);

            _dialogActionHovered = _dialogActionRect.Width > 0 &&
                Raylib.CheckCollisionPointRec(mouse, _dialogActionRect) &&
                (isGround || canDrink || (canFill && !canDrink) || eatAction == DialogItemAction.EatSoup);
            _dialogSecondaryActionHovered = _dialogSecondaryActionRect.Width > 0 &&
                canDrink && canFill &&
                Raylib.CheckCollisionPointRec(mouse, _dialogSecondaryActionRect);
            _dialogDropHovered = _dialogDropRect.Width > 0 &&
                Raylib.CheckCollisionPointRec(mouse, _dialogDropRect);
            _dialogCloseHovered = Raylib.CheckCollisionPointRec(mouse, _dialogCloseRect);

            if (leftClicked && _dialogActionHovered)
            {
                if (isGround)
                    TryPickupDroppedItem();
                else if (canDrink)
                    TryPerformDialogItemAction(DialogItemAction.DrinkWater);
                else if (canFill)
                    TryPerformDialogItemAction(DialogItemAction.FillBottle);
                else if (eatAction == DialogItemAction.EatSoup)
                    TryPerformDialogItemAction(DialogItemAction.EatSoup);
                return;
            }
            if (leftClicked && _dialogSecondaryActionHovered)
            {
                TryPerformDialogItemAction(DialogItemAction.FillBottle);
                return;
            }
            if (leftClicked && _dialogDropHovered)
            {
                TryDropItemFromBackpack();
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
            bool mouseOverBuy = Raylib.CheckCollisionPointRec(mouse, _storeBuyPurchaseRect);
            _storeBuyPurchaseHovered = mouseOverBuy;
        }
        else
        {
            _storeBuyCloseHovered = false;
            _storeBuyPurchaseHovered = false;
        }

        if (_showGloveBoxMenu)
        {
            _gloveBoxCloseHovered = Raylib.CheckCollisionPointRec(mouse, _gloveBoxCloseRect);
            _gloveBoxPickupHovered = Raylib.CheckCollisionPointRec(mouse, _gloveBoxPickupRect);
        }
        else
        {
            _gloveBoxCloseHovered = false;
            _gloveBoxPickupHovered = false;
        }

        if (AllowsSidebarAndSceneInput())
        {
            _statsHelpIconHovered = _statsHelpIconRect.Width > 0 &&
                Raylib.CheckCollisionPointRec(mouse, _statsHelpIconRect);
            _regionMapThumbHovered = _regionMapClickRect.Width > 0 &&
                Raylib.CheckCollisionPointRec(mouse, _regionMapClickRect);
            _buildSidebarButtonHovered = _buildSidebarButtonRect.Width > 0 &&
                Raylib.CheckCollisionPointRec(mouse, _buildSidebarButtonRect);
            _huntSidebarButtonHovered = _huntSidebarButtonRect.Width > 0 &&
                Raylib.CheckCollisionPointRec(mouse, _huntSidebarButtonRect);
            _forageSidebarButtonHovered = _forageSidebarButtonRect.Width > 0 &&
                Raylib.CheckCollisionPointRec(mouse, _forageSidebarButtonRect);
            _quitSidebarButtonHovered = _quitSidebarButtonRect.Width > 0 &&
                Raylib.CheckCollisionPointRec(mouse, _quitSidebarButtonRect);

            if (leftClicked && _statsHelpIconHovered)
            {
                OpenStatsHelp();
                return;
            }

            if (leftClicked && _buildSidebarButtonHovered)
            {
                OpenBuildDialog();
                return;
            }

            if (leftClicked && _huntSidebarButtonHovered)
            {
                ClearActionDeltas();
                PerformHunt();
                return;
            }

            if (leftClicked && _forageSidebarButtonHovered)
            {
                OpenForageDialog();
                return;
            }

            if (leftClicked && _quitSidebarButtonHovered)
            {
                OpenQuitConfirm();
                return;
            }

            if (leftClicked && _regionMapThumbHovered)
            {
                OpenRegionMap();
                return;
            }

            if (_hasTrashBagTent && GamePhase.IsOutdoorsSurvival(_phase) && _trashBagTentClickRect.Width > 0)
            {
                _trashBagTentHovered = Raylib.CheckCollisionPointRec(mouse, _trashBagTentClickRect);
                if (leftClicked && _trashBagTentHovered)
                {
                    EnterTent();
                    return;
                }
            }
            else
            {
                _trashBagTentHovered = false;
            }

            if (_phase == Phase.DeliveryTruck)
            {
                if (!GloveCompartmentHasRemainingLoot())
                    _gloveCompartmentClickRect = default;
                else
                {
                    GetCinematicArtBounds(out int ax, out int ay, out int aw, out int ah);
                    _gloveCompartmentClickRect = ComputeDeliveryTruckGloveBoxClickRect(ax, ay, aw, ah);
                }

                if (_gloveCompartmentClickRect.Width > 0)
                {
                    _gloveCompartmentHovered = Raylib.CheckCollisionPointRec(mouse, _gloveCompartmentClickRect);
                    if (leftClicked && _gloveCompartmentHovered)
                    {
                        OpenGloveBoxMenu();
                        return;
                    }
                }
                else
                    _gloveCompartmentHovered = false;
            }
            else
            {
                _gloveCompartmentClickRect = default;
                _gloveCompartmentHovered = false;
            }

            _hoveredDroppedItemListIndex = -1;
            for (int i = 0; i < _droppedItemClickRects.Count; i++)
            {
                if (Raylib.CheckCollisionPointRec(mouse, _droppedItemClickRects[i]))
                {
                    _hoveredDroppedItemListIndex = i;
                    if (leftClicked)
                    {
                        OpenDroppedItemDialog(_droppedItemVisibleIndices[i]);
                        return;
                    }
                    break;
                }
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
            for (int i = 0; i < StoreCatalog.Entries.Length; i++)
            {
                if (Raylib.CheckCollisionPointRec(mouse, _storeBuyItemRects[i]))
                {
                    _storeBuyHighlightedIndex = i;
                    if (leftClicked)
                        _storeBuyDetailIndex = i;
                    break;
                }
            }

            if (leftClicked && _storeBuyPurchaseHovered && _storeBuyDetailIndex >= 0)
            {
                TryBuyStoreItem(_storeBuyDetailIndex);
                return;
            }

            if (leftClicked && Raylib.CheckCollisionPointRec(mouse, _storeBuyCloseRect))
            {
                CloseStoreBuyMenu();
                return;
            }

            if (leftClicked && !Raylib.CheckCollisionPointRec(mouse, _storeBuyPanelRect))
            {
                CloseStoreBuyMenu();
                return;
            }
        }

        if (_showGloveBoxMenu)
        {
            RefreshGloveBoxVisibleList();

            for (int i = 0; i < _gloveBoxVisibleCount; i++)
            {
                if (Raylib.CheckCollisionPointRec(mouse, _gloveBoxItemRects[i]))
                {
                    _gloveBoxHighlightedIndex = i;
                    if (leftClicked)
                        _gloveBoxDetailIndex = i;
                    break;
                }
            }

            if (leftClicked && _gloveBoxDetailIndex >= 0 &&
                (_gloveBoxPickupHovered || Raylib.CheckCollisionPointRec(mouse, _gloveBoxPickupRect)))
            {
                int catalogIndex = GetGloveBoxCatalogIndexFromVisibleIndex(_gloveBoxDetailIndex);
                if (catalogIndex >= 0)
                    TryTakeGloveBoxItem(catalogIndex);
                return;
            }

            if (leftClicked && Raylib.CheckCollisionPointRec(mouse, _gloveBoxCloseRect))
            {
                CloseGloveBoxMenu();
                return;
            }

            if (leftClicked && !Raylib.CheckCollisionPointRec(mouse, _gloveBoxPanelRect))
            {
                CloseGloveBoxMenu();
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

        if (_showStoreBuyMenu)
            GameStatMath.TickTimedMessage(ref _storeBuyFeedbackTimer, ref _storeBuyFeedback, dt);

        if (_showGloveBoxMenu)
            GameStatMath.TickTimedMessage(ref _gloveBoxFeedbackTimer, ref _gloveBoxFeedback, dt);

        if (_showBuildDialog)
            GameStatMath.TickTimedMessage(ref _buildFeedbackTimer, ref _buildFeedback, dt);

        // === Update mouse cursor to indicate clickable elements ===
        bool overClickable = false;

        // Top-right utility buttons (always available)
        if (Raylib.CheckCollisionPointRec(mouse, _restartButtonRect) ||
            Raylib.CheckCollisionPointRec(mouse, _debugStartButtonRect) ||
            Raylib.CheckCollisionPointRec(mouse, _controllerButtonRect) ||
            (_showControllerDebug && (
                Raylib.CheckCollisionPointRec(mouse, _controllerDebugCloseRect) ||
                Raylib.CheckCollisionPointRec(mouse, _controllerDebugPrevRect) ||
                Raylib.CheckCollisionPointRec(mouse, _controllerDebugNextRect) ||
                _controllerDebugTabHovered.Any(h => h))))
            overClickable = true;

        if (AllowsSidebarAndSceneInput())
        {
            if (_statsHelpIconHovered || _regionMapThumbHovered || _buildSidebarButtonHovered ||
                _huntSidebarButtonHovered || _forageSidebarButtonHovered || _quitSidebarButtonHovered)
                overClickable = true;

            if (_trashBagTentHovered)
                overClickable = true;

            if (_gloveCompartmentHovered)
                overClickable = true;

            if (_hoveredDroppedItemListIndex >= 0)
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
                Raylib.CheckCollisionPointRec(mouse, _dialogSecondaryActionRect) ||
                Raylib.CheckCollisionPointRec(mouse, _dialogDropRect) ||
                !Raylib.CheckCollisionPointRec(mouse, _dialogPanelRect))
            {
                overClickable = true;
            }
        }

        // Store buy menu: list rows, buy/close buttons, or overlay
        if (_showStoreBuyMenu)
        {
            if (_storeBuyCloseHovered || _storeBuyPurchaseHovered ||
                Raylib.CheckCollisionPointRec(mouse, _storeBuyPanelRect) ||
                !Raylib.CheckCollisionPointRec(mouse, _storeBuyPanelRect))
                overClickable = true;

            for (int i = 0; i < _storeBuyItemRects.Length; i++)
            {
                if (Raylib.CheckCollisionPointRec(mouse, _storeBuyItemRects[i]))
                {
                    overClickable = true;
                    break;
                }
            }
        }

        if (_showGloveBoxMenu)
        {
            if (_gloveBoxCloseHovered || _gloveBoxPickupHovered ||
                Raylib.CheckCollisionPointRec(mouse, _gloveBoxPanelRect) ||
                !Raylib.CheckCollisionPointRec(mouse, _gloveBoxPanelRect))
                overClickable = true;

            for (int i = 0; i < _gloveBoxItemRects.Length; i++)
            {
                if (Raylib.CheckCollisionPointRec(mouse, _gloveBoxItemRects[i]))
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

        if (_showForageDialog)
        {
            if (_forageCloseHovered || _forageOptionHovered.Any(h => h) ||
                !Raylib.CheckCollisionPointRec(mouse, _foragePanelRect))
                overClickable = true;
        }

        if (_showCafeOwnerDialog)
        {
            if (_cafeOwnerCloseHovered || _cafeOwnerOptionHovered.Any(h => h) ||
                !Raylib.CheckCollisionPointRec(mouse, _cafeOwnerPanelRect))
                overClickable = true;
        }

        if (_showQuitConfirm &&
            (_quitConfirmYesHovered || _quitConfirmNoHovered))
            overClickable = true;

        if (_showStatsHelp &&
            (_statsHelpCloseHovered || !Raylib.CheckCollisionPointRec(mouse, _statsHelpPanelRect)))
            overClickable = true;

        Raylib.SetMouseCursor(overClickable
            ? MouseCursor.MOUSE_CURSOR_POINTING_HAND
            : MouseCursor.MOUSE_CURSOR_DEFAULT);
    }

    // --- Choice handlers ---
    private void PerformChoice(int index)
    {
        ClearActionDeltas();
        switch (_phase)
        {
            case Phase.Opening:
                HandleOpeningChoice(index);
                break;

            case Phase.ForestEntry:
                HandleForestEntryChoice(index);
                break;

            case Phase.Forest:
                HandleForestChoice(index);
                break;

            case Phase.ForestStream:
                HandleForestStreamChoice(index);
                break;

            case Phase.Outside:
                HandleOutsideChoice(index);
                break;

            case Phase.Town:
                HandleTownChoice(index);
                break;

            case Phase.IndustrialDistrict:
                HandleIndustrialDistrictChoice(index);
                break;

            case Phase.CommercialDistrict:
                HandleCommercialDistrictChoice(index);
                break;

            case Phase.Store:
                HandleStoreChoice(index);
                break;

            case Phase.Cafe:
                HandleCafeChoice(index);
                break;

            case Phase.DeliveryTruck:
                HandleDeliveryTruckChoice(index);
                break;

            case Phase.WarehouseTruck:
                HandleWarehouseTruckChoice(index);
                break;

            case Phase.WarehouseAmbush:
                HandleWarehouseAmbushChoice(index);
                break;

            case Phase.Tent:
                HandleTentChoice(index);
                break;

            case Phase.Death:
                if (index >= 0 && index < _choices.Length && _choices[index] == ChoiceTryAgain)
                    EnterPhase(Phase.Opening);
                break;
        }
    }

    private void HandleOpeningChoice(int index)
    {
        if (index < 0 || index >= _choices.Length)
            return;

        switch (_choices[index])
        {
            case ChoiceOpenDoor:
                EnterDeath("You opened the door.", "Conscripted. Dead on the front three weeks later.");
                return;

            case ChoiceFleeOutWindow:
                _actionMessage = "You climb out the window and drop into the yard behind the block.";
                _actionMessageTimer = 2.5f;
                AdvanceTime();   // the climb and landing take a moment
                ApplyTravelEnergyCost();
                EnterPhase(Phase.Outside);
                break;

            case ChoiceBarDoorAndFight:
                EnterDeath(DefaultDeathLine1, DefaultDeathLine2);
                break;
        }
    }

    private void HandleForestEntryChoice(int index)
    {
        if (index < 0 || index >= _choices.Length)
            return;

        switch (_choices[index])
        {
            case ChoiceFollowStream:
                _actionMessage = "You pick your way downhill toward the sound of running water.";
                _actionMessageTimer = 2.5f;
                AdvanceTime();
                ApplyTravelEnergyCost(EnergyCostTravelShort);
                ApplyEnvironmentOnAction();
                EnterForestArea(Phase.ForestStream);
                break;

            case ChoiceGoBackToTown:
                ApplyTravelEnergyCost();
                AdvanceTime();
                EnterPhase(Phase.Town);
                break;

            case ChoiceEnterTent:
                EnterTent();
                break;

            case ChoiceDisassembleTent:
                TryDisassembleTrashBagTent();
                break;

            case ChoiceWait:
                PerformIdle();
                break;
        }
    }

    private void HandleForestChoice(int index)
    {
        if (index < 0 || index >= _choices.Length)
            return;

        switch (_choices[index])
        {
            case ChoiceFollowStream:
                _actionMessage = "You pick your way downhill toward the sound of running water.";
                _actionMessageTimer = 2.5f;
                AdvanceTime();
                ApplyTravelEnergyCost(EnergyCostTravelShort);
                ApplyEnvironmentOnAction();
                EnterForestArea(Phase.ForestStream);
                break;

            case ChoiceEnterTent:
                EnterTent();
                break;

            case ChoiceDisassembleTent:
                TryDisassembleTrashBagTent();
                break;

            case ChoiceWait:
                PerformIdle();
                break;
        }
    }

    private void HandleForestStreamChoice(int index)
    {
        if (index < 0 || index >= _choices.Length)
            return;

        switch (_choices[index])
        {
            case ChoiceEnterDeepForest:
                _actionMessage = "You push uphill into the older pines and leave the stream behind.";
                _actionMessageTimer = 2.5f;
                AdvanceTime();
                ApplyTravelEnergyCost(EnergyCostTravelShort);
                ApplyEnvironmentOnAction();
                EnterForestArea(Phase.Forest);
                break;

            case ChoiceBackToForestEntry:
                _actionMessage = "You work your way back toward the edge of town.";
                _actionMessageTimer = 2.5f;
                AdvanceTime();
                ApplyTravelEnergyCost(EnergyCostTravelShort);
                ApplyEnvironmentOnAction();
                EnterForestArea(Phase.ForestEntry);
                break;

            case ChoiceEnterTent:
                EnterTent();
                break;

            case ChoiceDisassembleTent:
                TryDisassembleTrashBagTent();
                break;

            case ChoiceWait:
                PerformIdle();
                break;
        }
    }

    private void HandleOutsideChoice(int index)
    {
        if (index < 0 || index >= _choices.Length)
            return;

        switch (_choices[index])
        {
            case ChoiceHideInGarbage:
                EnterDeath("They found you.", "Dragged from the garbage like an animal.");
                return;

            case ChoiceGoIntoTown:
                _actionMessage = "You slip through the gap in the fence and onto the empty street.";
                _actionMessageTimer = 2.0f;
                AdvanceTime();
                ApplyTravelEnergyCost(EnergyCostTravelShort);
                ApplyEnvironmentOnAction();
                EnterPhase(Phase.Town);
                return;

            case ChoiceGoToUnclesHouse:
                EnterDeath("You went to your uncle.", "He called them before you could even sit down.");
                return;

            case ChoiceEnterTent:
                EnterTent();
                return;

            case ChoiceDisassembleTent:
                TryDisassembleTrashBagTent();
                return;

            case ChoiceWait:
                PerformIdle();
                return;
        }

        AdvanceTime();
        ApplyEnvironmentOnAction();
        _actionMessageTimer = ActionMessageDuration;
    }

    private void HandleTownChoice(int index)
    {
        if (index < 0 || index >= _choices.Length)
            return;

        switch (_choices[index])
        {
            case ChoiceIndustrialDistrict:
                _actionMessage = "You cut west toward the warehouses and loading bays.";
                _actionMessageTimer = 2.0f;
                AdvanceTime();
                ApplyTravelEnergyCost(EnergyCostTravelShort);
                ApplyEnvironmentOnAction();
                EnterPhase(Phase.IndustrialDistrict);
                return;

            case ChoiceCommercialDistrict:
                _actionMessage = "You slip east toward the lit shopfronts.";
                _actionMessageTimer = 2.0f;
                AdvanceTime();
                ApplyTravelEnergyCost(EnergyCostTravelShort);
                ApplyEnvironmentOnAction();
                EnterPhase(Phase.CommercialDistrict);
                return;

            case ChoiceConvenienceStore:
                _phaseBeforeStore = Phase.Town;
                ApplyEnvironmentOnAction();
                _actionMessage = "You push through the heavy glass door into the harsh light.";
                _actionMessageTimer = 1.8f;
                ApplyTravelEnergyCost(EnergyCostTravelShort);
                EnterPhase(Phase.Store);
                return;

            case ChoiceBackToCourtyard:
                _actionMessage = "You duck back through the fence into the courtyard behind your block.";
                _actionMessageTimer = 2.0f;
                AdvanceTime();
                ApplyTravelEnergyCost(EnergyCostTravelShort);
                ApplyEnvironmentOnAction();
                EnterPhase(Phase.Outside);
                return;

            case ChoiceEnterTent:
                EnterTent();
                return;

            case ChoiceDisassembleTent:
                TryDisassembleTrashBagTent();
                return;

            case ChoiceWait:
                PerformIdle();
                return;
        }

        AdvanceTime();
        ApplyEnvironmentOnAction();
        _actionMessageTimer = ActionMessageDuration;
    }

    private void HandleIndustrialDistrictChoice(int index)
    {
        if (index < 0 || index >= _choices.Length)
            return;

        switch (_choices[index])
        {
            case ChoiceCafe:
                _phaseBeforeCafe = Phase.IndustrialDistrict;
                ApplyEnvironmentOnAction();
                _actionMessage = "You push through the frosted glass door into the warmth.";
                _actionMessageTimer = 1.8f;
                ApplyTravelEnergyCost(EnergyCostTravelShort);
                EnterPhase(Phase.Cafe);
                return;

            case ChoiceBackToTownCenter:
                _actionMessage = "You leave the warehouses behind and return to the central streets.";
                _actionMessageTimer = 2.0f;
                AdvanceTime();
                ApplyTravelEnergyCost(EnergyCostTravelShort);
                ApplyEnvironmentOnAction();
                EnterPhase(Phase.Town);
                return;

            case ChoiceEnterTent:
                EnterTent();
                return;

            case ChoiceDisassembleTent:
                TryDisassembleTrashBagTent();
                return;

            case ChoiceWait:
                PerformIdle();
                return;
        }

        AdvanceTime();
        ApplyEnvironmentOnAction();
        _actionMessageTimer = ActionMessageDuration;
    }

    private void HandleCommercialDistrictChoice(int index)
    {
        if (index < 0 || index >= _choices.Length)
            return;

        switch (_choices[index])
        {
            case ChoiceHeadForForest:
                ModifyStatFromAction(ref _comfort, ref _actionComfortDelta, -5);
                _actionMessage = "You slip south from the shopfronts and into the dark pines at the edge of town.";
                AdvanceTime();
                ApplyTravelEnergyCost();
                ApplyEnvironmentOnAction();
                EnterPhase(Phase.ForestEntry);
                return;

            case ChoiceBackToTownCenter:
                _actionMessage = "You leave the shopfronts behind and return to the central streets.";
                _actionMessageTimer = 2.0f;
                AdvanceTime();
                ApplyTravelEnergyCost(EnergyCostTravelShort);
                ApplyEnvironmentOnAction();
                EnterPhase(Phase.Town);
                return;

            case ChoiceEnterTent:
                EnterTent();
                return;

            case ChoiceDisassembleTent:
                TryDisassembleTrashBagTent();
                return;

            case ChoiceWait:
                PerformIdle();
                return;
        }

        AdvanceTime();
        ApplyEnvironmentOnAction();
        _actionMessageTimer = ActionMessageDuration;
    }

    private void HandleTentChoice(int index)
    {
        if (index < 0 || index >= _choices.Length)
            return;

        switch (_choices[index])
        {
            case ChoiceExitTent:
                ExitTent();
                break;

            case ChoiceDisassembleTent:
                TryDisassembleTrashBagTent();
                break;

            case ChoiceSleep:
                SleepInTent();
                break;

            case ChoiceWait:
                PerformIdle();
                break;
        }
    }

    private void SleepInTent()
    {
        if (_phase != Phase.Tent)
            return;

        _actionMessage = "You curl up inside the shelter and sleep.";
        _actionMessageTimer = 2.4f;

        // Time passes; the player wakes rested. We apply the energy refill after time-drain.
        AdvanceTime(TentSleepTimeSteps);
        SetStatFromAction(ref _energy, ref _actionEnergyDelta, 100);

        // Sleeping still costs water/food, but the body recovers a bit.
        ModifyStatFromAction(ref _satiation, ref _actionSatiationDelta, TentSleepSatiationCost);
        ModifyStatFromAction(ref _hydration, ref _actionHydrationDelta, TentSleepHydrationCost);
        ModifyStatFromAction(ref _health, ref _actionHealthDelta, TentSleepHealthGain);
    }

    private void HandleStoreChoice(int index)
    {
        if (index < 0 || index >= _choices.Length)
            return;

        switch (_choices[index])
        {
            case ChoiceBrowseShelves:
                OpenStoreBuyMenu();
                return;   // do not advance time or close the store phase yet

            case ChoiceLeaveStore:
                _actionMessage = _phaseBeforeStore == Phase.Outside
                    ? "You push back out into the cold dark yard."
                    : "You push back out onto the empty street.";
                AdvanceTime();
                ApplyTravelEnergyCost();
                EnterPhase(_phaseBeforeStore == Phase.Outside || GamePhase.IsTownDistrict(_phaseBeforeStore)
                    ? _phaseBeforeStore
                    : Phase.Town);
                return;

            case ChoiceWait:
                PerformIdle();
                return;
        }

        AdvanceTime();
        _actionMessageTimer = ActionMessageDuration;
    }

    private void HandleDeliveryTruckChoice(int index)
    {
        if (index < 0 || index >= _choices.Length)
            return;

        switch (_choices[index])
        {
            case ChoiceDriveToWarehouse:
                _actionMessage = "You pull out onto the industrial roads, headlights cutting the rain toward the west yards.";
                _actionMessageTimer = 2.8f;
                AdvanceTime();
                ApplyTravelEnergyCost();
                ApplyEnvironmentOnAction();
                EnterPhase(Phase.WarehouseTruck);
                return;

            case ChoiceWait:
                PerformIdle();
                return;
        }

        AdvanceTime();
        _actionMessageTimer = ActionMessageDuration;
    }

    private void HandleWarehouseTruckChoice(int index)
    {
        if (index < 0 || index >= _choices.Length)
            return;

        switch (_choices[index])
        {
            case ChoiceGetOutOfTruck:
                _actionMessage = "You push the door open and climb down from the cab.";
                _actionMessageTimer = 2.1f;
                AdvanceTime();
                ApplyEnvironmentOnAction();
                EnterPhase(Phase.WarehouseAmbush);
                return;

            case ChoiceWait:
                PerformIdle();
                return;
        }

        AdvanceTime();
        _actionMessageTimer = ActionMessageDuration;
    }

    private void HandleWarehouseAmbushChoice(int index)
    {
        if (index < 0 || index >= _choices.Length)
            return;

        switch (_choices[index])
        {
            case ChoiceFight:
                EnterDeath(
                    "You swung at the nearest bratdva.",
                    "Two against one in the rain. Boris sold you cheap.");
                return;

            case ChoiceWait:
                PerformIdle();
                return;
        }

        AdvanceTime();
        _actionMessageTimer = ActionMessageDuration;
    }

    private bool GloveCompartmentHasRemainingLoot()
    {
        for (int i = 0; i < _gloveBoxLootTaken.Length; i++)
        {
            if (!_gloveBoxLootTaken[i])
                return true;
        }

        return false;
    }

    private void ResetGloveCompartmentLoot() => Array.Clear(_gloveBoxLootTaken);

    private void RefreshGloveBoxVisibleList()
    {
        int count = 0;
        for (int i = 0; i < GloveCompartmentCatalog.EntryCount; i++)
        {
            if (!_gloveBoxLootTaken[i])
                _gloveBoxVisibleCatalogIndices[count++] = i;
        }

        _gloveBoxVisibleCount = count;

        if (_gloveBoxVisibleCount <= 0)
        {
            _gloveBoxHighlightedIndex = 0;
            _gloveBoxDetailIndex = -1;
            return;
        }

        _gloveBoxHighlightedIndex = Math.Clamp(_gloveBoxHighlightedIndex, 0, _gloveBoxVisibleCount - 1);
        if (_gloveBoxDetailIndex >= _gloveBoxVisibleCount)
            _gloveBoxDetailIndex = _gloveBoxHighlightedIndex;
    }

    private int GetGloveBoxCatalogIndexFromVisibleIndex(int visibleIndex) =>
        visibleIndex < 0 || visibleIndex >= _gloveBoxVisibleCount ? -1 : _gloveBoxVisibleCatalogIndices[visibleIndex];

    private void OpenGloveBoxMenu()
    {
        if (_phase != Phase.DeliveryTruck || !GloveCompartmentHasRemainingLoot())
            return;

        _showGloveBoxMenu = true;
        _gloveBoxHighlightedIndex = 0;
        _gloveBoxDetailIndex = -1;
        _gloveBoxFeedback = "";
        _gloveBoxFeedbackTimer = 0f;
        _gloveBoxCloseHovered = false;
        _gloveBoxPickupHovered = false;
        RefreshGloveBoxVisibleList();
    }

    private void CloseGloveBoxMenu()
    {
        _showGloveBoxMenu = false;
        _gloveBoxDetailIndex = -1;
        _gloveBoxFeedback = "";
        _gloveBoxFeedbackTimer = 0f;
        _gloveBoxCloseHovered = false;
        _gloveBoxPickupHovered = false;
    }

    private bool CanTakeGloveBoxItem(int index)
    {
        if (index < 0 || index >= GloveCompartmentCatalog.EntryCount || _gloveBoxLootTaken[index])
            return false;

        var entry = GloveCompartmentCatalog.Entries[index];
        return entry.IsMoney || _backpack.Any(s => string.IsNullOrEmpty(s));
    }

    private void TryTakeGloveBoxItem(int index)
    {
        if (index < 0 || index >= GloveCompartmentCatalog.EntryCount)
            return;

        if (_gloveBoxLootTaken[index])
        {
            _gloveBoxFeedback = "Already taken.";
            _gloveBoxFeedbackTimer = 1.6f;
            return;
        }

        var entry = GloveCompartmentCatalog.Entries[index];

        if (entry.IsMoney)
        {
            _money += entry.MoneyAmount;
            _gloveBoxLootTaken[index] = true;
            ClearActionDeltas();
            MarkActionChanged();
            _gloveBoxFeedback = $"Took {entry.Name} (+{entry.MoneyAmount:N0} ₽)";
            _gloveBoxFeedbackTimer = 1.4f;
            return;
        }

        if (!TryAddToBackpack(entry.Name))
        {
            _gloveBoxFeedback = "Backpack is full.";
            _gloveBoxFeedbackTimer = 1.6f;
            return;
        }

        _gloveBoxLootTaken[index] = true;
        _gloveBoxFeedback = $"Took {entry.Name}";
        _gloveBoxFeedbackTimer = 1.2f;

        RefreshGloveBoxVisibleList();
        if (_gloveBoxVisibleCount <= 0)
            CloseGloveBoxMenu();
    }

    private void HandleCafeChoice(int index)
    {
        if (index < 0 || index >= _choices.Length)
            return;

        switch (_choices[index])
        {
            case ChoiceTalkToOwner:
                OpenCafeOwnerDialog();
                return;

            case ChoiceLeaveCafe:
                CloseCafeOwnerDialog();
                _actionMessage = "You step back out into the cold industrial dark.";
                AdvanceTime();
                ApplyTravelEnergyCost();
                EnterPhase(_phaseBeforeCafe == Phase.IndustrialDistrict
                    ? _phaseBeforeCafe
                    : Phase.IndustrialDistrict);
                return;

            case ChoiceWait:
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
            case Phase.Town:
                _actionMessage = "You flatten yourself against a wall and watch the street. Nothing moves.";
                ApplyEnvironmentOnAction();
                break;
            case Phase.IndustrialDistrict:
                _actionMessage = "You press into the shadow of a loading bay and listen. The yards are still.";
                ApplyEnvironmentOnAction();
                break;
            case Phase.CommercialDistrict:
                _actionMessage = "You linger in an alley between shopfronts. The street stays empty.";
                ApplyEnvironmentOnAction();
                break;
            case Phase.Store:
                _actionMessage = "You linger by the shelves, pretending to read labels.";
                break;
            case Phase.Cafe:
                _actionMessage = "You keep your head down. The owner hasn't stopped watching you.";
                break;
            case Phase.DeliveryTruck:
                _actionMessage = "The engine rumbles under you. The yards are a few minutes away.";
                ApplyEnvironmentOnAction();
                break;
            case Phase.WarehouseTruck:
                _actionMessage = "You sit in the cab and watch the bay through the windshield.";
                ApplyEnvironmentOnAction();
                break;
            case Phase.WarehouseAmbush:
                _actionMessage = "You hold still, listening to the rain and the men breathing in the dark.";
                ApplyEnvironmentOnAction();
                break;
            case Phase.ForestEntry:
                _actionMessage = "You hold still among the young pines. The city is still too close.";
                break;
            case Phase.Forest:
                _actionMessage = "You stay low and motionless. The forest is quiet.";
                break;
            case Phase.ForestStream:
                _actionMessage = "You crouch by the icy water. The stream mutters over the stones.";
                break;
            case Phase.Tent:
                _actionMessage = "You sit still in the cramped shelter, listening to the wind on the plastic.";
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
        _controllerButtonRect = new Rectangle(x, 10f + (size + gap) * 2f, size, size);
    }

    private void ResetDeathLines()
    {
        _deathLine1 = DefaultDeathLine1;
        _deathLine2 = DefaultDeathLine2;
    }

    private void EnterDeath(string line1, string line2)
    {
        _deathLine1 = line1;
        _deathLine2 = line2;
        EnterPhase(Phase.Death);
    }

    private void CloseAllOverlays()
    {
        _showItemDialog = false;
        CloseStoreBuyMenu();
        CloseGloveBoxMenu();
        CloseRegionMap();
        CloseBuildDialog();
        CloseForageDialog();
        CloseCafeOwnerDialog();
        CloseControllerDebug();
        CloseQuitConfirm();
        CloseStatsHelp();
    }

    private bool BlocksActionBarNavigation() =>
        _showRegionMap || _showItemDialog || _showStoreBuyMenu || _showGloveBoxMenu || _showBuildDialog
        || _showForageDialog || _showCafeOwnerDialog || _showControllerDebug || _showQuitConfirm
        || _showStatsHelp;

    private bool AllowsSidebarAndSceneInput() =>
        !_showItemDialog && !_showStoreBuyMenu && !_showGloveBoxMenu && !_showRegionMap
        && !_showBuildDialog && !_showForageDialog && !_showCafeOwnerDialog && !_showQuitConfirm
        && !_showStatsHelp;

    private void RestartGame()
    {
        _actionMessage = "";
        _actionMessageTimer = 0f;
        _selectedIndex = 0;
        CloseAllOverlays();
        _hasTrashBagTent = false;
        _tentBuiltInPhase = null;
        _borisDeliveryJobActive = false;
        ResetGloveCompartmentLoot();
        _buildFeedback = "";
        ResetDeathLines();
        ClearDroppedItems();
        EnterPhase(Phase.Opening);
    }

    /// <summary>
    /// Jump to a reproducible debug snapshot in Boris's delivery truck (glove box loot, drive to warehouse).
    /// Resets stats, money, and backpack for delivery-run testing.
    /// </summary>
    private void DebugStartGame()
    {
        CloseAllOverlays();
        _hasTrashBagTent = false;
        _tentBuiltInPhase = null;
        _buildFeedback = "";
        ResetDeathLines();

        _health = 96;
        _energy = 58;
        _satiation = 69;
        _hydration = 76;
        _comfort = 50;
        _money = 10000;
        _backpack = new string?[] { GameItems.TrashBags, GameItems.DuctTape, "Knife", "Lighter", "Phone", GameItems.EmptyBottle, null, null };
        _backpackItemCharges = new int?[8];
        ClearEnvDeltas();
        ClearActionDeltas();

        _borisDeliveryJobActive = true;
        ResetGloveCompartmentLoot();
        EnterPhase(Phase.DeliveryTruck);
    }

    // --- Inventory & ground items ---
    private void OpenItemDialog(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _backpack.Length) return;
        string? item = _backpack[slotIndex];
        if (string.IsNullOrEmpty(item)) return;

        _dialogItemIndex = slotIndex;
        _dialogDroppedItemIndex = -1;
        _dialogItemName = item;
        _showItemDialog = true;
        ResetItemDialogHover();
    }

    private void OpenDroppedItemDialog(int droppedIndex)
    {
        if (droppedIndex < 0 || droppedIndex >= _droppedItems.Count) return;
        DroppedItem dropped = _droppedItems[droppedIndex];
        if (dropped.Room != _phase || dropped.TurnsRemaining <= 0) return;

        _dialogItemIndex = -1;
        _dialogDroppedItemIndex = droppedIndex;
        _dialogItemName = dropped.Name;
        _showItemDialog = true;
        ResetItemDialogHover();
    }

    private void ResetItemDialogHover()
    {
        _dialogCloseHovered = false;
        _dialogActionHovered = false;
        _dialogSecondaryActionHovered = false;
        _dialogDropHovered = false;
    }

    private void CloseItemDialog()
    {
        _showItemDialog = false;
        _dialogItemIndex = -1;
        _dialogDroppedItemIndex = -1;
        _dialogItemName = "";
        ResetItemDialogHover();
    }

    private bool IsDroppedItemDialog => _dialogDroppedItemIndex >= 0;

    private int GetDialogSlotIndex() =>
        IsDroppedItemDialog ? -1 : _dialogItemIndex;

    private int? GetDialogChargesOverride() =>
        IsDroppedItemDialog ? _droppedItems[_dialogDroppedItemIndex].Charges : null;

    private void ClearDroppedItems() => _droppedItems.Clear();

    private int CountDroppedItemsInRoom(Phase room)
    {
        int count = 0;
        foreach (DroppedItem item in _droppedItems)
        {
            if (item.Room == room && item.TurnsRemaining > 0)
                count++;
        }
        return count;
    }

    private void TickDroppedItemsInCurrentRoom(int steps)
    {
        if (steps <= 0 || _phase == Phase.Death) return;

        foreach (DroppedItem item in _droppedItems)
        {
            if (item.Room == _phase)
                item.TurnsRemaining -= steps;
        }

        int before = CountDroppedItemsInRoom(_phase);
        _droppedItems.RemoveAll(d => d.TurnsRemaining <= 0);
        if (CountDroppedItemsInRoom(_phase) != before)
            RefreshConcealment();
        if (IsDroppedItemDialog)
            ValidateDroppedItemDialog();
    }

    private void ValidateDroppedItemDialog()
    {
        if (_dialogDroppedItemIndex < 0)
            return;
        if (_dialogDroppedItemIndex >= _droppedItems.Count)
        {
            CloseItemDialog();
            return;
        }

        DroppedItem dropped = _droppedItems[_dialogDroppedItemIndex];
        if (dropped.Room != _phase || dropped.TurnsRemaining <= 0)
            CloseItemDialog();
    }

    private void TryDropItemFromBackpack()
    {
        if (IsDroppedItemDialog || _dialogItemIndex < 0 || _dialogItemIndex >= _backpack.Length) return;

        string? item = _backpack[_dialogItemIndex];
        if (string.IsNullOrEmpty(item)) return;

        if (CountDroppedItemsInRoom(_phase) >= MaxDroppedItemsPerRoom)
        {
            _actionMessage = "There is no room to leave anything else here.";
            _actionMessageTimer = ActionMessageDuration;
            return;
        }

        int anchor = CountDroppedItemsInRoom(_phase);
        _droppedItems.Add(new DroppedItem
        {
            Name = item,
            Charges = _backpackItemCharges[_dialogItemIndex],
            Room = _phase,
            TurnsRemaining = DroppedItemLifetimeTurns,
            AnchorIndex = anchor
        });

        _backpack[_dialogItemIndex] = null;
        _backpackItemCharges[_dialogItemIndex] = null;
        CompactBackpack();

        _actionMessage = $"You set down the {item}.";
        _actionMessageTimer = ActionMessageDuration;
        RefreshConcealment();
        CloseItemDialog();
    }

    private void TryPickupDroppedItem()
    {
        if (!IsDroppedItemDialog || _dialogDroppedItemIndex < 0 || _dialogDroppedItemIndex >= _droppedItems.Count)
            return;

        DroppedItem dropped = _droppedItems[_dialogDroppedItemIndex];
        if (dropped.Room != _phase || dropped.TurnsRemaining <= 0)
        {
            CloseItemDialog();
            return;
        }

        if (!TryAddToBackpack(dropped.Name, dropped.Charges))
        {
            _actionMessage = "Backpack is full — make space before picking this up.";
            _actionMessageTimer = ActionMessageDuration;
            return;
        }

        _droppedItems.RemoveAt(_dialogDroppedItemIndex);
        _actionMessage = $"You pick up the {dropped.Name}.";
        _actionMessageTimer = ActionMessageDuration;
        RefreshConcealment();
        CloseItemDialog();
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

    private void OpenForageDialog()
    {
        if (!GamePhase.IsForestSurvival(_phase))
            return;

        _showForageDialog = true;
        _forageHighlightedIndex = 0;
        _forageCloseHovered = false;
        Array.Clear(_forageOptionHovered);
    }

    private void CloseForageDialog()
    {
        _showForageDialog = false;
        _forageCloseHovered = false;
        Array.Clear(_forageOptionHovered);
    }

    private void OpenCafeOwnerDialog()
    {
        if (_phase != Phase.Cafe)
            return;

        _showCafeOwnerDialog = true;
        _cafeOwnerDialogStage = CafeOwnerDialog.Stage.Main;
        _cafeOwnerHighlightedIndex = 0;
        _cafeOwnerSelectedOption = -1;
        _cafeOwnerCloseHovered = false;
        Array.Clear(_cafeOwnerOptionHovered);
    }

    private void CloseCafeOwnerDialog()
    {
        _showCafeOwnerDialog = false;
        _cafeOwnerCloseHovered = false;
        _cafeOwnerSelectedOption = -1;
        _cafeOwnerDialogStage = CafeOwnerDialog.Stage.Main;
        Array.Clear(_cafeOwnerOptionHovered);
    }

    private void SelectCafeOwnerOption(int optionIndex)
    {
        if (!_showCafeOwnerDialog)
            return;

        int count = CafeOwnerDialog.GetOptionCount(_cafeOwnerDialogStage);
        if (optionIndex < 0 || optionIndex >= count)
            return;

        if (_cafeOwnerDialogStage == CafeOwnerDialog.Stage.Main)
        {
            if (optionIndex == CafeOwnerDialog.WorkOptionIndex && !_borisDeliveryJobActive)
            {
                _cafeOwnerDialogStage = CafeOwnerDialog.Stage.DeliveryOffer;
                _cafeOwnerSelectedOption = -1;
                _cafeOwnerHighlightedIndex = 0;
                return;
            }

            _cafeOwnerSelectedOption = optionIndex;
            _cafeOwnerHighlightedIndex = optionIndex;
            return;
        }

        _cafeOwnerSelectedOption = optionIndex;
        _cafeOwnerHighlightedIndex = optionIndex;
        if (optionIndex == 0)
            AcceptBorisDeliveryJob();
        else
            DeclineBorisDeliveryJob();
    }

    private void AcceptBorisDeliveryJob()
    {
        CloseCafeOwnerDialog();
        _borisDeliveryJobActive = true;
        ResetGloveCompartmentLoot();
        _actionMessage = "Boris slides keys across the counter. \"Get in the truck. " +
                         CafeOwnerDialog.WarehouseName + ", bay three. Move.\"";
        _actionMessageTimer = 2.8f;
        AdvanceTime();
        ApplyTravelEnergyCost(EnergyCostTravelShort);
        EnterPhase(Phase.DeliveryTruck);
    }

    private void DeclineBorisDeliveryJob()
    {
        CloseCafeOwnerDialog();
        _actionMessage = "Boris turns back to the samovar. \"Then don't waste my time.\"";
        _actionMessageTimer = ActionMessageDuration;
    }

    private void TryPerformForage(int optionIndex)
    {
        if (!GamePhase.IsForestSurvival(_phase))
            return;
        if (optionIndex < 0 || optionIndex >= ForageOptionCount)
            return;

        CloseForageDialog();
        ClearActionDeltas();

        string item = ForageOptionItems[optionIndex];
        ApplyEnvironmentOnAction();
        AdvanceTime();
        ModifyStatFromAction(ref _energy, ref _actionEnergyDelta, -EnergyCostForage);

        bool stored = TryAddToBackpack(item);
        string target = item.ToLowerInvariant();
        _actionMessage = stored
            ? $"You gather {target} and stow it in your pack."
            : $"You gather {target}, but your pack is full.";
        _actionMessageTimer = ActionMessageDuration;
    }

    private void OpenControllerDebug()
    {
        _showControllerDebug = true;
        _controllerDebugCloseHovered = false;
        _controllerDebugPrevHovered = false;
        _controllerDebugNextHovered = false;
        Array.Clear(_controllerDebugTabHovered);

        _controllerDebugPadIndex = Math.Max(0, InputManager.ActiveGamepad);
    }

    private void CloseControllerDebug()
    {
        _showControllerDebug = false;
        _controllerDebugCloseHovered = false;
        _controllerDebugPrevHovered = false;
        _controllerDebugNextHovered = false;
        Array.Clear(_controllerDebugTabHovered);
    }

    private void CycleControllerDebugPad(int delta)
    {
        _controllerDebugPadIndex = (_controllerDebugPadIndex + delta + GamepadDebugLayout.MaxGamepadsToShow) % GamepadDebugLayout.MaxGamepadsToShow;
    }

    private void OpenQuitConfirm()
    {
        _showQuitConfirm = true;
        _quitConfirmSelectedButton = 0;
        _quitConfirmYesHovered = false;
        _quitConfirmNoHovered = false;
    }

    private void CloseQuitConfirm()
    {
        _showQuitConfirm = false;
        _quitConfirmYesHovered = false;
        _quitConfirmNoHovered = false;
    }

    private void OpenStatsHelp()
    {
        _showStatsHelp = true;
        _statsHelpCloseHovered = false;
    }

    private void CloseStatsHelp()
    {
        _showStatsHelp = false;
        _statsHelpCloseHovered = false;
    }

    private void RefreshOutdoorActionChoices()
    {
        if (_phase == Phase.Outside)
        {
            _choices = _hasTrashBagTent && _tentBuiltInPhase == Phase.Outside
                ? new[]
                {
                    ChoiceHideInGarbage,
                    ChoiceGoIntoTown,
                    ChoiceGoToUnclesHouse,
                    ChoiceEnterTent,
                    ChoiceDisassembleTent,
                    ChoiceWait
                }
                : _hasTrashBagTent
                ? new[]
                {
                    ChoiceHideInGarbage,
                    ChoiceGoIntoTown,
                    ChoiceGoToUnclesHouse,
                    ChoiceEnterTent,
                    ChoiceWait
                }
                : new[]
                {
                    ChoiceHideInGarbage,
                    ChoiceGoIntoTown,
                    ChoiceGoToUnclesHouse,
                    ChoiceWait
                };
        }
        else if (_phase == Phase.Town)
        {
            _choices = _hasTrashBagTent && _tentBuiltInPhase == Phase.Town
                ? new[]
                {
                    ChoiceIndustrialDistrict,
                    ChoiceCommercialDistrict,
                    ChoiceConvenienceStore,
                    ChoiceBackToCourtyard,
                    ChoiceEnterTent,
                    ChoiceDisassembleTent,
                    ChoiceWait
                }
                : _hasTrashBagTent
                ? new[]
                {
                    ChoiceIndustrialDistrict,
                    ChoiceCommercialDistrict,
                    ChoiceConvenienceStore,
                    ChoiceBackToCourtyard,
                    ChoiceEnterTent,
                    ChoiceWait
                }
                : new[]
                {
                    ChoiceIndustrialDistrict,
                    ChoiceCommercialDistrict,
                    ChoiceConvenienceStore,
                    ChoiceBackToCourtyard,
                    ChoiceWait
                };
        }
        else if (_phase == Phase.IndustrialDistrict)
        {
            _choices = _hasTrashBagTent && _tentBuiltInPhase == Phase.IndustrialDistrict
                ? new[] { ChoiceCafe, ChoiceBackToTownCenter, ChoiceEnterTent, ChoiceDisassembleTent, ChoiceWait }
                : _hasTrashBagTent
                ? new[] { ChoiceCafe, ChoiceBackToTownCenter, ChoiceEnterTent, ChoiceWait }
                : new[] { ChoiceCafe, ChoiceBackToTownCenter, ChoiceWait };
        }
        else if (_phase == Phase.CommercialDistrict)
        {
            _choices = _hasTrashBagTent && _tentBuiltInPhase == Phase.CommercialDistrict
                ? new[]
                {
                    ChoiceHeadForForest,
                    ChoiceBackToTownCenter,
                    ChoiceEnterTent,
                    ChoiceDisassembleTent,
                    ChoiceWait
                }
                : _hasTrashBagTent
                ? new[]
                {
                    ChoiceHeadForForest,
                    ChoiceBackToTownCenter,
                    ChoiceEnterTent,
                    ChoiceWait
                }
                : new[]
                {
                    ChoiceHeadForForest,
                    ChoiceBackToTownCenter,
                    ChoiceWait
                };
        }
        else if (_phase == Phase.ForestEntry)
        {
            _choices = _hasTrashBagTent && _tentBuiltInPhase == Phase.ForestEntry
                ? new[] { ChoiceFollowStream, ChoiceGoBackToTown, ChoiceEnterTent, ChoiceDisassembleTent, ChoiceWait }
                : _hasTrashBagTent
                ? new[] { ChoiceFollowStream, ChoiceGoBackToTown, ChoiceEnterTent, ChoiceWait }
                : new[] { ChoiceFollowStream, ChoiceGoBackToTown, ChoiceWait };
        }
        else if (_phase == Phase.Forest)
        {
            _choices = _hasTrashBagTent && _tentBuiltInPhase == Phase.Forest
                ? new[] { ChoiceFollowStream, ChoiceEnterTent, ChoiceDisassembleTent, ChoiceWait }
                : _hasTrashBagTent
                ? new[] { ChoiceFollowStream, ChoiceEnterTent, ChoiceWait }
                : new[] { ChoiceFollowStream, ChoiceWait };
        }
        else if (_phase == Phase.ForestStream)
        {
            _choices = _hasTrashBagTent && _tentBuiltInPhase == Phase.ForestStream
                ? new[] { ChoiceEnterDeepForest, ChoiceBackToForestEntry, ChoiceEnterTent, ChoiceDisassembleTent, ChoiceWait }
                : _hasTrashBagTent
                ? new[] { ChoiceEnterDeepForest, ChoiceBackToForestEntry, ChoiceEnterTent, ChoiceWait }
                : new[] { ChoiceEnterDeepForest, ChoiceBackToForestEntry, ChoiceWait };
        }
        else
        {
            return;
        }

        if (_selectedIndex >= _choices.Length)
            _selectedIndex = Math.Max(0, _choices.Length - 1);
    }

    private bool CanDrinkFromDialogSlot(int slotIndex) =>
        slotIndex >= 0 &&
        slotIndex < _backpack.Length &&
        string.Equals(_backpack[slotIndex], GameItems.BottledWater, StringComparison.OrdinalIgnoreCase) &&
        GetBackpackSlotCharges(slotIndex, GameItems.BottledWater) > 0;

    private bool CanFillBottleAtStream(int slotIndex)
    {
        if (_phase != Phase.ForestStream || slotIndex < 0 || slotIndex >= _backpack.Length)
            return false;

        string? item = _backpack[slotIndex];
        if (string.Equals(item, GameItems.EmptyBottle, StringComparison.OrdinalIgnoreCase))
            return true;

        return string.Equals(item, GameItems.BottledWater, StringComparison.OrdinalIgnoreCase) &&
               GetBackpackSlotCharges(slotIndex, GameItems.BottledWater) < GameItems.BottledWaterMaxSips;
    }

    private void PerformFillBottleFromStream()
    {
        if (!CanFillBottleAtStream(_dialogItemIndex))
        {
            _actionMessage = "You need a bottle that is not already full, and you must be at the stream.";
            _actionMessageTimer = ActionMessageDuration;
            return;
        }

        int slot = _dialogItemIndex;
        bool wasEmpty = string.Equals(_backpack[slot], GameItems.EmptyBottle, StringComparison.OrdinalIgnoreCase);

        ClearActionDeltas();
        AdvanceTime();
        ApplyEnvironmentOnAction();
        ModifyStatFromAction(ref _energy, ref _actionEnergyDelta, -EnergyCostFillBottle);

        _backpack[slot] = GameItems.BottledWater;
        _backpackItemCharges[slot] = GameItems.BottledWaterMaxSips;

        _actionMessage = wasEmpty
            ? "You kneel in the icy water and fill the bottle. It is painfully cold, but drinkable."
            : "You kneel in the stream and top off the bottle until it is full.";
        _actionMessageTimer = ActionMessageDuration;
        CloseItemDialog();
    }

    private void PerformHunt()
    {
        if (!GamePhase.IsForestSurvival(_phase))
            return;

        ApplyEnvironmentOnAction();
        AdvanceTime();
        ModifyStatFromAction(ref _energy, ref _actionEnergyDelta, -EnergyCostHunt);

        // Weighted outcomes: raccoon most likely, rabbit next, otherwise nothing.
        double roll = _rng.NextDouble();
        string? catchItem = roll < 0.55 ? GameItems.Raccoon : roll < 0.80 ? GameItems.Rabbit : null;

        if (catchItem is null)
        {
            _actionMessage = "You stalk through the brush for an hour, but come up empty-handed.";
            _actionMessageTimer = ActionMessageDuration;
            return;
        }

        bool stored = TryAddToBackpack(catchItem);
        if (string.Equals(catchItem, GameItems.Raccoon, StringComparison.OrdinalIgnoreCase))
        {
            ModifyStatFromAction(ref _satiation, ref _actionSatiationDelta, 12);
            _actionMessage = stored
                ? "You catch a raccoon. You eat what you can, then stash the rest."
                : "You catch a raccoon. You eat what you can, but your pack is full — you leave the rest.";
        }
        else
        {
            ModifyStatFromAction(ref _satiation, ref _actionSatiationDelta, 8);
            _actionMessage = stored
                ? "You catch a rabbit. You eat what you can, then stash the rest."
                : "You catch a rabbit. You eat what you can, but your pack is full — you leave the rest.";
        }

        _actionMessageTimer = ActionMessageDuration;
    }

    private void ShowNotImplementedAction(string actionDescription)
    {
        _actionMessage = $"{actionDescription} is not implemented yet.";
        _actionMessageTimer = ActionMessageDuration;
    }

    private void EnterTent()
    {
        if (!_hasTrashBagTent || !GamePhase.IsOutdoorsSurvival(_phase))
            return;

        _phaseOutdoorBeforeTent = _phase;
        _actionMessage = "You crawl through the flap into the cramped shelter.";
        _actionMessageTimer = 2.2f;
        ApplyTravelEnergyCost(EnergyCostTravelShort);
        EnterPhase(Phase.Tent);
    }

    private void ExitTent()
    {
        if (_phase != Phase.Tent)
            return;

        _actionMessage = "You push back out into the cold air.";
        _actionMessageTimer = 2f;
        ApplyTravelEnergyCost(EnergyCostTravelShort);
        EnterPhase(_phaseOutdoorBeforeTent);
    }

    private bool HasBackpackItem(string itemName) =>
        FindBackpackSlotIndex(itemName) >= 0;

    private int FindBackpackSlotIndex(string itemName)
    {
        for (int i = 0; i < _backpack.Length; i++)
        {
            if (string.Equals(_backpack[i], itemName, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private bool HasUsableBackpackItem(string itemName)
    {
        int slot = FindBackpackSlotIndex(itemName);
        if (slot < 0)
            return false;
        int max = GameItems.GetMaxCharges(itemName);
        return max <= 0 || GetBackpackSlotCharges(slot, itemName) > 0;
    }

    private void CompactBackpack()
    {
        int write = 0;
        for (int read = 0; read < _backpack.Length; read++)
        {
            if (!string.IsNullOrEmpty(_backpack[read]))
            {
                if (write != read)
                {
                    _backpack[write] = _backpack[read];
                    _backpackItemCharges[write] = _backpackItemCharges[read];
                }
                write++;
            }
        }

        for (int i = write; i < _backpack.Length; i++)
        {
            _backpack[i] = null;
            _backpackItemCharges[i] = null;
        }
    }

    /// <summary>Uses one charge of a partial-use item, or removes a non-charge item entirely.</summary>
    private bool TryUseBackpackItemCharge(string itemName)
    {
        int slot = FindBackpackSlotIndex(itemName);
        if (slot < 0)
            return false;

        int max = GameItems.GetMaxCharges(itemName);
        if (max <= 0)
        {
            _backpack[slot] = null;
            _backpackItemCharges[slot] = null;
            CompactBackpack();
            return true;
        }

        int remaining = GetBackpackSlotCharges(slot, itemName) - 1;
        if (remaining <= 0)
        {
            _backpack[slot] = null;
            _backpackItemCharges[slot] = null;
            CompactBackpack();
        }
        else
        {
            _backpackItemCharges[slot] = remaining;
        }

        return true;
    }

    private void RemoveBackpackItemAtSlot(int slot)
    {
        if (slot < 0 || slot >= _backpack.Length)
            return;

        _backpack[slot] = null;
        _backpackItemCharges[slot] = null;
    }

    private bool CanCraftMolotov(out string reason)
    {
        int vodkaSlot = FindBackpackSlotIndex(GameItems.Vodka);
        if (vodkaSlot < 0)
        {
            reason = $"Need {GameItems.Vodka}.";
            return false;
        }

        int ragSlot = FindBackpackSlotIndex(GameItems.Rag);
        if (ragSlot < 0)
        {
            reason = $"Need {GameItems.Rag}.";
            return false;
        }

        reason = "";
        return true;
    }

    private bool CanCraftLitMolotov(out string reason)
    {
        int molotovSlot = FindBackpackSlotIndex(GameItems.Molotov);
        if (molotovSlot < 0)
        {
            reason = $"Need {GameItems.Molotov}.";
            return false;
        }

        int lighterSlot = FindBackpackSlotIndex("Lighter");
        if (lighterSlot < 0)
        {
            reason = "Need Lighter.";
            return false;
        }

        reason = "";
        return true;
    }

    private void TryCraftLitMolotov()
    {
        if (!CanCraftLitMolotov(out string reason))
        {
            _buildFeedback = reason;
            _buildFeedbackTimer = BuildFeedbackDuration;
            return;
        }

        int molotovSlot = FindBackpackSlotIndex(GameItems.Molotov);
        if (molotovSlot < 0)
            return;

        RemoveBackpackItemAtSlot(molotovSlot);
        CompactBackpack();
        TryAddToBackpack(GameItems.LitMolotov);

        _buildFeedback = $"Crafted {CraftLitMolotov}.";
        _buildFeedbackTimer = BuildFeedbackDuration;
    }

    private void TryCraftMolotov()
    {
        if (!CanCraftMolotov(out string reason))
        {
            _buildFeedback = reason;
            _buildFeedbackTimer = BuildFeedbackDuration;
            return;
        }

        int vodkaSlot = FindBackpackSlotIndex(GameItems.Vodka);
        int ragSlot = FindBackpackSlotIndex(GameItems.Rag);
        if (vodkaSlot < 0 || ragSlot < 0)
            return;

        RemoveBackpackItemAtSlot(vodkaSlot);
        if (ragSlot == vodkaSlot)
            ragSlot = FindBackpackSlotIndex(GameItems.Rag);
        RemoveBackpackItemAtSlot(ragSlot);

        CompactBackpack();
        TryAddToBackpack(GameItems.Molotov);

        _buildFeedback = $"Crafted {CraftMolotov}.";
        _buildFeedbackTimer = BuildFeedbackDuration;
    }

    private bool CanBuildTrashBagTent(out string reason)
    {
        if (_hasTrashBagTent)
        {
            reason = "Already built.";
            return false;
        }

        if (!GamePhase.IsOutdoorsSurvival(_phase))
        {
            reason = "Must be outdoors.";
            return false;
        }

        if (!HasUsableBackpackItem(GameItems.TrashBags))
        {
            reason = "Need trash bags.";
            return false;
        }

        if (!HasUsableBackpackItem(GameItems.DuctTape))
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

        if (!TryUseBackpackItemCharge(GameItems.TrashBags) || !TryUseBackpackItemCharge(GameItems.DuctTape))
            return;

        _hasTrashBagTent = true;
        _tentBuiltInPhase = _phase;
        RefreshOutdoorComfortEnvironment();
        RefreshOutdoorActionChoices();
        RefreshConcealment();

        int bagsSlot = FindBackpackSlotIndex(GameItems.TrashBags);
        int tapeSlot = FindBackpackSlotIndex(GameItems.DuctTape);
        bool materialsRemain = (bagsSlot >= 0 && GetBackpackSlotCharges(bagsSlot, GameItems.TrashBags) > 0) ||
                               (tapeSlot >= 0 && GetBackpackSlotCharges(tapeSlot, GameItems.DuctTape) > 0);

        _buildFeedback = materialsRemain
            ? "Shelter pitched — bags and tape only partly used."
            : "You rig a crude shelter from plastic and tape.";
        _buildFeedbackTimer = BuildFeedbackDuration;
        _actionMessage = "Trash bag tent pitched. A little warmer out here.";
        _actionMessageTimer = ActionMessageDuration;
    }

    private int CountEmptyBackpackSlots() =>
        _backpack.Count(s => string.IsNullOrEmpty(s));

    private int SlotsNeededToRestoreMaterial(string itemName) =>
        FindBackpackSlotIndex(itemName) >= 0 ? 0 : 1;

    private int SlotsNeededForTentDisassembly() =>
        SlotsNeededToRestoreMaterial(GameItems.TrashBags)
        + SlotsNeededToRestoreMaterial(GameItems.DuctTape)
        + CountDroppedItemsInRoom(Phase.Tent);

    private bool CanDisassembleTrashBagTent(out string reason)
    {
        if (!_hasTrashBagTent)
        {
            reason = "No tent pitched here.";
            return false;
        }

        if (_phase != Phase.Tent && _phase != _tentBuiltInPhase)
        {
            reason = "Your shelter is pitched somewhere else.";
            return false;
        }

        int needed = SlotsNeededForTentDisassembly();
        int empty = CountEmptyBackpackSlots();
        if (empty < needed)
        {
            reason = needed == 1
                ? "Backpack is full — make space before taking down the tent."
                : $"You need {needed} free backpack slots for the shelter, materials, and items inside.";
            return false;
        }

        reason = "";
        return true;
    }

    private bool TryRestoreBackpackMaterial(string itemName)
    {
        int max = GameItems.GetMaxCharges(itemName);
        int slot = FindBackpackSlotIndex(itemName);
        if (slot >= 0)
        {
            if (max > 0)
            {
                int current = GetBackpackSlotCharges(slot, itemName);
                _backpackItemCharges[slot] = Math.Min(max, current + 1);
            }
            return true;
        }

        return TryAddToBackpack(itemName, max > 0 ? 1 : null);
    }

    private bool TryDisassembleTrashBagTent()
    {
        if (!CanDisassembleTrashBagTent(out string reason))
        {
            _actionMessage = reason;
            _actionMessageTimer = ActionMessageDuration;
            _buildFeedback = reason;
            _buildFeedbackTimer = BuildFeedbackDuration;
            return false;
        }

        var tentDropped = _droppedItems
            .Where(d => d.Room == Phase.Tent && d.TurnsRemaining > 0)
            .ToList();

        foreach (DroppedItem dropped in tentDropped)
        {
            if (!TryAddToBackpack(dropped.Name, dropped.Charges))
            {
                _actionMessage = "Backpack is full — make space before taking down the tent.";
                _actionMessageTimer = ActionMessageDuration;
                return false;
            }
        }

        _droppedItems.RemoveAll(d => d.Room == Phase.Tent);

        if (!TryRestoreBackpackMaterial(GameItems.TrashBags) || !TryRestoreBackpackMaterial(GameItems.DuctTape))
        {
            _actionMessage = "Backpack is full — make space before taking down the tent.";
            _actionMessageTimer = ActionMessageDuration;
            return false;
        }

        bool wasInside = _phase == Phase.Tent;
        Phase outdoorPhase = wasInside ? _phaseOutdoorBeforeTent : _phase;

        _hasTrashBagTent = false;
        _tentBuiltInPhase = null;

        if (wasInside)
            EnterPhase(outdoorPhase);
        else
        {
            RefreshOutdoorComfortEnvironment();
            RefreshOutdoorActionChoices();
            RefreshConcealment();
        }

        _buildFeedback = "";
        _buildFeedbackTimer = 0f;
        _actionMessage = tentDropped.Count > 0
            ? "You take down the shelter and pack up what was left inside."
            : "You fold up the trash-bag shelter and stow the bags and tape.";
        _actionMessageTimer = ActionMessageDuration;

        if (_selectedIndex >= _choices.Length)
            _selectedIndex = Math.Max(0, _choices.Length - 1);

        return true;
    }

    private enum DialogItemAction
    {
        None,
        DrinkWater,
        EatSoup,
        FillBottle
    }

    private DialogItemAction GetDialogItemAction(string itemName, int slotIndex)
    {
        if (string.Equals(itemName, GameItems.BottledWater, StringComparison.OrdinalIgnoreCase) &&
            GetBackpackSlotCharges(slotIndex, GameItems.BottledWater) > 0)
            return DialogItemAction.DrinkWater;

        if (string.Equals(itemName, GameItems.CannedSoup, StringComparison.OrdinalIgnoreCase) &&
            GetBackpackSlotCharges(slotIndex, GameItems.CannedSoup) > 0)
            return DialogItemAction.EatSoup;

        if (string.Equals(itemName, GameItems.EmptyBottle, StringComparison.OrdinalIgnoreCase) &&
            _phase == Phase.ForestStream)
            return DialogItemAction.FillBottle;

        return DialogItemAction.None;
    }

    private bool CanPerformDialogItemAction(string itemName, int slotIndex) =>
        CanDrinkFromDialogSlot(slotIndex) || CanFillBottleAtStream(slotIndex);

    private static string GetDialogItemActionLabel(DialogItemAction action) =>
        action switch
        {
            DialogItemAction.DrinkWater => "DRINK",
            DialogItemAction.EatSoup => "EAT",
            DialogItemAction.FillBottle => "FILL",
            _ => ""
        };

    private void TryPerformDialogItemAction(DialogItemAction action)
    {
        if (_dialogItemIndex < 0 || _dialogItemIndex >= _backpack.Length) return;
        switch (action)
        {
            case DialogItemAction.DrinkWater:
                TryDrinkBottledWater();
                return;
            case DialogItemAction.EatSoup:
                TryEatCannedSoup();
                return;
            case DialogItemAction.FillBottle:
                PerformFillBottleFromStream();
                return;
            default:
                return;
        }
    }

    private void TryDrinkBottledWater()
    {
        if (_dialogItemIndex < 0 || _dialogItemIndex >= _backpack.Length) return;
        if (!CanDrinkFromDialogSlot(_dialogItemIndex)) return;

        int remaining = GetBackpackSlotCharges(_dialogItemIndex, GameItems.BottledWater) - 1;
        ClearActionDeltas();
        ModifyStatFromAction(ref _hydration, ref _actionHydrationDelta, GameItems.BottledWaterHydrationPerSip);

        if (remaining <= 0)
        {
            _backpack[_dialogItemIndex] = GameItems.EmptyBottle;
            _backpackItemCharges[_dialogItemIndex] = null;
            _actionMessage = "You finish the last of the water. The bottle is empty.";
            _actionMessageTimer = ActionMessageDuration;
            CloseItemDialog();
            return;
        }

        _backpackItemCharges[_dialogItemIndex] = remaining;
        _actionMessage = remaining == 1
            ? "You take a drink. One sip left in the bottle."
            : $"You take a drink. {remaining} sips left in the bottle.";
        _actionMessageTimer = ActionMessageDuration;
    }

    private void TryEatCannedSoup()
    {
        if (_dialogItemIndex < 0 || _dialogItemIndex >= _backpack.Length) return;
        string itemName = _backpack[_dialogItemIndex] ?? "";
        if (GetDialogItemAction(itemName, _dialogItemIndex) != DialogItemAction.EatSoup) return;

        int remaining = GetBackpackSlotCharges(_dialogItemIndex, itemName) - 1;
        ClearActionDeltas();
        ModifyStatFromAction(ref _satiation, ref _actionSatiationDelta, GameItems.CannedSoupSatiationPerServing);
        ModifyStatFromAction(ref _hydration, ref _actionHydrationDelta, GameItems.CannedSoupHydrationPerServing);
        ModifyStatFromAction(ref _health, ref _actionHealthDelta, GameItems.CannedSoupHealthPerServing);

        if (remaining <= 0)
        {
            _backpack[_dialogItemIndex] = GameItems.EmptyCan;
            _backpackItemCharges[_dialogItemIndex] = null;
            _actionMessage = "You scrape the last of the soup. The can is empty.";
            _actionMessageTimer = ActionMessageDuration;
            CloseItemDialog();
            return;
        }

        _backpackItemCharges[_dialogItemIndex] = remaining;
        _actionMessage = remaining == 1
            ? "You eat a serving. One serving left in the can."
            : $"You eat a serving. {remaining} servings left in the can.";
        _actionMessageTimer = ActionMessageDuration;
    }

    private bool TryAddToBackpack(string item, int? charges = null)
    {
        for (int i = 0; i < _backpack.Length; i++)
        {
            if (string.IsNullOrEmpty(_backpack[i]))
            {
                _backpack[i] = item;
                _backpackItemCharges[i] = charges;
                return true;
            }
        }
        return false; // backpack full
    }

    private void OpenStoreBuyMenu()
    {
        _showStoreBuyMenu = true;
        _storeBuyHighlightedIndex = 0;
        _storeBuyDetailIndex = -1;
        _storeBuyFeedback = "";
        _storeBuyFeedbackTimer = 0f;
        _storeBuyCloseHovered = false;
        _storeBuyPurchaseHovered = false;
    }

    private void CloseStoreBuyMenu()
    {
        _showStoreBuyMenu = false;
        _storeBuyDetailIndex = -1;
        _storeBuyFeedback = "";
        _storeBuyFeedbackTimer = 0f;
        _storeBuyCloseHovered = false;
        _storeBuyPurchaseHovered = false;
    }

    private bool CanBuyStoreItem(int index)
    {
        if (index < 0 || index >= StoreCatalog.Entries.Length)
            return false;

        var (_, price, _, _, _) = StoreCatalog.Entries[index];
        return _money >= price && _backpack.Any(s => string.IsNullOrEmpty(s));
    }

    private void TryBuyStoreItem(int index)
    {
        if (index < 0 || index >= StoreCatalog.Entries.Length) return;

        var (name, price, satiationDelta, hydrationDelta, healthDelta) = StoreCatalog.Entries[index];

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

    private void DrawRestartButton() =>
        GameDialogUi.DrawToolbarIconButton(_restartButtonRect, _restartHovered, GameToolbarIcons.DrawRestart);

    private void DrawDebugStartButton() =>
        GameDialogUi.DrawToolbarTextButton(_debugStartButtonRect, _debugStartHovered, _uiFont, "DBG", 10f);

    private void DrawControllerButton() =>
        GameDialogUi.DrawToolbarIconButton(
            _controllerButtonRect,
            _showControllerDebug || _controllerHovered,
            GameToolbarIcons.DrawController);

    // =====================================================================
    // CONTROLLER DEBUG — live gamepad buttons, axes, and sticks
    // =====================================================================
    private void DrawControllerDebugScreen()
    {
        ControllerDebugScreenDrawing.ScreenLayout layout = ControllerDebugScreenDrawing.DrawScreen(
            _screenWidth,
            _screenHeight,
            _uiFont,
            _controllerDebugPadIndex,
            _controllerDebugPrevHovered,
            _controllerDebugNextHovered,
            _controllerDebugCloseHovered,
            _controllerDebugTabHovered);

        _controllerDebugPrevRect = layout.PrevRect;
        _controllerDebugNextRect = layout.NextRect;
        _controllerDebugCloseRect = layout.CloseRect;
        for (int i = 0; i < layout.TabRects.Length; i++)
            _controllerDebugTabRects[i] = layout.TabRects[i];
    }

    private void DrawTopRightButtons()
    {
        DrawRestartButton();
        DrawDebugStartButton();
        DrawControllerButton();
    }

    // =====================================================================
    // ITEM DIALOG (modal) — use / examine / close per item
    // =====================================================================
    private string GetItemDialogBody(bool isGround, DialogItemAction eatAction, bool canDrink, bool canFill)
    {
        if (isGround)
        {
            int turns = _droppedItems[_dialogDroppedItemIndex].TurnsRemaining;
            string groundLine = turns == 1
                ? "On the ground here. About one turn left before you lose track of it."
                : $"On the ground here. About {turns} turns left before you lose track of it.";
            if (GameItems.IsBuildingMaterial(_dialogItemName))
                return groundLine + "\n\n" + StoreCatalog.GetFlavorText(_dialogItemName);
            return groundLine;
        }

        if (GameItems.IsBuildingMaterial(_dialogItemName))
            return StoreCatalog.GetFlavorText(_dialogItemName);

        int slot = _dialogItemIndex;
        return eatAction switch
        {
            DialogItemAction.EatSoup => GetCannedSoupDialogText(slot),
            _ when canDrink => GetBottledWaterDialogText(slot),
            _ when canFill && string.Equals(_dialogItemName, GameItems.EmptyBottle, StringComparison.OrdinalIgnoreCase) =>
                "An empty plastic bottle. The stream is right here — you could fill it.",
            _ => string.Equals(_dialogItemName, GameItems.EmptyBottle, StringComparison.OrdinalIgnoreCase)
                ? "An empty plastic bottle. Nothing left to drink."
                : string.Equals(_dialogItemName, GameItems.EmptyCan, StringComparison.OrdinalIgnoreCase)
                    ? "An empty can. Nothing left to eat."
                    : "Set it down here to lighten your pack, or keep carrying it."
        };
    }

    private void DrawItemDialog()
    {
        int screenW = _screenWidth;
        int screenH = _screenHeight;

        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 170));

        bool isGround = IsDroppedItemDialog;
        DialogItemAction eatAction = isGround
            ? DialogItemAction.None
            : GetDialogItemAction(_dialogItemName, _dialogItemIndex);
        bool canDrink = !isGround && CanDrinkFromDialogSlot(_dialogItemIndex);
        bool canFill = !isGround && CanFillBottleAtStream(_dialogItemIndex);
        bool canAct = canDrink || canFill || eatAction == DialogItemAction.EatSoup;

        int panelW = isGround ? 380 : canDrink && canFill ? 440 : 400;
        Font font = _uiFont;

        const float bodySpacing = 0.6f;
        int bodySize = 16;
        int bodyLineHeight = 22;
        int textMaxW = panelW - 48;
        string body = GetItemDialogBody(isGround, eatAction, canDrink, canFill);
        var (bodyLines, bodyHeight) = GameTextLayout.WrapForBox(body, font, bodySize, bodySpacing, textMaxW, bodyLineHeight);
        bool showBuildingTag = GameItems.IsBuildingMaterial(_dialogItemName);
        string? buildingTag = showBuildingTag
            ? StoreCatalog.FormatEffects(_dialogItemName, 0, 0, 0)
            : null;
        int tagHeight = buildingTag != null ? 20 : 0;
        int tagGap = buildingTag != null ? 4 : 0;

        int panelH = Math.Max(240, 124 + bodyHeight + tagGap + tagHeight + 60);
        int panelX = (screenW - panelW) / 2;
        int panelY = (screenH - panelH) / 2 - 20;

        _dialogPanelRect = new Rectangle(panelX, panelY, panelW, panelH);

        Raylib.DrawRectangle(panelX, panelY, panelW, panelH, Palette.CardBg);
        Raylib.DrawRectangleLines(panelX, panelY, panelW, panelH, Palette.CardBorder);

        const int iconSize = 56;
        int iconX = panelX + (panelW - iconSize) / 2;
        int iconY = panelY + 16;
        Raylib.DrawRectangle(iconX - 2, iconY - 2, iconSize + 4, iconSize + 4, new Color(22, 20, 17, 255));
        Raylib.DrawRectangleLines(iconX - 2, iconY - 2, iconSize + 4, iconSize + 4, Palette.SubtleBorder);
        DrawItemIcon(_dialogItemName, new Rectangle(iconX, iconY, iconSize, iconSize), Color.WHITE,
            GetDialogSlotIndex(), GetDialogChargesOverride());

        string title = _dialogItemName.ToUpperInvariant();
        int titleSize = 28;
        int titleW = (int)Raylib.MeasureTextEx(font, title, titleSize, 0.8f).X;
        Raylib.DrawTextEx(font, title,
            new Vector2(panelX + (panelW - titleW) / 2, panelY + 82),
            titleSize, 0.8f, Palette.TextPrimary);

        Raylib.DrawLine(panelX + 40, panelY + 112, panelX + panelW - 40, panelY + 112, Palette.SubtleBorder);

        int textY = panelY + 124;
        foreach (string line in bodyLines)
        {
            int lineW = (int)Raylib.MeasureTextEx(font, line, bodySize, bodySpacing).X;
            Raylib.DrawTextEx(font, line,
                new Vector2(panelX + (panelW - lineW) / 2, textY),
                bodySize, bodySpacing, Palette.TextSecondary);
            textY += string.IsNullOrEmpty(line) ? bodyLineHeight / 2 : bodyLineHeight;
        }

        if (buildingTag != null)
        {
            textY += tagGap;
            int tagW = (int)Raylib.MeasureTextEx(font, buildingTag, 15, 0.5f).X;
            Raylib.DrawTextEx(font, buildingTag,
                new Vector2(panelX + (panelW - tagW) / 2, textY),
                15, 0.5f, Palette.TextDim);
        }

        int btnH = 36;
        int btnY = panelY + panelH - 52;
        int gap = 8;
        _dialogSecondaryActionRect = new Rectangle(0, 0, 0, 0);
        _dialogDropRect = new Rectangle(0, 0, 0, 0);

        if (isGround)
        {
            int btnW = 120;
            int totalW = btnW * 2 + gap;
            int startX = panelX + (panelW - totalW) / 2;
            _dialogActionRect = new Rectangle(startX, btnY, btnW, btnH);
            _dialogCloseRect = new Rectangle(startX + btnW + gap, btnY, btnW, btnH);
            GameDialogUi.DrawDialogButton(_dialogActionRect, "PICK UP", _dialogActionHovered, font);
            GameDialogUi.DrawDialogButton(_dialogCloseRect, "CLOSE", _dialogCloseHovered, font);
        }
        else if (canDrink && canFill)
        {
            int btnW = 78;
            int totalW = btnW * 4 + gap * 3;
            int startX = panelX + (panelW - totalW) / 2;
            _dialogActionRect = new Rectangle(startX, btnY, btnW, btnH);
            _dialogSecondaryActionRect = new Rectangle(startX + (btnW + gap), btnY, btnW, btnH);
            _dialogDropRect = new Rectangle(startX + (btnW + gap) * 2, btnY, btnW, btnH);
            _dialogCloseRect = new Rectangle(startX + (btnW + gap) * 3, btnY, btnW, btnH);
            GameDialogUi.DrawDialogButton(_dialogActionRect, "DRINK", _dialogActionHovered, font);
            GameDialogUi.DrawDialogButton(_dialogSecondaryActionRect, "FILL", _dialogSecondaryActionHovered, font);
            GameDialogUi.DrawDialogButton(_dialogDropRect, "DROP", _dialogDropHovered, font);
            GameDialogUi.DrawDialogButton(_dialogCloseRect, "CLOSE", _dialogCloseHovered, font);
        }
        else if (canAct)
        {
            int btnW = 88;
            int totalW = btnW * 3 + gap * 2;
            int startX = panelX + (panelW - totalW) / 2;
            _dialogActionRect = new Rectangle(startX, btnY, btnW, btnH);
            _dialogDropRect = new Rectangle(startX + btnW + gap, btnY, btnW, btnH);
            _dialogCloseRect = new Rectangle(startX + (btnW + gap) * 2, btnY, btnW, btnH);
            string actionLabel = canDrink
                ? "DRINK"
                : canFill
                    ? "FILL"
                    : GetDialogItemActionLabel(eatAction);
            GameDialogUi.DrawDialogButton(_dialogActionRect, actionLabel, _dialogActionHovered, font);
            GameDialogUi.DrawDialogButton(_dialogDropRect, "DROP", _dialogDropHovered, font);
            GameDialogUi.DrawDialogButton(_dialogCloseRect, "CLOSE", _dialogCloseHovered, font);
        }
        else
        {
            int btnW = 100;
            int totalW = btnW * 2 + gap;
            int startX = panelX + (panelW - totalW) / 2;
            _dialogActionRect = new Rectangle(0, 0, 0, 0);
            _dialogDropRect = new Rectangle(startX, btnY, btnW, btnH);
            _dialogCloseRect = new Rectangle(startX + btnW + gap, btnY, btnW, btnH);
            GameDialogUi.DrawDialogButton(_dialogDropRect, "DROP", _dialogDropHovered, font);
            GameDialogUi.DrawDialogButton(_dialogCloseRect, "CLOSE", _dialogCloseHovered, font);
        }
    }

    // =====================================================================
    // STATS HELP (modal) — explains sidebar status values
    // =====================================================================
    private void DrawStatsHelpDialog()
    {
        GameDialogUi.DrawModalBackdrop(_screenWidth, _screenHeight);

        Font font = _uiFont;
        int panelW = 500;
        int panelH = 600;
        int panelX = (_screenWidth - panelW) / 2;
        int panelY = (_screenHeight - panelH) / 2 - 12;

        _statsHelpPanelRect = new Rectangle(panelX, panelY, panelW, panelH);

        Raylib.DrawRectangle(panelX, panelY, panelW, panelH, Palette.CardBg);
        Raylib.DrawRectangleLines(panelX, panelY, panelW, panelH, Palette.CardBorder);

        const int titleSize = 24;
        string title = "WHAT THE STATS MEAN";
        int titleW = (int)Raylib.MeasureTextEx(font, title, titleSize, 0.75f).X;
        Raylib.DrawTextEx(font, title,
            new Vector2(panelX + (panelW - titleW) / 2, panelY + 18),
            titleSize, 0.75f, Palette.TextPrimary);

        Raylib.DrawLine(panelX + 36, panelY + 52, panelX + panelW - 36, panelY + 52, Palette.SubtleBorder);

        int textX = panelX + 28;
        int textMaxW = panelW - 56;
        int y = panelY + 64;
        const float bodySize = 15f;
        const float bodySpacing = 0.55f;
        const int lineHeight = 20;

        DrawStatsHelpEntry(ref y, textX, textMaxW, font, bodySize, bodySpacing, lineHeight,
            "Health", Palette.Health,
            "Your overall physical condition. Food and drinks from the convenience store can raise it.");
        DrawStatsHelpEntry(ref y, textX, textMaxW, font, bodySize, bodySpacing, lineHeight,
            "Energy", Palette.Energy,
            "How rested you are. Energy fades faster as the day wears on; travel between places costs extra. Very low energy will eventually force sleep.");
        DrawStatsHelpEntry(ref y, textX, textMaxW, font, bodySize, bodySpacing, lineHeight,
            "Satiation", Palette.Satiation,
            "How well fed you are. Meals at home and store food restore it.");
        DrawStatsHelpEntry(ref y, textX, textMaxW, font, bodySize, bodySpacing, lineHeight,
            "Hydration", Palette.Hydration,
            "How hydrated you are. Drink bottled water or buy drinks at the store.");
        DrawStatsHelpEntry(ref y, textX, textMaxW, font, bodySize, bodySpacing, lineHeight,
            "Comfort", Palette.Comfort,
            "Protection from cold and exposure. Outdoors and low temperatures wear it down; heated places and a trash-bag tent help.");
        DrawStatsHelpEntry(ref y, textX, textMaxW, font, bodySize, bodySpacing, lineHeight,
            "Concealment", Palette.Concealment,
            "How unlikely you are to be spotted or caught. Darkness and good hiding spots help. Gear on the ground and a pitched tent make the area easier to find.");
        DrawStatsHelpEntry(ref y, textX, textMaxW, font, bodySize, bodySpacing, lineHeight,
            "Money", Palette.Money,
            "Rubles in hand. Spend them at the convenience store kiosk.");
        DrawStatsHelpEntry(ref y, textX, textMaxW, font, bodySize, bodySpacing, lineHeight,
            "Status", Palette.TextMuted,
            "Your current situation — where you are and how close the authorities are.");

        y += 4;
        var (arrowLines, _) = GameTextLayout.WrapForBox(
            "Arrows beside a stat show change: your recent choices (briefly), plus ongoing outdoor effects such as cold.",
            font, bodySize, bodySpacing, textMaxW, lineHeight);
        foreach (string line in arrowLines)
        {
            Raylib.DrawTextEx(font, line, new Vector2(textX, y), bodySize, bodySpacing, Palette.TextSecondary);
            y += lineHeight;
        }

        const int btnW = 120;
        const int btnH = 36;
        int btnX = panelX + (panelW - btnW) / 2;
        int btnY = panelY + panelH - 52;
        _statsHelpCloseRect = new Rectangle(btnX, btnY, btnW, btnH);
        GameDialogUi.DrawDialogButton(_statsHelpCloseRect, "CLOSE", _statsHelpCloseHovered, font);
    }

    private void DrawStatsHelpEntry(ref int y, int x, int maxWidth, Font font, float bodySize, float spacing,
        int lineHeight, string name, Color nameColor, string description)
    {
        string heading = name + " —";
        Raylib.DrawTextEx(font, heading, new Vector2(x, y), bodySize + 1f, spacing, nameColor);
        y += lineHeight;

        var (lines, _) = GameTextLayout.WrapForBox(description, font, bodySize, spacing, maxWidth, lineHeight);
        foreach (string line in lines)
        {
            Raylib.DrawTextEx(font, line, new Vector2(x + 8, y), bodySize, spacing, Palette.TextSecondary);
            y += lineHeight;
        }

        y += 6;
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
        bool canCraftMolotov = CanCraftMolotov(out _);
        bool canCraftLitMolotov = CanCraftLitMolotov(out _);
        int craftRows = (canCraftMolotov ? 1 : 0) + (canCraftLitMolotov ? 1 : 0);
        int panelH = 320 + craftRows * 70;
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
        bool canDisassemble = CanDisassembleTrashBagTent(out string disassembleBlockReason);
        bool built = _hasTrashBagTent;
        bool outdoors = GamePhase.IsOutdoorsSurvival(_phase);
        bool hasBags = HasUsableBackpackItem(GameItems.TrashBags);
        bool hasTape = HasUsableBackpackItem(GameItems.DuctTape);
        int bagsSlot = FindBackpackSlotIndex(GameItems.TrashBags);
        int tapeSlot = FindBackpackSlotIndex(GameItems.DuctTape);

        Color rowBg = _buildTentButtonHovered
            ? Palette.ButtonSelectedBg
            : new Color(16, 18, 22, 255);
        Raylib.DrawRectangleRec(_buildTentRowRect, rowBg);
        Raylib.DrawRectangleLinesEx(_buildTentRowRect, 1f, Palette.SubtleBorder);

        const int iconSize = 28;
        int iconY = rowY + (rowH - iconSize) / 2;
        DrawItemIcon(GameItems.TrashBags, new Rectangle(rowX + 10, iconY, iconSize, iconSize),
            hasBags || built ? Color.WHITE : new Color(255, 255, 255, 90), bagsSlot);
        DrawItemIcon(GameItems.DuctTape, new Rectangle(rowX + 10 + iconSize + 4, iconY, iconSize, iconSize),
            hasTape || built ? Color.WHITE : new Color(255, 255, 255, 90), tapeSlot);

        int textX = rowX + 10 + iconSize * 2 + 14;
        Raylib.DrawTextEx(font, BuildTrashBagTent,
            new Vector2(textX, rowY + 10), 20, 0.65f,
            built ? Palette.TextDim : Palette.TextPrimary);

        string reqLine = built
            ? (CountDroppedItemsInRoom(Phase.Tent) > 0
                ? "Shelter pitched — items still inside"
                : "Shelter pitched — +comfort outdoors")
            : "Uses some bags & tape · Outdoors only";
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
            if (canDisassemble)
                GameDialogUi.DrawDialogButton(_buildTentButtonRect, "TAKE DOWN", _buildTentButtonHovered, font);
            else
            {
                Raylib.DrawRectangleRec(_buildTentButtonRect, new Color(24, 26, 30, 255));
                Raylib.DrawRectangleLinesEx(_buildTentButtonRect, 1f, Palette.SubtleBorder);
                string label = "TAKE DOWN";
                int labelSize = 15;
                int labelW = (int)Raylib.MeasureTextEx(font, label, labelSize, 0.5f).X;
                Raylib.DrawTextEx(font, label,
                    new Vector2(btnX + (btnW - labelW) / 2f, btnY + 8),
                    labelSize, 0.5f, Palette.TextDim);
            }

            if (!canDisassemble && !string.IsNullOrEmpty(disassembleBlockReason))
            {
                Raylib.DrawTextEx(font, disassembleBlockReason,
                    new Vector2(textX, rowY + 42), 12, 0.45f, new Color(180, 120, 100, 255));
            }
        }
        else
        {
            if (canBuild)
                GameDialogUi.DrawDialogButton(_buildTentButtonRect, "BUILD", _buildTentButtonHovered, font);
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
            Color fbColor = _buildFeedback.Contains("Shelter pitched", StringComparison.OrdinalIgnoreCase) ||
                            _buildFeedback.Contains("crude shelter", StringComparison.OrdinalIgnoreCase)
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

        // Molotov craft row (only shown when craftable)
        int craftRowIndex = 0;

        if (canCraftMolotov)
        {
            int molotovRowY = rowY + rowH + 12 + craftRowIndex * (rowH + 12);
            _buildMolotovRowRect = new Rectangle(rowX, molotovRowY, rowW, rowH);

            Color mRowBg = _buildMolotovButtonHovered
                ? Palette.ButtonSelectedBg
                : new Color(16, 18, 22, 255);
            Raylib.DrawRectangleRec(_buildMolotovRowRect, mRowBg);
            Raylib.DrawRectangleLinesEx(_buildMolotovRowRect, 1f, Palette.SubtleBorder);

            int iconY2 = molotovRowY + (rowH - iconSize) / 2;
            int vodkaSlot = FindBackpackSlotIndex(GameItems.Vodka);
            int ragSlot = FindBackpackSlotIndex(GameItems.Rag);
            DrawItemIcon(GameItems.Vodka, new Rectangle(rowX + 10, iconY2, iconSize, iconSize), Color.WHITE, vodkaSlot);
            DrawItemIcon(GameItems.Rag, new Rectangle(rowX + 10 + iconSize + 4, iconY2, iconSize, iconSize), Color.WHITE, ragSlot);

            int textX2 = rowX + 10 + iconSize * 2 + 14;
            Raylib.DrawTextEx(font, CraftMolotov,
                new Vector2(textX2, molotovRowY + 10), 20, 0.65f, Palette.TextPrimary);
            Raylib.DrawTextEx(font, $"Uses {GameItems.Vodka} + {GameItems.Rag}",
                new Vector2(textX2, molotovRowY + 30), 14, 0.5f, Palette.TextDim);

            int mBtnW = 72;
            int mBtnH = 30;
            int mBtnX = rowX + rowW - mBtnW - 10;
            int btnY2 = molotovRowY + (rowH - mBtnH) / 2;
            _buildMolotovButtonRect = new Rectangle(mBtnX, btnY2, mBtnW, mBtnH);
            GameDialogUi.DrawDialogButton(_buildMolotovButtonRect, "CRAFT", _buildMolotovButtonHovered, font);
            craftRowIndex++;
        }
        else
        {
            _buildMolotovRowRect = new Rectangle(0, 0, 0, 0);
            _buildMolotovButtonRect = new Rectangle(0, 0, 0, 0);
        }

        // Lit Molotov craft row (only shown when craftable)
        if (canCraftLitMolotov)
        {
            int litRowY = rowY + rowH + 12 + craftRowIndex * (rowH + 12);
            _buildLitMolotovRowRect = new Rectangle(rowX, litRowY, rowW, rowH);

            Color lRowBg = _buildLitMolotovButtonHovered
                ? Palette.ButtonSelectedBg
                : new Color(16, 18, 22, 255);
            Raylib.DrawRectangleRec(_buildLitMolotovRowRect, lRowBg);
            Raylib.DrawRectangleLinesEx(_buildLitMolotovRowRect, 1f, Palette.SubtleBorder);

            int iconY2 = litRowY + (rowH - iconSize) / 2;
            int molotovSlot = FindBackpackSlotIndex(GameItems.Molotov);
            int lighterSlot = FindBackpackSlotIndex("Lighter");
            DrawItemIcon(GameItems.Molotov, new Rectangle(rowX + 10, iconY2, iconSize, iconSize), Color.WHITE, molotovSlot);
            DrawItemIcon("Lighter", new Rectangle(rowX + 10 + iconSize + 4, iconY2, iconSize, iconSize), Color.WHITE, lighterSlot);

            int textX2 = rowX + 10 + iconSize * 2 + 14;
            Raylib.DrawTextEx(font, CraftLitMolotov,
                new Vector2(textX2, litRowY + 10), 20, 0.65f, Palette.TextPrimary);
            Raylib.DrawTextEx(font, $"Uses {GameItems.Molotov} + Lighter",
                new Vector2(textX2, litRowY + 30), 14, 0.5f, Palette.TextDim);

            int lBtnW = 92;
            int lBtnH = 30;
            int lBtnX = rowX + rowW - lBtnW - 10;
            int btnY2 = litRowY + (rowH - lBtnH) / 2;
            _buildLitMolotovButtonRect = new Rectangle(lBtnX, btnY2, lBtnW, lBtnH);
            GameDialogUi.DrawDialogButton(_buildLitMolotovButtonRect, "LIGHT", _buildLitMolotovButtonHovered, font);
        }
        else
        {
            _buildLitMolotovRowRect = new Rectangle(0, 0, 0, 0);
            _buildLitMolotovButtonRect = new Rectangle(0, 0, 0, 0);
        }

        int closeW = 120;
        int closeH = 36;
        int closeX = panelX + (panelW - closeW) / 2;
        int closeY = panelY + panelH - closeH - 16;
        _buildCloseRect = new Rectangle(closeX, closeY, closeW, closeH);
        GameDialogUi.DrawDialogButton(_buildCloseRect, "CLOSE", _buildCloseHovered, font);
    }

    // =====================================================================
    // FORAGE DIALOG (modal) — choose what to gather
    // =====================================================================
    private void DrawForageDialog()
    {
        int screenW = _screenWidth;
        int screenH = _screenHeight;

        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 170));

        int panelW = 420;
        int panelH = 280;
        int panelX = (screenW - panelW) / 2;
        int panelY = (screenH - panelH) / 2 - 10;

        _foragePanelRect = new Rectangle(panelX, panelY, panelW, panelH);

        Raylib.DrawRectangle(panelX, panelY, panelW, panelH, Palette.CardBg);
        Raylib.DrawRectangleLines(panelX, panelY, panelW, panelH, Palette.CardBorder);

        Font font = _uiFont;

        Raylib.DrawTextEx(font, "FORAGE",
            new Vector2(panelX + 22, panelY + 18), 25, 0.75f, Palette.TextPrimary);

        Raylib.DrawLine(panelX + 22, panelY + 46, panelX + panelW - 22, panelY + 46, Palette.SubtleBorder);

        string subtitle = "Choose what to search for in the forest.";
        Raylib.DrawTextEx(font, subtitle,
            new Vector2(panelX + 22, panelY + 58), 18, 0.6f, Palette.TextSecondary);

        int rowY = panelY + 88;
        int rowH = 56;
        int rowX = panelX + 22;
        int rowW = panelW - 44;
        const int iconSize = 28;

        for (int i = 0; i < ForageOptionCount; i++)
        {
            _forageOptionRowRects[i] = new Rectangle(rowX, rowY, rowW, rowH);

            bool highlighted = _forageHighlightedIndex == i || _forageOptionHovered[i];
            Color rowBg = highlighted
                ? Palette.ButtonSelectedBg
                : new Color(16, 18, 22, 255);
            Raylib.DrawRectangleRec(_forageOptionRowRects[i], rowBg);
            Raylib.DrawRectangleLinesEx(_forageOptionRowRects[i], 1f, Palette.SubtleBorder);

            int iconY = rowY + (rowH - iconSize) / 2;
            DrawItemIcon(ForageOptionItems[i], new Rectangle(rowX + 10, iconY, iconSize, iconSize), Color.WHITE);

            int textX = rowX + 10 + iconSize + 14;
            string label = ForageOptionItems[i].ToUpperInvariant();
            Raylib.DrawTextEx(font, label,
                new Vector2(textX, rowY + 10), 20, 0.65f, Palette.TextPrimary);
            Raylib.DrawTextEx(font, ForageOptionDescriptions[i],
                new Vector2(textX, rowY + 30), 14, 0.5f, Palette.TextDim);

            rowY += rowH + 8;
        }

        int closeW = 120;
        int closeH = 36;
        int closeX = panelX + (panelW - closeW) / 2;
        int closeY = panelY + panelH - closeH - 16;
        _forageCloseRect = new Rectangle(closeX, closeY, closeW, closeH);
        GameDialogUi.DrawDialogButton(_forageCloseRect, "CLOSE", _forageCloseHovered, font);
    }

    // =====================================================================
    // CAFÉ OWNER DIALOG (modal) — talk to Boris (Bratva)
    // =====================================================================
    private const int CafeOwnerPortraitColumnW = 158;

    private void DrawCafeOwnerDialog()
    {
        int screenW = _screenWidth;
        int screenH = _screenHeight;

        GameDialogUi.DrawModalBackdrop(screenW, screenH);

        int panelW = 640;
        int panelH = 440;
        int panelX = (screenW - panelW) / 2;
        int panelY = (screenH - panelH) / 2 - 10;

        _cafeOwnerPanelRect = new Rectangle(panelX, panelY, panelW, panelH);

        Raylib.DrawRectangle(panelX, panelY, panelW, panelH, Palette.CardBg);
        Raylib.DrawRectangleLines(panelX, panelY, panelW, panelH, Palette.CardBorder);

        Font font = _uiFont;

        int contentX = panelX + CafeOwnerPortraitColumnW;
        int contentW = panelW - CafeOwnerPortraitColumnW - 22;

        DrawCafeOwnerPortrait(panelX, panelY, panelH);

        Raylib.DrawLine(contentX - 8, panelY + 14, contentX - 8, panelY + panelH - 14, Palette.SubtleBorder);

        Raylib.DrawTextEx(font, CafeOwnerDialog.Title,
            new Vector2(contentX, panelY + 18), 25, 0.75f, Palette.TextPrimary);

        Raylib.DrawTextEx(font, CafeOwnerDialog.Subtitle,
            new Vector2(contentX, panelY + 44), 14, 0.5f, Palette.TextDim);

        Raylib.DrawLine(contentX, panelY + 64, panelX + panelW - 22, panelY + 64, Palette.SubtleBorder);

        string response = CafeOwnerDialog.GetResponseText(
            _cafeOwnerDialogStage, _cafeOwnerSelectedOption, _borisDeliveryJobActive);
        int responseY = panelY + 74;
        DrawWrappedDialogText(font, response, contentX, responseY, contentW, 16, 0.55f, Palette.TextSecondary);

        string pickPrompt = _cafeOwnerDialogStage == CafeOwnerDialog.Stage.DeliveryOffer
            ? CafeOwnerDialog.DeliveryPickPrompt
            : CafeOwnerDialog.PickPrompt;
        Raylib.DrawTextEx(font, pickPrompt,
            new Vector2(contentX, panelY + 148), 16, 0.55f, Palette.TextDim);

        int rowY = panelY + 172;
        int rowH = 44;
        int rowX = contentX;
        int rowW = contentW;
        int optionCount = CafeOwnerDialog.GetOptionCount(_cafeOwnerDialogStage);
        string[] playerLines = CafeOwnerDialog.GetPlayerLines(_cafeOwnerDialogStage);

        for (int i = 0; i < optionCount; i++)
        {
            _cafeOwnerOptionRowRects[i] = new Rectangle(rowX, rowY, rowW, rowH);

            bool highlighted = _cafeOwnerHighlightedIndex == i || _cafeOwnerOptionHovered[i];
            bool chosen = _cafeOwnerSelectedOption == i;
            Color rowBg = highlighted || chosen
                ? Palette.ButtonSelectedBg
                : new Color(16, 18, 22, 255);
            Raylib.DrawRectangleRec(_cafeOwnerOptionRowRects[i], rowBg);
            Color border = chosen ? Palette.ButtonSelectedBorder : Palette.SubtleBorder;
            Raylib.DrawRectangleLinesEx(_cafeOwnerOptionRowRects[i], 1f, border);

            Raylib.DrawTextEx(font, playerLines[i],
                new Vector2(rowX + 12, rowY + 12), 17, 0.55f, Palette.TextPrimary);

            rowY += rowH + 6;
        }

        int closeW = 120;
        int closeH = 36;
        int closeX = contentX + (contentW - closeW) / 2;
        int closeY = panelY + panelH - closeH - 16;
        _cafeOwnerCloseRect = new Rectangle(closeX, closeY, closeW, closeH);
        GameDialogUi.DrawDialogButton(_cafeOwnerCloseRect, "CLOSE", _cafeOwnerCloseHovered, font);
    }

    private void DrawCafeOwnerPortrait(int panelX, int panelY, int panelH)
    {
        const int pad = 14;
        const float portraitAspect = 3f / 4f; // width / height — matches cafe-owner-portrait.png

        int frameW = CafeOwnerPortraitColumnW - pad * 2;
        int frameH = (int)MathF.Round(frameW / portraitAspect);
        int maxH = panelH - pad * 2 - 8;
        if (frameH > maxH)
        {
            frameH = maxH;
            frameW = (int)MathF.Round(frameH * portraitAspect);
        }

        float frameX = panelX + pad + (CafeOwnerPortraitColumnW - pad * 2 - frameW) / 2f;
        float frameY = panelY + pad + 4 + (maxH - frameH) / 2f;
        var frame = new Rectangle(frameX, frameY, frameW, frameH);

        Raylib.DrawRectangleRec(frame, new Color(10, 11, 14, 255));
        Raylib.DrawRectangleLinesEx(frame, 1.5f, Palette.SubtleBorder);

        if (_cafeOwnerPortraitTexture.Id == 0)
            return;

        var inset = new Rectangle(frame.X + 3, frame.Y + 3, frame.Width - 6, frame.Height - 6);
        Rectangle src = new Rectangle(0, 0, _cafeOwnerPortraitTexture.Width, _cafeOwnerPortraitTexture.Height);
        float texAspect = _cafeOwnerPortraitTexture.Width / (float)_cafeOwnerPortraitTexture.Height;
        Rectangle fitted = FitRectangleAspect(inset, texAspect, cover: false);
        Raylib.DrawTexturePro(_cafeOwnerPortraitTexture, src, fitted, Vector2.Zero, 0f, Color.WHITE);
    }

    /// <summary>Size and center a rect to match texture aspect (contain or cover).</summary>
    private static Rectangle FitRectangleAspect(Rectangle bounds, float widthOverHeight, bool cover)
    {
        float boundsAspect = bounds.Width / bounds.Height;
        float destW;
        float destH;

        if (cover)
        {
            if (boundsAspect > widthOverHeight)
            {
                destH = bounds.Height;
                destW = destH * widthOverHeight;
            }
            else
            {
                destW = bounds.Width;
                destH = destW / widthOverHeight;
            }
        }
        else
        {
            if (boundsAspect > widthOverHeight)
            {
                destW = bounds.Width;
                destH = destW / widthOverHeight;
            }
            else
            {
                destH = bounds.Height;
                destW = destH * widthOverHeight;
            }
        }

        return new Rectangle(
            bounds.X + (bounds.Width - destW) / 2f,
            bounds.Y + (bounds.Height - destH) / 2f,
            destW,
            destH);
    }

    private static void DrawWrappedDialogText(
        Font font,
        string text,
        float x,
        float y,
        float maxWidth,
        float fontSize,
        float spacing,
        Color color)
    {
        if (string.IsNullOrEmpty(text))
            return;

        string[] words = text.Split(' ');
        string line = "";
        float lineY = y;

        foreach (string word in words)
        {
            string trial = string.IsNullOrEmpty(line) ? word : line + " " + word;
            if (Raylib.MeasureTextEx(font, trial, fontSize, spacing).X > maxWidth && !string.IsNullOrEmpty(line))
            {
                Raylib.DrawTextEx(font, line, new Vector2(x, lineY), fontSize, spacing, color);
                lineY += fontSize + 4f;
                line = word;
            }
            else
            {
                line = trial;
            }
        }

        if (!string.IsNullOrEmpty(line))
            Raylib.DrawTextEx(font, line, new Vector2(x, lineY), fontSize, spacing, color);
    }

    // =====================================================================
    // STORE BUY MENU (modal shopping interface)
    // =====================================================================
    private void DrawStoreBuyMenu()
    {
        int screenW = _screenWidth;
        int screenH = _screenHeight;

        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 160));

        int panelW = 720;
        int panelH = 360;
        int panelX = (screenW - panelW) / 2;
        int panelY = (screenH - panelH) / 2 - 10;

        _storeBuyPanelRect = new Rectangle(panelX, panelY, panelW, panelH);

        Raylib.DrawRectangle(panelX, panelY, panelW, panelH, Palette.CardBg);
        Raylib.DrawRectangleLines(panelX, panelY, panelW, panelH, Palette.CardBorder);

        Font font = _uiFont;

        string title = "SHELVES";
        int titleSize = 28;
        Raylib.DrawTextEx(font, title,
            new Vector2(panelX + 24, panelY + 18),
            titleSize, 0.8f, Palette.TextPrimary);

        string moneyStr = $"{_money:N0} ₽";
        int moneyW = (int)Raylib.MeasureTextEx(font, moneyStr, 20, 0.6f).X;
        Raylib.DrawTextEx(font, moneyStr,
            new Vector2(panelX + panelW - 24 - moneyW, panelY + 20),
            20, 0.6f, Palette.TextSecondary);

        int headerBottom = panelY + 50;
        Raylib.DrawLine(panelX + 20, headerBottom, panelX + panelW - 20, headerBottom, Palette.SubtleBorder);

        int listX = panelX + 16;
        int listW = 268;
        int contentTop = headerBottom + 10;
        int panelBottom = panelY + panelH - 12;
        int closeH = 32;
        int closeW = 100;
        int closeY = panelBottom - closeH;
        int closeX = listX + (listW - closeW) / 2;
        int listBottom = closeY - 10;
        int dividerX = listX + listW + 8;
        int detailX = dividerX + 9;
        int detailW = panelX + panelW - 20 - detailX;

        Raylib.DrawLine(dividerX, contentTop, dividerX, panelBottom, Palette.SubtleBorder);
        Raylib.DrawLine(listX, closeY - 6, listX + listW, closeY - 6, Palette.SubtleBorder);

        int rowHeight = 44;
        const int iconSize = 28;

        for (int i = 0; i < StoreCatalog.Entries.Length; i++)
        {
            var (name, price, _, _, _) = StoreCatalog.Entries[i];

            int rowY = contentTop + i * rowHeight;
            bool canAfford = _money >= price;
            bool hasSpace = _backpack.Any(s => string.IsNullOrEmpty(s));
            bool rowHovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), _storeBuyItemRects[i]);
            bool rowHighlighted = i == _storeBuyHighlightedIndex;
            bool rowConfirmed = _storeBuyDetailIndex >= 0 && i == _storeBuyDetailIndex;

            _storeBuyItemRects[i] = new Rectangle(listX, rowY, listW, rowHeight - 4);

            if (rowConfirmed)
                Raylib.DrawRectangle(listX, rowY, listW, rowHeight - 4, new Color(62, 58, 48, 200));
            else if (rowHovered || rowHighlighted)
                Raylib.DrawRectangle(listX, rowY, listW, rowHeight - 4, new Color(48, 46, 40, 180));

            Color tint = (canAfford && hasSpace) ? Color.WHITE : new Color(120, 118, 112, 255);
            int iconY = rowY + (rowHeight - 4 - iconSize) / 2;
            Raylib.DrawRectangle(listX + 6, iconY - 1, iconSize + 2, iconSize + 2, new Color(18, 17, 15, 255));
            DrawItemIcon(name, new Rectangle(listX + 7, iconY, iconSize, iconSize), tint);

            Color nameColor = (canAfford && hasSpace) ? Palette.TextPrimary : Palette.TextMuted;
            Raylib.DrawTextEx(font, name, new Vector2(listX + 42, rowY + 6), 18, 0.6f, nameColor);

            string priceStr = $"{price} ₽";
            int pW = (int)Raylib.MeasureTextEx(font, priceStr, 17, 0.6f).X;
            Color priceColor = canAfford ? new Color(185, 160, 90, 255) : Palette.TextMuted;
            Raylib.DrawTextEx(font, priceStr,
                new Vector2(listX + listW - 10 - pW, rowY + 8), 17, 0.6f, priceColor);
        }

        DrawStoreBuyDetailPanel(font, detailX, contentTop, detailW, panelBottom - contentTop);

        if (_storeBuyFeedbackTimer > 0f && !string.IsNullOrEmpty(_storeBuyFeedback))
        {
            int fbW = (int)Raylib.MeasureTextEx(font, _storeBuyFeedback, 17, 0.5f).X;
            Raylib.DrawTextEx(font, _storeBuyFeedback,
                new Vector2(detailX + (detailW - fbW) / 2, panelBottom - 48),
                17, 0.5f, Palette.TextSecondary);
        }

        _storeBuyCloseRect = new Rectangle(closeX, closeY, closeW, closeH);
        GameDialogUi.DrawDialogButton(_storeBuyCloseRect, "CLOSE", _storeBuyCloseHovered, font);
    }

    private void DrawStoreBuyDetailPanel(Font font, int x, int y, int w, int h)
    {
        if (_storeBuyDetailIndex < 0)
        {
            string hint = "Select an item";
            int hintSize = 20;
            int hintW = (int)Raylib.MeasureTextEx(font, hint, hintSize, 0.6f).X;
            Raylib.DrawTextEx(font, hint,
                new Vector2(x + (w - hintW) / 2, y + h / 2 - 12),
                hintSize, 0.6f, Palette.TextMuted);
            _storeBuyPurchaseRect = new Rectangle(0, 0, 0, 0);
            return;
        }

        var (name, price, satiation, hydration, health) = StoreCatalog.Entries[_storeBuyDetailIndex];
        bool canBuy = CanBuyStoreItem(_storeBuyDetailIndex);

        const int iconSize = 64;
        int iconX = x + (w - iconSize) / 2;
        int iconY = y + 4;
        Raylib.DrawRectangle(iconX - 2, iconY - 2, iconSize + 4, iconSize + 4, new Color(22, 20, 17, 255));
        Raylib.DrawRectangleLines(iconX - 2, iconY - 2, iconSize + 4, iconSize + 4, Palette.SubtleBorder);
        DrawItemIcon(name, new Rectangle(iconX, iconY, iconSize, iconSize), Color.WHITE);

        string title = name.ToUpperInvariant();
        int titleSize = 22;
        int titleW = (int)Raylib.MeasureTextEx(font, title, titleSize, 0.75f).X;
        Raylib.DrawTextEx(font, title,
            new Vector2(x + (w - titleW) / 2, iconY + iconSize + 10),
            titleSize, 0.75f, Palette.TextPrimary);

        string priceStr = $"{price} ₽";
        int priceW = (int)Raylib.MeasureTextEx(font, priceStr, 20, 0.6f).X;
        Color priceColor = _money >= price ? new Color(185, 160, 90, 255) : Palette.TextMuted;
        Raylib.DrawTextEx(font, priceStr,
            new Vector2(x + (w - priceW) / 2, iconY + iconSize + 36),
            20, 0.6f, priceColor);

        int textY = iconY + iconSize + 62;
        string flavor = StoreCatalog.GetFlavorText(name);
        int flavorSize = 16;
        float flavorSpacing = 0.55f;
        int flavorLineHeight = 22;
        var (flavorLines, _) = GameTextLayout.WrapForBox(flavor, font, flavorSize, flavorSpacing, w - 8, flavorLineHeight);
        foreach (string line in flavorLines)
        {
            Raylib.DrawTextEx(font, line, new Vector2(x + 4, textY), flavorSize, flavorSpacing, Palette.TextSecondary);
            textY += flavorLineHeight;
        }

        textY += 4;
        string effects = StoreCatalog.FormatEffects(name, satiation, hydration, health);
        Raylib.DrawTextEx(font, effects, new Vector2(x + 4, textY), 15, 0.5f, Palette.TextDim);
        textY += 22;

        if (!canBuy)
        {
            string blockReason = _money < price ? "Not enough money." : "Backpack is full.";
            Raylib.DrawTextEx(font, blockReason, new Vector2(x + 4, textY), 15, 0.5f, new Color(200, 130, 110, 255));
        }

        int btnW = 108;
        int btnH = 34;
        int btnX = x + (w - btnW) / 2;
        int btnY = y + h - btnH;
        _storeBuyPurchaseRect = new Rectangle(btnX, btnY, btnW, btnH);

        if (canBuy)
            GameDialogUi.DrawDialogButton(_storeBuyPurchaseRect, "BUY", _storeBuyPurchaseHovered, font);
        else
        {
            Raylib.DrawRectangleRec(_storeBuyPurchaseRect, new Color(24, 26, 30, 255));
            Raylib.DrawRectangleLinesEx(_storeBuyPurchaseRect, 1f, Palette.SubtleBorder);
            int labelSize = 18;
            int labelW = (int)Raylib.MeasureTextEx(font, "BUY", labelSize, 0.55f).X;
            Raylib.DrawTextEx(font, "BUY",
                new Vector2(btnX + (btnW - labelW) / 2f, btnY + 8),
                labelSize, 0.55f, Palette.TextMuted);
        }
    }

    // =====================================================================
    // GLOVE COMPARTMENT MENU (modal — take items, store-style layout)
    // =====================================================================
    private void DrawGloveBoxMenu()
    {
        RefreshGloveBoxVisibleList();

        int screenW = _screenWidth;
        int screenH = _screenHeight;

        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 160));

        int panelW = 720;
        int panelH = 360;
        int panelX = (screenW - panelW) / 2;
        int panelY = (screenH - panelH) / 2 - 10;

        _gloveBoxPanelRect = new Rectangle(panelX, panelY, panelW, panelH);

        Raylib.DrawRectangle(panelX, panelY, panelW, panelH, Palette.CardBg);
        Raylib.DrawRectangleLines(panelX, panelY, panelW, panelH, Palette.CardBorder);

        Font font = _uiFont;

        Raylib.DrawTextEx(font, "GLOVE COMPARTMENT",
            new Vector2(panelX + 24, panelY + 18), 28, 0.8f, Palette.TextPrimary);

        string hint = "Take what you want";
        int hintW = (int)Raylib.MeasureTextEx(font, hint, 18, 0.55f).X;
        Raylib.DrawTextEx(font, hint,
            new Vector2(panelX + panelW - 24 - hintW, panelY + 22),
            18, 0.55f, Palette.TextSecondary);

        int headerBottom = panelY + 50;
        Raylib.DrawLine(panelX + 20, headerBottom, panelX + panelW - 20, headerBottom, Palette.SubtleBorder);

        int listX = panelX + 16;
        int listW = 268;
        int contentTop = headerBottom + 10;
        int panelBottom = panelY + panelH - 12;
        int closeH = 32;
        int closeW = 100;
        int closeY = panelBottom - closeH;
        int closeX = listX + (listW - closeW) / 2;
        int dividerX = listX + listW + 8;
        int detailX = dividerX + 9;
        int detailW = panelX + panelW - 20 - detailX;

        Raylib.DrawLine(dividerX, contentTop, dividerX, panelBottom, Palette.SubtleBorder);
        Raylib.DrawLine(listX, closeY - 6, listX + listW, closeY - 6, Palette.SubtleBorder);

        int rowHeight = 44;
        const int iconSize = 28;

        for (int i = 0; i < _gloveBoxVisibleCount; i++)
        {
            int catalogIndex = _gloveBoxVisibleCatalogIndices[i];
            var entry = GloveCompartmentCatalog.Entries[catalogIndex];
            bool canTake = CanTakeGloveBoxItem(catalogIndex);

            int rowY = contentTop + i * rowHeight;
            _gloveBoxItemRects[i] = new Rectangle(listX, rowY, listW, rowHeight - 4);
            bool rowHovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), _gloveBoxItemRects[i]);
            bool rowHighlighted = i == _gloveBoxHighlightedIndex;
            bool rowConfirmed = _gloveBoxDetailIndex >= 0 && i == _gloveBoxDetailIndex;

            if (rowConfirmed)
                Raylib.DrawRectangle(listX, rowY, listW, rowHeight - 4, new Color(62, 58, 48, 200));
            else if (rowHovered || rowHighlighted)
                Raylib.DrawRectangle(listX, rowY, listW, rowHeight - 4, new Color(48, 46, 40, 180));

            Color tint = canTake ? Color.WHITE : new Color(120, 118, 112, 255);
            int iconY = rowY + (rowHeight - 4 - iconSize) / 2;
            Raylib.DrawRectangle(listX + 6, iconY - 1, iconSize + 2, iconSize + 2, new Color(18, 17, 15, 255));
            DrawItemIcon(entry.IconItemName, new Rectangle(listX + 7, iconY, iconSize, iconSize), tint);

            Color nameColor = Palette.TextPrimary;
            Raylib.DrawTextEx(font, entry.Name, new Vector2(listX + 42, rowY + 6), 18, 0.6f, nameColor);
        }

        DrawGloveBoxDetailPanel(font, detailX, contentTop, detailW, panelBottom - contentTop);

        if (_gloveBoxFeedbackTimer > 0f && !string.IsNullOrEmpty(_gloveBoxFeedback))
        {
            int fbW = (int)Raylib.MeasureTextEx(font, _gloveBoxFeedback, 17, 0.5f).X;
            Raylib.DrawTextEx(font, _gloveBoxFeedback,
                new Vector2(detailX + (detailW - fbW) / 2, panelBottom - 48),
                17, 0.5f, Palette.TextSecondary);
        }

        _gloveBoxCloseRect = new Rectangle(closeX, closeY, closeW, closeH);
        GameDialogUi.DrawDialogButton(_gloveBoxCloseRect, "CLOSE", _gloveBoxCloseHovered, font);
    }

    private void DrawGloveBoxDetailPanel(Font font, int x, int y, int w, int h)
    {
        if (_gloveBoxDetailIndex < 0)
        {
            string hint = "Select an item";
            int hintSize = 20;
            int hintW = (int)Raylib.MeasureTextEx(font, hint, hintSize, 0.6f).X;
            Raylib.DrawTextEx(font, hint,
                new Vector2(x + (w - hintW) / 2, y + h / 2 - 12),
                hintSize, 0.6f, Palette.TextMuted);
            _gloveBoxPickupRect = new Rectangle(0, 0, 0, 0);
            return;
        }

        int catalogIndex = GetGloveBoxCatalogIndexFromVisibleIndex(_gloveBoxDetailIndex);
        if (catalogIndex < 0)
        {
            _gloveBoxDetailIndex = -1;
            _gloveBoxPickupRect = new Rectangle(0, 0, 0, 0);
            return;
        }

        var entry = GloveCompartmentCatalog.Entries[catalogIndex];
        bool canTake = CanTakeGloveBoxItem(catalogIndex);

        const int iconSize = 64;
        int iconX = x + (w - iconSize) / 2;
        int iconY = y + 4;
        Raylib.DrawRectangle(iconX - 2, iconY - 2, iconSize + 4, iconSize + 4, new Color(22, 20, 17, 255));
        Raylib.DrawRectangleLines(iconX - 2, iconY - 2, iconSize + 4, iconSize + 4, Palette.SubtleBorder);
        DrawItemIcon(entry.IconItemName, new Rectangle(iconX, iconY, iconSize, iconSize), Color.WHITE);

        string title = entry.Name.ToUpperInvariant();
        int titleW = (int)Raylib.MeasureTextEx(font, title, 22, 0.75f).X;
        Raylib.DrawTextEx(font, title,
            new Vector2(x + (w - titleW) / 2, iconY + iconSize + 10),
            22, 0.75f, Palette.TextPrimary);

        int textY = iconY + iconSize + 36;
        const int flavorSize = 16;
        float flavorSpacing = 0.55f;
        int flavorLineHeight = 22;
        var (flavorLines, _) = GameTextLayout.WrapForBox(entry.Flavor, font, flavorSize, flavorSpacing, w - 8, flavorLineHeight);
        foreach (string line in flavorLines)
        {
            Raylib.DrawTextEx(font, line, new Vector2(x + 4, textY), flavorSize, flavorSpacing, Palette.TextSecondary);
            textY += flavorLineHeight;
        }

        textY += 4;
        Raylib.DrawTextEx(font, entry.EffectHint, new Vector2(x + 4, textY), 15, 0.5f, Palette.TextDim);
        textY += 22;

        if (!canTake)
        {
            Raylib.DrawTextEx(font, "Backpack is full.", new Vector2(x + 4, textY), 15, 0.5f,
                new Color(200, 130, 110, 255));
        }

        int btnW = 108;
        int btnH = 34;
        int btnX = x + (w - btnW) / 2;
        int btnY = y + h - btnH;
        _gloveBoxPickupRect = new Rectangle(btnX, btnY, btnW, btnH);

        if (canTake)
            GameDialogUi.DrawDialogButton(_gloveBoxPickupRect, "TAKE", _gloveBoxPickupHovered, font);
        else
        {
            Raylib.DrawRectangleRec(_gloveBoxPickupRect, new Color(24, 26, 30, 255));
            Raylib.DrawRectangleLinesEx(_gloveBoxPickupRect, 1f, Palette.SubtleBorder);
            int labelSize = 18;
            int labelW = (int)Raylib.MeasureTextEx(font, "TAKE", labelSize, 0.55f).X;
            Raylib.DrawTextEx(font, "TAKE",
                new Vector2(btnX + (btnW - labelW) / 2f, btnY + 8),
                labelSize, 0.55f, Palette.TextMuted);
        }
    }

    // --- Render ---
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
            case Phase.Town:
            case Phase.IndustrialDistrict:
            case Phase.CommercialDistrict:
            case Phase.Store:
            case Phase.Cafe:
            case Phase.DeliveryTruck:
            case Phase.WarehouseTruck:
            case Phase.WarehouseAmbush:
            case Phase.ForestEntry:
            case Phase.Forest:
            case Phase.ForestStream:
            case Phase.Tent:
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

        if (_showGloveBoxMenu)
        {
            DrawGloveBoxMenu();
        }

        if (_showRegionMap)
        {
            DrawRegionMapModal();
        }

        if (_showBuildDialog)
        {
            DrawBuildDialog();
        }

        if (_showForageDialog)
        {
            DrawForageDialog();
        }

        if (_showCafeOwnerDialog)
        {
            DrawCafeOwnerDialog();
        }

        if (_showQuitConfirm)
        {
            DrawQuitConfirmDialog();
        }

        if (_showStatsHelp)
        {
            DrawStatsHelpDialog();
        }

        if (_showControllerDebug)
        {
            DrawControllerDebugScreen();
            DrawTopRightButtons();
        }

        DrawBuildDateBottomRight();

        Raylib.EndDrawing();
    }

    private void DrawBuildDateBottomRight()
    {
        const float pad = 6f;

        string buildLine = $"Build date: {BuildInfo.Timestamp}";
        float rightEdge = _screenWidth - pad;
        float bottomEdge = _screenHeight - pad;
        float minX = pad;
        float maxWidth = MathF.Max(0f, rightEdge - minX);

        float fontSize = 16f;
        float width = Raylib.MeasureTextEx(_uiFontItalic, buildLine, fontSize, 0.8f).X;
        while (width > maxWidth && fontSize > 11f)
        {
            fontSize -= 1f;
            width = Raylib.MeasureTextEx(_uiFontItalic, buildLine, fontSize, 0.8f).X;
        }

        float x = rightEdge - width;
        float textH = Raylib.MeasureTextEx(_uiFontItalic, buildLine, fontSize, 0.8f).Y;
        float y = bottomEdge - textH + 7f;
        Color buildTint = new Color((byte)Palette.TextMuted.R, (byte)Palette.TextMuted.G, (byte)Palette.TextMuted.B, (byte)165);
        Raylib.DrawTextEx(_uiFontItalic, buildLine, new Vector2(x, y), fontSize, 0.8f, buildTint);
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

        // LEFT ZONE — title logo (conscript-title.png)
        int leftX = 26;
        const int titleLogoHeight = 38;
        int titleW;
        if (_titleLogoTexture.Id != 0)
        {
            titleW = (int)(titleLogoHeight * (_titleLogoTexture.Width / (float)_titleLogoTexture.Height));
            Rectangle src = new Rectangle(0, 0, _titleLogoTexture.Width, _titleLogoTexture.Height);
            Rectangle dst = new Rectangle(leftX, row1Y, titleW, titleLogoHeight);
            Raylib.DrawTexturePro(_titleLogoTexture, src, dst, Vector2.Zero, 0f, Color.WHITE);
        }
        else
        {
            Raylib.DrawTextEx(font, "CONSCRIPT",
                new Vector2(leftX, row1Y),
                LayoutConstants.TitleFontSize, 0.85f, Palette.TextPrimary);
            titleW = (int)Raylib.MeasureTextEx(font, "CONSCRIPT", LayoutConstants.TitleFontSize, 0.85f).X;
            Raylib.DrawLine(leftX, row1Y + 34, leftX + titleW, row1Y + 34, Palette.StrongBorder);
        }

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

        SeasonIconDrawing.Draw(iconCenterX, iconCenterY, _season, iconSize);

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

        // === STATUS header + info icon ===
        const int statsInfoIconSize = 16;
        Raylib.DrawTextEx(font, "STATUS",
            new Vector2(tx, cy), LayoutConstants.SidebarHeaderSize, 0.7f, Palette.TextMuted);
        int statusLabelW = (int)Raylib.MeasureTextEx(font, "STATUS", LayoutConstants.SidebarHeaderSize, 0.7f).X;
        _statsHelpIconRect = new Rectangle(tx + statusLabelW + 8, cy + 1, statsInfoIconSize, statsInfoIconSize);
        GameDialogUi.DrawInfoIcon(font, _statsHelpIconRect, _statsHelpIconHovered);
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
        DrawCleanStatLine(ref cy, tx, "Concealment", _concealment, 0, 0, Palette.Concealment);

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
        if (GamePhase.IsForestSurvival(_phase))
        {
            cy += 44;
            DrawHuntSidebarButton(cy, tx);
            cy += 44;
            DrawForageSidebarButton(cy, tx);
        }
        else
        {
            _huntSidebarButtonRect = default;
            _huntSidebarButtonHovered = false;
            _forageSidebarButtonRect = default;
            _forageSidebarButtonHovered = false;
        }

        const int btnH = 36;
        int quitY = y + h - GameConstants.SidebarPadding - btnH;
        DrawQuitSidebarButton(quitY, tx);
    }

    private void DrawQuitSidebarButton(int y, int x)
    {
        Font font = _uiFont;
        int available = GameConstants.RightPanelWidth - GameConstants.SidebarPadding * 2;
        const int btnH = 36;
        _quitSidebarButtonRect = new Rectangle(x, y, available, btnH);
        GameDialogUi.DrawDialogButton(_quitSidebarButtonRect, "QUIT", _quitSidebarButtonHovered, font);
    }

    private void DrawQuitConfirmDialog()
    {
        GameDialogUi.DrawModalBackdrop(_screenWidth, _screenHeight, 175);

        Font font = _uiFont;
        int panelW = 400;
        int panelH = 190;
        int panelX = (_screenWidth - panelW) / 2;
        int panelY = (_screenHeight - panelH) / 2 - 16;

        _quitConfirmPanelRect = new Rectangle(panelX, panelY, panelW, panelH);

        Raylib.DrawRectangle(panelX, panelY, panelW, panelH, Palette.CardBg);
        Raylib.DrawRectangleLines(panelX, panelY, panelW, panelH, Palette.CardBorder);

        string title = "QUIT?";
        int titleSize = 24;
        int titleW = (int)Raylib.MeasureTextEx(font, title, titleSize, 0.8f).X;
        Raylib.DrawTextEx(font, title,
            new Vector2(panelX + (panelW - titleW) / 2, panelY + 22),
            titleSize, 0.8f, Palette.TextPrimary);

        string body = "Are you sure you want to exit the game?";
        int bodySize = 17;
        int bodyW = (int)Raylib.MeasureTextEx(font, body, bodySize, 0.6f).X;
        Raylib.DrawTextEx(font, body,
            new Vector2(panelX + (panelW - bodyW) / 2, panelY + 62),
            bodySize, 0.6f, Palette.TextSecondary);

        Raylib.DrawLine(panelX + 40, panelY + 98, panelX + panelW - 40, panelY + 98, Palette.SubtleBorder);

        int btnW = 108;
        int btnH = 36;
        int gap = 14;
        int totalW = btnW * 2 + gap;
        int startX = panelX + (panelW - totalW) / 2;
        int btnY = panelY + panelH - 56;

        _quitConfirmNoRect = new Rectangle(startX, btnY, btnW, btnH);
        _quitConfirmYesRect = new Rectangle(startX + btnW + gap, btnY, btnW, btnH);

        GameDialogUi.DrawDialogButton(_quitConfirmNoRect, "CANCEL", _quitConfirmNoHovered, font);
        GameDialogUi.DrawDialogButton(_quitConfirmYesRect, "QUIT", _quitConfirmYesHovered, font);
    }

    private void DrawBuildSidebarButton(int y, int x)
    {
        Font font = _uiFont;
        int available = GameConstants.RightPanelWidth - GameConstants.SidebarPadding * 2;
        const int btnH = 36;
        _buildSidebarButtonRect = new Rectangle(x, y, available, btnH);
        GameDialogUi.DrawDialogButton(_buildSidebarButtonRect, "BUILD", _buildSidebarButtonHovered, font);
    }

    private void DrawHuntSidebarButton(int y, int x)
    {
        Font font = _uiFont;
        int available = GameConstants.RightPanelWidth - GameConstants.SidebarPadding * 2;
        const int btnH = 36;
        _huntSidebarButtonRect = new Rectangle(x, y, available, btnH);
        GameDialogUi.DrawDialogButton(_huntSidebarButtonRect, ChoiceHunt, _huntSidebarButtonHovered, font);
    }

    private void DrawForageSidebarButton(int y, int x)
    {
        Font font = _uiFont;
        int available = GameConstants.RightPanelWidth - GameConstants.SidebarPadding * 2;
        const int btnH = 36;
        _forageSidebarButtonRect = new Rectangle(x, y, available, btnH);
        GameDialogUi.DrawDialogButton(_forageSidebarButtonRect, ChoiceForage, _forageSidebarButtonHovered, font);
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
            int count = GameStatMath.StatArrowCount(leftTotal);
            int slotRight = x + StatLeftArrowSlotW - 4;
            int startX = slotRight - (count - 1) * StatArrowSpacing;
            for (int i = 0; i < count; i++)
                DrawChevronLeft(startX + i * StatArrowSpacing, arrowY, negative);
        }

        if (rightTotal > 0)
        {
            int count = GameStatMath.StatArrowCount(rightTotal);
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
                        DrawItemIcon(item!, new Rectangle(sx + 2, sy + 2, slot - 4, slot - 4), Color.WHITE, idx);
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

    private Rectangle GetSidebarMapDrawRect(Rectangle mapArea)
    {
        float drawW = mapArea.Width;
        float drawH = drawW / RegionMapGeo.LonLatAspect;
        if (drawH > mapArea.Height)
        {
            drawH = mapArea.Height;
            drawW = drawH * RegionMapGeo.LonLatAspect;
        }

        return new Rectangle(
            mapArea.X + (mapArea.Width - drawW) * 0.5f,
            mapArea.Y + (mapArea.Height - drawH) * 0.5f,
            drawW,
            drawH);
    }

    private void GetMapViewBounds(out double minLon, out double maxLon, out double minLat, out double maxLat)
    {
        double fullLatSpan = RegionMapGeo.MaxLat - RegionMapGeo.MinLat;
        double viewLatSpan = fullLatSpan / CurrentMapZoom;
        double viewLonSpan = viewLatSpan * _expandedMapViewAspect;

        minLon = _mapViewCenterLon - viewLonSpan / 2;
        maxLon = _mapViewCenterLon + viewLonSpan / 2;
        minLat = _mapViewCenterLat - viewLatSpan / 2;
        maxLat = _mapViewCenterLat + viewLatSpan / 2;
    }

    private void ClampMapViewCenter()
    {
        double fullLatSpan = RegionMapGeo.MaxLat - RegionMapGeo.MinLat;
        double fullLonSpan = RegionMapGeo.MaxLon - RegionMapGeo.MinLon;
        double halfLat = fullLatSpan / CurrentMapZoom / 2;
        double halfLon = halfLat * _expandedMapViewAspect;

        double latMin = RegionMapGeo.MinLat + Math.Min(halfLat, fullLatSpan / 2);
        double latMax = RegionMapGeo.MaxLat - Math.Min(halfLat, fullLatSpan / 2);
        _mapViewCenterLat = RegionMapGeo.SafeClamp(_mapViewCenterLat, latMin, latMax);

        double lonMin = RegionMapGeo.MinLon + Math.Min(halfLon, fullLonSpan / 2);
        double lonMax = RegionMapGeo.MaxLon - Math.Min(halfLon, fullLonSpan / 2);
        _mapViewCenterLon = RegionMapGeo.SafeClamp(_mapViewCenterLon, lonMin, lonMax);
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

        double newLatSpan = (RegionMapGeo.MaxLat - RegionMapGeo.MinLat) / CurrentMapZoom;
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
        GameDialogUi.DrawDialogButton(_regionMapCloseRect, "CLOSE", _regionMapCloseHovered, font);
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
        double viewMinLon = RegionMapGeo.MinLon,
        double viewMaxLon = RegionMapGeo.MaxLon,
        double viewMinLat = RegionMapGeo.MinLat,
        double viewMaxLat = RegionMapGeo.MaxLat)
    {
        Font font = _uiFont;

        Raylib.DrawRectangleRec(mapRect, new Color(12, 14, 18, 255));

        if (_regionMapTexture.Id != 0)
        {
            double fullLonSpan = RegionMapGeo.MaxLon - RegionMapGeo.MinLon;
            double fullLatSpan = RegionMapGeo.MaxLat - RegionMapGeo.MinLat;
            float texW = _regionMapTexture.Width;
            float texH = _regionMapTexture.Height;

            double viewLonSpan = viewMaxLon - viewMinLon;
            double viewLatSpan = viewMaxLat - viewMinLat;

            Raylib.BeginScissorMode((int)mapRect.X, (int)mapRect.Y, (int)mapRect.Width, (int)mapRect.Height);

            if (viewLonSpan > 1e-9 && viewLatSpan > 1e-9)
            {
                double geoMinLon = Math.Max(viewMinLon, RegionMapGeo.MinLon);
                double geoMaxLon = Math.Min(viewMaxLon, RegionMapGeo.MaxLon);
                double geoMinLat = Math.Max(viewMinLat, RegionMapGeo.MinLat);
                double geoMaxLat = Math.Min(viewMaxLat, RegionMapGeo.MaxLat);

                if (geoMinLon < geoMaxLon && geoMinLat < geoMaxLat)
                {
                    float destX = mapRect.X + (float)((geoMinLon - viewMinLon) / viewLonSpan * mapRect.Width);
                    float destY = mapRect.Y + (float)((viewMaxLat - geoMaxLat) / viewLatSpan * mapRect.Height);
                    float destW = (float)((geoMaxLon - geoMinLon) / viewLonSpan * mapRect.Width);
                    float destH = (float)((geoMaxLat - geoMinLat) / viewLatSpan * mapRect.Height);
                    Rectangle dest = new Rectangle(destX, destY, destW, destH);

                    Rectangle src = new Rectangle(
                        (float)((geoMinLon - RegionMapGeo.MinLon) / fullLonSpan * texW),
                        (float)((RegionMapGeo.MaxLat - geoMaxLat) / fullLatSpan * texH),
                        (float)((geoMaxLon - geoMinLon) / fullLonSpan * texW),
                        (float)((geoMaxLat - geoMinLat) / fullLatSpan * texH));

                    Raylib.DrawTexturePro(_regionMapTexture, src, dest, Vector2.Zero, 0f, Color.WHITE);
                }
            }

            Raylib.EndScissorMode();
        }

        Raylib.DrawRectangleLinesEx(mapRect, 1f, Palette.SubtleBorder);

        (double lon, double lat) = GetMapPlayerGeoPosition();
        Vector2 player = RegionMapGeo.LonLatToPixel(mapRect, lon, lat, viewMinLon, viewMaxLon, viewMinLat, viewMaxLat);

        int px = (int)player.X;
        int py = (int)player.Y;
        float glowR = markerRadius + 2.5f;
        Raylib.DrawCircle(px, py, glowR, new Color(195, 175, 105, 50));
        Raylib.DrawCircle(px, py, markerRadius, Palette.ActionFlash);
        Raylib.DrawCircleLines(px, py, (int)(markerRadius + 1.5f), Palette.TextPrimary);

        string markerLabel = GamePhase.IsForestSurvival(_phase) ? "You" : "Ulan-Ude";
        int labelW = (int)Raylib.MeasureTextEx(font, markerLabel, labelFontSize, 0.35f).X;
        Raylib.DrawTextEx(font, markerLabel,
            new Vector2(player.X - labelW / 2f, player.Y + markerRadius + 4),
            labelFontSize, 0.35f, Palette.TextPrimary);
    }

    private (double lon, double lat) GetMapPlayerGeoPosition() =>
        _phase switch
        {
            Phase.Town                => (RegionMapGeo.TownLon, RegionMapGeo.TownLat),
            Phase.IndustrialDistrict  => (RegionMapGeo.IndustrialDistrictLon, RegionMapGeo.IndustrialDistrictLat),
            Phase.Cafe                => (RegionMapGeo.CafeLon, RegionMapGeo.CafeLat),
            Phase.DeliveryTruck       => (RegionMapGeo.DeliveryTruckLon, RegionMapGeo.DeliveryTruckLat),
            Phase.WarehouseTruck      => (RegionMapGeo.WarehouseLon, RegionMapGeo.WarehouseLat),
            Phase.WarehouseAmbush     => (RegionMapGeo.WarehouseLon, RegionMapGeo.WarehouseLat),
            Phase.CommercialDistrict  => (RegionMapGeo.CommercialDistrictLon, RegionMapGeo.CommercialDistrictLat),
            Phase.ForestEntry  => (RegionMapGeo.ForestEntryLon, RegionMapGeo.ForestEntryLat),
            Phase.Forest       => (RegionMapGeo.ForestCampLon, RegionMapGeo.ForestCampLat),
            Phase.ForestStream => (RegionMapGeo.ForestStreamLon, RegionMapGeo.ForestStreamLat),
            _                  => (RegionMapGeo.UlanUdeLon, RegionMapGeo.UlanUdeLat)
        };

    private string GetSceneNarrative()
    {
        return _phase switch
        {
            Phase.Opening => OpeningNarrative,
            Phase.Outside => OutsideNarrative,
            Phase.Town               => TownNarrative,
            Phase.IndustrialDistrict => IndustrialDistrictNarrative,
            Phase.Cafe               => CafeNarrative,
            Phase.DeliveryTruck      => DeliveryTruckNarrative,
            Phase.WarehouseTruck     => WarehouseTruckNarrative,
            Phase.WarehouseAmbush    => WarehouseAmbushNarrative,
            Phase.CommercialDistrict => CommercialDistrictNarrative,
            Phase.Store   => StoreNarrative,
            Phase.ForestEntry  => ForestEntryNarrative,
            Phase.Forest       => ForestNarrative,
            Phase.ForestStream => ForestStreamNarrative,
            Phase.Tent         => TentNarrative,
            _                  => ForestNarrative
        };
    }

    private static void GetCinematicArtBounds(out int artX, out int artY, out int artW, out int artH)
    {
        int left = GameConstants.SceneLeft;
        int top = GameConstants.SceneTop;
        int w = GameConstants.SceneWidth;
        int h = GameConstants.SceneHeight;
        artX = left + GameConstants.ScenePadding;
        artY = top + GameConstants.ScenePadding;
        artW = w - GameConstants.ScenePadding * 2;
        artH = h - GameConstants.ScenePadding * 2;
    }

    // Floor scatter (varied x, low y — left/center, clear of the narrative card)
    private static readonly (float x, float y)[] DroppedItemSceneAnchors =
    {
        (0.22f, 0.78f), (0.36f, 0.82f), (0.50f, 0.76f), (0.28f, 0.85f), (0.42f, 0.79f), (0.56f, 0.83f)
    };

    /// <summary>Draw clickable item icons for things left on the ground in this room.</summary>
    private void DrawDroppedItemsInScene(int artX, int artY, int artW, int artH)
    {
        _droppedItemClickRects.Clear();
        _droppedItemVisibleIndices.Clear();

        for (int i = 0; i < _droppedItems.Count; i++)
        {
            DroppedItem item = _droppedItems[i];
            if (item.Room != _phase || item.TurnsRemaining <= 0)
                continue;

            int anchor = item.AnchorIndex % DroppedItemSceneAnchors.Length;
            (float ax, float ay) = DroppedItemSceneAnchors[anchor];
            int plate = DroppedItemSceneIconSize + DroppedItemScenePlatePad * 2;
            int px = artX + (int)(artW * ax) - plate / 2;
            int py = artY + (int)(artH * ay) - plate / 2;
            var clickRect = new Rectangle(px, py, plate, plate);

            int listIndex = _droppedItemClickRects.Count;
            bool hovered = _hoveredDroppedItemListIndex == listIndex;

            DrawDroppedItemMarker(item.Name, clickRect, hovered, -1, item.Charges);

            _droppedItemVisibleIndices.Add(i);
            _droppedItemClickRects.Add(clickRect);
        }
    }

    /// <summary>High-contrast ground marker so dropped items stay visible on dark scene photos.</summary>
    private void DrawDroppedItemMarker(string itemName, Rectangle plateRect, bool hovered, int slotIndex, int? chargesOverride)
    {
        int px = (int)plateRect.X;
        int py = (int)plateRect.Y;
        int plate = (int)plateRect.Width;

        Raylib.DrawRectangle(px + 3, py + plate - 7, plate - 6, 6, new Color(0, 0, 0, 120));

        var plateBg = hovered ? new Color(48, 44, 38, 245) : new Color(28, 26, 22, 235);
        Raylib.DrawRectangle(px, py, plate, plate, plateBg);
        Color border = hovered ? Palette.ActionFlash : new Color(110, 102, 88, 255);
        Raylib.DrawRectangleLines(px, py, plate, plate, border);
        if (hovered)
            Raylib.DrawRectangle(px + 1, py + 1, plate - 2, plate - 2, new Color(200, 185, 120, 22));

        int pad = DroppedItemScenePlatePad;
        var iconDest = new Rectangle(px + pad, py + pad, plate - pad * 2, plate - pad * 2);
        var tint = hovered ? Color.WHITE : new Color(235, 230, 218, 255);

        if (_itemIcons.ContainsKey(itemName))
            DrawItemIcon(itemName, iconDest, tint, slotIndex, chargesOverride);
        else
        {
            Font font = _uiFont;
            string label = itemName.Length > 6 ? itemName[..6] : itemName;
            float fz = 11f;
            Vector2 size = Raylib.MeasureTextEx(font, label, fz, 0.4f);
            Raylib.DrawTextEx(font, label.ToUpperInvariant(),
                new Vector2(iconDest.X + (iconDest.Width - size.X) / 2f, iconDest.Y + (iconDest.Height - size.Y) / 2f),
                fz, 0.4f, Palette.TextPrimary);
        }
    }

    private static Rectangle ComputeTrashBagTentDestRect(int artX, int artY, int artW, int artH, int tentTexW, int tentTexH)
    {
        if (tentTexW <= 0 || tentTexH <= 0)
            return default;

        int groundY = artY + (int)(artH * 0.73f);
        int destW = (int)(artW * 0.34f);
        int destH = (int)(destW * (tentTexH / (float)tentTexW));
        int destX = artX + (int)(artW * 0.04f);
        int destY = groundY - destH + (int)(destH * 0.06f);
        return new Rectangle(destX, destY, destW, destH);
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

        GetCinematicArtBounds(out int artX, out int artY, out int artW, out int artH);

        DrawSceneBackground(artX, artY, artW, artH);

        if (_hasTrashBagTent && GamePhase.IsOutdoorsSurvival(_phase))
            DrawTrashBagTentOverlay(artX, artY, artW, artH);

        if (_phase == Phase.DeliveryTruck)
            DrawDeliveryTruckGloveCompartmentHotspot(artX, artY, artW, artH);

        // Light atmospheric snow (outdoor scenes only)
        if (GamePhase.IsOutdoorsSurvival(_phase))
        {
            int groundY = artY + (int)(artH * 0.68f);
            DrawAtmosphericSnow(artX, artY, artW, groundY, 48);
        }

        // === Inner elegant frame + vignette for cinematic feel ===
        Raylib.DrawRectangleLines(artX + 2, artY + 2, artW - 4, artH - 4, Palette.SubtleBorder);

        // Stronger vignette on the edges
        Raylib.DrawRectangle(artX, artY, artW, 18, new Color(0, 0, 0, 70));
        Raylib.DrawRectangle(artX, artY + artH - 22, artW, 22, new Color(0, 0, 0, 80));
        Raylib.DrawRectangle(artX, artY, 22, artH, new Color(0, 0, 0, 55));
        Raylib.DrawRectangle(artX + artW - 22, artY, 22, artH, new Color(0, 0, 0, 55));

        // === Main narrative / flavor text box — clean, anchored to the right side of the image ===
        DrawRightSideNarrative(artX, artY, artW, artH, GetSceneNarrative());

        // Dropped items on top of overlays/narrative so they stay visible and clickable
        DrawDroppedItemsInScene(artX, artY, artW, artH);

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

        DrawDroppedItemsInScene(artX, artY, artW, artH);

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

        // The single try-again button is drawn by DrawActionBar (we set _choices to ChoiceTryAgain)
        DrawActionBar();

        DrawTopRightButtons();
    }

    private static Rectangle ComputeDeliveryTruckGloveBoxClickRect(int artX, int artY, int artW, int artH)
    {
        // Driver's POV — glove compartment latch on the right side of the dashboard
        int x = artX + (int)(artW * 0.58f);
        int y = artY + (int)(artH * 0.50f);
        int w = (int)(artW * 0.24f);
        int h = (int)(artH * 0.20f);
        return new Rectangle(x, y, w, h);
    }

    private void DrawDeliveryTruckGloveCompartmentHotspot(int artX, int artY, int artW, int artH)
    {
        if (!GloveCompartmentHasRemainingLoot() || _gloveCompartmentClickRect.Width <= 0)
            return;

        Rectangle r = _gloveCompartmentClickRect;

        Color fill = _gloveCompartmentHovered
            ? new Color(200, 185, 120, 36)
            : new Color(200, 185, 120, 14);
        Raylib.DrawRectangleRec(r, fill);
        Color border = _gloveCompartmentHovered
            ? new Color(220, 200, 130, 200)
            : new Color(180, 165, 110, 90);
        Raylib.DrawRectangleLinesEx(r, _gloveCompartmentHovered ? 2f : 1f, border);

        if (_gloveCompartmentHovered)
        {
            const string label = "GLOVE COMPARTMENT";
            Font font = _uiFont;
            float fontSize = 13f;
            Vector2 size = Raylib.MeasureTextEx(font, label, fontSize, 0.45f);
            float lx = r.X + (r.Width - size.X) / 2f;
            float ly = r.Y - size.Y - 4f;
            Raylib.DrawRectangle((int)lx - 4, (int)ly - 2, (int)size.X + 8, (int)size.Y + 4,
                new Color(8, 10, 14, 210));
            Raylib.DrawTextEx(font, label, new Vector2(lx, ly), fontSize, 0.45f, Palette.TextPrimary);
        }
    }

    private void DrawTrashBagTentOverlay(int artX, int artY, int artW, int artH)
    {
        if (_trashBagTentTexture.Id == 0)
        {
            _trashBagTentClickRect = default;
            return;
        }

        Rectangle dst = ComputeTrashBagTentDestRect(
            artX, artY, artW, artH, _trashBagTentTexture.Width, _trashBagTentTexture.Height);
        _trashBagTentClickRect = dst;

        Rectangle src = new Rectangle(0, 0, _trashBagTentTexture.Width, _trashBagTentTexture.Height);
        Color tint = GetOutdoorTimeOfDayTint();
        Raylib.DrawTexturePro(_trashBagTentTexture, src, dst, Vector2.Zero, 0f, tint);

        if (_trashBagTentHovered)
        {
            Raylib.DrawRectangleLinesEx(dst, 2f, new Color(200, 185, 120, 200));
            Raylib.DrawRectangle((int)dst.X, (int)dst.Y, (int)dst.Width, (int)dst.Height,
                new Color(200, 185, 120, 18));
        }
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
        int blankLineHeight = lineHeight / 2;

        int maxCardWidth = 320;
        int horizontalPadding = 18;
        int verticalPadding = 16;

        int textMaxWidth = maxCardWidth - horizontalPadding * 2;

        var (wrappedLines, textHeight) = GameTextLayout.WrapForBox(
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

        int y = textTop;
        for (int i = 0; i < wrappedLines.Count; i++)
        {
            Raylib.DrawTextEx(
                font,
                wrappedLines[i],
                new Vector2(textLeft, y),
                fontSize,
                spacing,
                Palette.TextPrimary);

            y += string.IsNullOrEmpty(wrappedLines[i]) ? blankLineHeight : lineHeight;
        }
    }

    // =====================================================================
    // BOTTOM ACTION BAR — Strong visual weight, clear, tactile buttons
    // =====================================================================

    /// <summary>
    /// Computes the on-screen rectangles for the current action buttons.
    /// Used by both drawing and mouse hit-testing so the layout stays in one place.
    /// </summary>
    private Rectangle[] ComputeActionButtonRects()
    {
        int barY = _screenHeight - GameConstants.ActionBarHeight;
        int barH = GameConstants.ActionBarHeight;

        int count = _choices.Length;
        if (count == 0)
            return Array.Empty<Rectangle>();

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
    // BOTTOM ACTION BAR
    // =====================================================================
    private void DrawActionBar()
    {
        if (_choices.Length == 0)
            return;

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

    // --- Player stats (environment, concealment) ---
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
        stat = GameStatMath.ClampStat(stat + amount);
        MarkActionChanged();
    }

    private void SetStatFromAction(ref int stat, ref int actionDelta, int value)
    {
        int clamped = GameStatMath.ClampStat(value);
        int change = clamped - stat;
        if (change == 0) return;
        actionDelta += change;
        stat = clamped;
        MarkActionChanged();
    }

    private void ApplyEnvironmentOnAction()
    {
        if (!GamePhase.IsOutdoorsSurvival(_phase)) return;
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

    private void ApplyEnvironmentTentInterior()
    {
        ClearNonComfortEnvDeltas();
        SetEnvironmentComfort(TentInteriorComfortBonus);
    }

    private const int HeatedBuildingComfortBonus = 4;   // 1 green arrow — warmed up a little indoors

    private void SetEnvironmentComfort(int targetDelta)
    {
        int diff = targetDelta - _envComfortDelta;
        if (diff == 0) return;
        _envComfortDelta = targetDelta;
        _comfort = GameStatMath.ClampStat(_comfort + diff);
    }

    private int OutdoorComfortPerActionPenalty() =>
        SurvivalEnvironment.OutdoorComfortPerActionPenalty(_temperatureF);

    private int OutdoorShelterComfortBonus() =>
        _hasTrashBagTent && GamePhase.IsOutdoorsSurvival(_phase) ? TrashBagTentComfortBonus : 0;

    private void RefreshOutdoorComfortEnvironment()
    {
        if (!GamePhase.IsOutdoorsSurvival(_phase)) return;
        SetEnvironmentComfort(SurvivalEnvironment.OutdoorComfortPenaltyForTemp(_temperatureF) + OutdoorShelterComfortBonus());
    }

    /// <summary>Extra concealment from darkness; scaled down in well-lit or already-hidden places.</summary>
    private int ConcealmentTimeBonus()
    {
        if (!IsNightTimeSlot())
            return 0;

        int bonus = _timeOfDay == "Late Night" ? 14 : 10;

        return _phase switch
        {
            Phase.Store => bonus / 4,    // lit interior — night barely helps
            Phase.Cafe => bonus / 4,
            Phase.Tent => bonus / 3,     // already hidden
            Phase.Opening => bonus / 2,  // indoors with lights on
            _ => bonus
        };
    }

    private int ConcealmentDroppedItemsPenalty() =>
        CountDroppedItemsInRoom(_phase) * ConcealmentPenaltyPerDroppedItem;

    private int ConcealmentTentPenalty() =>
        _hasTrashBagTent && _tentBuiltInPhase == _phase ? ConcealmentPenaltyForTent : 0;

    private void RefreshConcealment() =>
        _concealment = GameStatMath.ClampStat(
            SurvivalEnvironment.ConcealmentForPhase(_phase) + ConcealmentTimeBonus()
            - ConcealmentDroppedItemsPenalty() - ConcealmentTentPenalty());

}

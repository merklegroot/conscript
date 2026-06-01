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
    private Texture2D _warehouseAftermathBackground;
    private Texture2D _warehouseClosedDoorTexture;
    private Texture2D _warehouseInteriorBackground;
    private Texture2D _gasStationBackground;
    private Texture2D _cafeOwnerPortraitTexture;
    private Texture2D _tentBackground;
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
    private const string ChoiceBrowseKiosk = "BROWSE KIOSK";
    private const string ChoiceLeaveStore = "LEAVE THE WAY YOU CAME";
    private const string ChoiceCafe = "КАФЕ";
    private const string ChoiceTalkToOwner = "TALK TO THE OWNER";
    private const string ChoiceLeaveCafe = "LEAVE THE WAY YOU CAME";
    private const string ChoiceDriveToWarehouse = "DRIVE TO THE WAREHOUSE";
    private const string ChoiceGetOutOfTruck = "GET OUT OF THE TRUCK";
    private const string ChoiceGetBackInTruck = "GET BACK IN THE TRUCK";
    private const string ChoiceFight = "FIGHT";
    private const string ChoiceBackToLoadingBay = "BACK TO THE BAY";
    private const string ChoiceWalkToGasStation = "WALK TO GAS STATION";
    private const string ChoiceWait = "WAIT";
    private const string ChoiceTryAgain = "Try again";
    private const string ChoiceOpenDoor = "Open the door";
    private const string ChoiceFleeOutWindow = "Flee out the window";
    private const string ChoiceBarDoorAndFight = "Bar the door and fight";


    private const int TentSleepTimeSteps = 3;      // ~9 hours (8 slots/day ≈ 3 hours each)

    // Items left on the ground in a room (location = phase)
    private const int DroppedItemLifetimeTurns = 5;
    private const int MaxDroppedItemsPerRoom = 6;
    private const int DroppedItemSceneIconSize = 54;
    private const int DroppedItemScenePlatePad = 5;

    private readonly Dictionary<string, Texture2D> _itemIcons = new(StringComparer.OrdinalIgnoreCase);

    // --- Top-right utility buttons (restart, debug start, controller) ---
    private Rectangle _undoButtonRect;
    private Rectangle _redoButtonRect;
    private Rectangle _restartButtonRect;
    private Rectangle _debugStartButtonRect;
    private Rectangle _areaSelectButtonRect;
    private Rectangle _controllerButtonRect;
    private Rectangle _copyRoomIdButtonRect;
    private bool _undoHovered;
    private bool _redoHovered;
    private bool _restartHovered;
    private bool _debugStartHovered;
    private bool _areaSelectHovered;
    private bool _controllerHovered;
    private bool _copyRoomIdHovered;

    private readonly List<GameStateSnapshot> _undoStack = new();
    private readonly List<GameStateSnapshot> _redoStack = new();
    private bool _isRestoringHistory;
    private int _historyRecordSuppression;
    private const int MaxHistoryDepth = 50;

    private readonly SceneAreaSelect _sceneAreaSelect = new();

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
        WarehouseAftermath, // Molotov blast — bratdvas dead, bay scorched
        WarehouseInterior, // Inside the hangar — entered via bay keypad
        GasStation,    // All-night fuel stop off the industrial roads
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
    private bool _warehouseAmbushersDead;
    private bool _foldedPaperMessageRead;
    private bool _noteMessageRead;
    private Texture2D _foldedPaperNoteTexture;
    private Texture2D _crateNoteTexture;
    private string _activeNoteReadItemName = "";
    private readonly FoldedPaperReaderDialog _foldedPaperReader = new();
    private readonly GasGaugeViewerDialog _gasGaugeViewer = new();
    private float _gasGaugeFuel = GasGaugeCatalog.EmptyFuel;
    private Rectangle _gasGaugeClickRect;
    private bool _gasGaugeHotspotHovered;
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

    // Sergei fled wearing his winter jacket (on his body, not in the backpack grid).

    // --- Backpack & ground items ---
    private const int BackpackColumns = 4;
    private const int BackpackRows = 3;
    private const int BackpackSlotCount = BackpackColumns * BackpackRows;

    private string?[] _backpack = new string?[BackpackSlotCount] { "Knife", "Lighter", "Phone", null, null, null, null, null, null, null, null, null };
    // Remaining uses per slot (null = full/default for that item type)
    private int?[] _backpackItemCharges = new int?[BackpackSlotCount];

    // Items dropped in the current scene (per-room, expire after several turns)
    private readonly List<DroppedItem> _droppedItems = new();
    private readonly List<Rectangle> _droppedItemClickRects = new();
    private readonly List<int> _droppedItemVisibleIndices = new(); // parallel to click rects → _droppedItems index
    private int _hoveredDroppedItemListIndex = -1; // index into visible/click lists

    // Item detail panel (left sidebar under backpack when item is selected)
    private bool _showItemDialog;
    private int _dialogItemIndex = -1;
    private int _dialogDroppedItemIndex = -1;
    private string _dialogItemName = "";
    private Rectangle _dialogActionRect;
    private bool _dialogActionHovered;
    private Rectangle _dialogSecondaryActionRect;
    private bool _dialogSecondaryActionHovered;
    private Rectangle _dialogDropRect;
    private bool _dialogDropHovered;

    // Shop buy menu (modal) — convenience store shelves or gas station kiosk
    private bool _showStoreBuyMenu;
    private ShopKind _shopBuyKind;
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
    private Rectangle _gloveBoxTakeAllRect;
    private bool _gloveBoxTakeAllHovered;
    private Rectangle _gloveBoxPickupRect;
    private bool _gloveBoxPickupHovered;
    private readonly bool[] _gloveBoxLootTaken = new bool[GloveCompartmentCatalog.EntryCount];
    private readonly int[] _gloveBoxVisibleCatalogIndices = new int[GloveCompartmentCatalog.EntryCount];
    private int _gloveBoxVisibleCount;

    // Warehouse aftermath — searchable bratdva bodies
    private bool _showBodyLootMenu;
    private int _activeBodyIndex = -1;
    private readonly bool[] _bodyLootTaken = new bool[WarehouseBodyLootCatalog.TotalLootCount];
    private int _bodyLootHighlightedIndex;
    private int _bodyLootDetailIndex = -1;
    private string _bodyLootFeedback = "";
    private float _bodyLootFeedbackTimer;
    private readonly Rectangle[] _bodyLootItemRects = new Rectangle[WarehouseBodyLootCatalog.MaxItemsPerBody];
    private Rectangle _bodyLootPanelRect;
    private Rectangle _bodyLootCloseRect;
    private bool _bodyLootCloseHovered;
    private Rectangle _bodyLootTakeAllRect;
    private bool _bodyLootTakeAllHovered;
    private Rectangle _bodyLootPickupRect;
    private bool _bodyLootPickupHovered;
    private readonly int[] _bodyLootVisibleCatalogIndices = new int[WarehouseBodyLootCatalog.MaxItemsPerBody];
    private int _bodyLootVisibleCount;
    private readonly Rectangle[] _bodyClickRects = new Rectangle[WarehouseBodyLootCatalog.BodyCount];
    private int _hoveredBodyIndex = -1;
    private Rectangle _warehouseLockClickRect;
    private bool _warehouseLockHotspotHovered;
    private Rectangle _warehouseDoorClickRect;
    private bool _warehouseDoorHotspotHovered;
    private Rectangle _warehouseTruckClickRect;
    private bool _warehouseTruckHotspotHovered;
    private readonly NumericKeypadLockDialog _warehouseKeypad =
        new(WarehouseAftermathHotspots.LockCode, WarehouseAftermathHotspots.LockCode.Length);

    // Warehouse interior — sealed crate
    private Rectangle _warehouseInteriorExitClickRect;
    private bool _warehouseInteriorExitHotspotHovered;
    private Rectangle _warehouseCrateClickRect;
    private bool _warehouseCrateHotspotHovered;
    private bool _warehouseCrateOpened;
    private bool _showWarehouseCrateDialog;
    private Rectangle _warehouseCratePanelRect;
    private Rectangle _warehouseCrateCloseRect;
    private bool _warehouseCrateCloseHovered;

    // Warehouse crate loot (after prying open)
    private bool _showCrateLootMenu;
    private int _crateLootHighlightedIndex;
    private int _crateLootDetailIndex = -1;
    private string _crateLootFeedback = "";
    private float _crateLootFeedbackTimer;
    private readonly Rectangle[] _crateLootItemRects = new Rectangle[WarehouseCrateLootCatalog.EntryCount];
    private Rectangle _crateLootPanelRect;
    private Rectangle _crateLootCloseRect;
    private bool _crateLootCloseHovered;
    private Rectangle _crateLootTakeAllRect;
    private bool _crateLootTakeAllHovered;
    private Rectangle _crateLootPickupRect;
    private bool _crateLootPickupHovered;
    private readonly bool[] _crateLootTaken = new bool[WarehouseCrateLootCatalog.EntryCount];
    private readonly int[] _crateLootVisibleCatalogIndices = new int[WarehouseCrateLootCatalog.EntryCount];
    private int _crateLootVisibleCount;

    // Opening apartment — bedroom window
    private Rectangle _openingWindowClickRect;
    private bool _openingWindowHotspotHovered;

    // Café — Boris behind the counter
    private Rectangle _cafeBorisClickRect;
    private bool _cafeBorisHotspotHovered;

    // Scene item use — guide an inventory item onto hotspots (mouse or left stick)
    private bool _itemUseActive;
    private bool _itemUsePointerInsideBounds;
    private string _itemUseItemName = "";
    private int _itemUseSlotIndex = -1;
    private Vector2 _itemUseCursorPos;
    private const float ItemUseStickDeadzone = 0.18f;
    private const float ItemUseMoveSpeed = 480f;
    private const int ItemUseIconSize = 56;

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
    private Rectangle _trashBagTentClickRect;
    private bool _trashBagTentHovered;
    private Rectangle _gloveCompartmentClickRect;
    private bool _gloveCompartmentHovered;

    // Cached backpack slot rectangles (updated during DrawBackpack every frame)
    private Rectangle[] _backpackSlotRects = new Rectangle[BackpackSlotCount];

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

    private const string WarehouseAftermathNarrative =
        "The loading bay is choking on black smoke.\n" +
        "Two figures lie motionless in the guttering flames — bratdvas.\n" +
        "Whatever was in that bottle burned hotter than any vodka.\n" +
        "Rain hisses on the embers. The roll-up door is scorched black.";

    private const string WarehouseInteriorNarrative =
        "Fluorescents buzz overhead. Pallets and shrink-wrapped crates line the aisles.\n" +
        "The air smells of diesel, cardboard, and something chemical.\n" +
        "Through the cracked roll-up door, rain and distant firelight stain the wet concrete.\n" +
        "Whoever ran this bay left in a hurry — or never left at all.";

    private const string GasStationNarrative =
        "A lone gas station blazes under sodium lights at the edge of the yards.\n" +
        "Pumps stand in the rain, no attendant behind the grimy glass.\n" +
        "Distant orange glow still stains the clouds toward the warehouse.\n" +
        "Your truck is back there on empty — if you mean to run, you need fuel.";

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

        StopItemUseMode();

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
                _season = "Early Autumn";
                _temperatureF = 34;   // tense night outside the apartment
                // Reset backpack to starting gear (knife, lighter, phone)
                _backpack = new string?[BackpackSlotCount] { "Knife", "Lighter", "Phone", null, null, null, null, null, null, null, null, null };
                _backpackItemCharges = new int?[BackpackSlotCount];
                _hasTrashBagTent = false;
                _tentBuiltInPhase = null;
                break;

            case Phase.ForestEntry:
                _day = 0;
                _timeOfDay = "Night";
                _location = "Forest Entry";
                _city = "Ulan-Ude, Republic of Buryatia";
                _season = "Early Autumn";
                _temperatureF = 22;
                RefreshOutdoorActionChoices();
                break;

            case Phase.Forest:
                _day = 3;
                _timeOfDay = "Morning";
                _location = "Deep Forest";
                _city = "Ulan-Ude, Republic of Buryatia";
                _season = "Early Autumn";
                _temperatureF = 19;   // colder the deeper you go
                RefreshOutdoorActionChoices();
                break;

            case Phase.ForestStream:
                _day = 3;
                _timeOfDay = "Morning";
                _location = "Forest Stream";
                _city = "Ulan-Ude, Republic of Buryatia";
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
                _season = "Early Autumn";
                _temperatureF = 27;   // clear cold night in the yard
                RefreshOutdoorActionChoices();
                break;

            case Phase.Town:
                _day = 0;
                _timeOfDay = "Night";
                _location = "Town";
                _city = "Ulan-Ude, Republic of Buryatia";
                _season = "Early Autumn";
                _temperatureF = 26;
                RefreshOutdoorActionChoices();
                break;

            case Phase.IndustrialDistrict:
                _day = 0;
                _timeOfDay = "Night";
                _location = "Industrial District";
                _city = "Ulan-Ude, Republic of Buryatia";
                _season = "Early Autumn";
                _temperatureF = 24;
                RefreshOutdoorActionChoices();
                break;

            case Phase.CommercialDistrict:
                _day = 0;
                _timeOfDay = "Night";
                _location = "Commercial District";
                _city = "Ulan-Ude, Republic of Buryatia";
                _season = "Early Autumn";
                _temperatureF = 26;
                RefreshOutdoorActionChoices();
                break;

            case Phase.Store:
                _choices = new[]
                {
                    ChoiceBrowseShelves,
                    ChoiceLeaveStore,
                    ChoiceWait
                };
                _day = 0;
                _timeOfDay = "Night";
                _location = "Convenience Store";
                _city = "Ulan-Ude, Republic of Buryatia";
                _season = "Early Autumn";
                _temperatureF = 24;   // slightly warmer inside
                break;

            case Phase.Cafe:
                _choices = new[]
                {
                    ChoiceTalkToOwner,
                    ChoiceLeaveCafe,
                    ChoiceWait
                };
                _day = 0;
                _timeOfDay = "Night";
                _location = "Кафе";
                _city = "Ulan-Ude, Republic of Buryatia";
                _season = "Early Autumn";
                _temperatureF = 28;
                break;

            case Phase.DeliveryTruck:
                _choices = new[] { ChoiceDriveToWarehouse, ChoiceWait };
                _day = 0;
                _timeOfDay = "Night";
                _location = "Delivery Truck";
                _city = "Ulan-Ude, Republic of Buryatia";
                _season = "Early Autumn";
                _temperatureF = 22;
                break;

            case Phase.WarehouseTruck:
                _choices = new[] { ChoiceGetOutOfTruck, ChoiceWait };
                _day = 0;
                _timeOfDay = "Night";
                _location = $"{CafeOwnerDialog.WarehouseName} — Bay 3";
                _city = "Ulan-Ude, Republic of Buryatia";
                _season = "Early Autumn";
                _temperatureF = 21;
                break;

            case Phase.WarehouseAmbush:
                _choices = new[] { ChoiceGetBackInTruck, ChoiceFight, ChoiceWait };
                _selectedIndex = 0;
                _day = 0;
                _timeOfDay = "Night";
                _location = $"{CafeOwnerDialog.WarehouseName} — Bay 3";
                _city = "Ulan-Ude, Republic of Buryatia";
                _season = "Early Autumn";
                _temperatureF = 21;
                break;

            case Phase.WarehouseAftermath:
                _choices = new[] { ChoiceGetBackInTruck, ChoiceWalkToGasStation, ChoiceWait };
                _selectedIndex = 0;
                _day = 0;
                _timeOfDay = "Night";
                _location = $"{CafeOwnerDialog.WarehouseName} — Bay 3";
                _city = "Ulan-Ude, Republic of Buryatia";
                _season = "Early Autumn";
                _temperatureF = 24;
                break;

            case Phase.GasStation:
                _choices = new[] { ChoiceBrowseKiosk, ChoiceBackToLoadingBay, ChoiceWait };
                _selectedIndex = 0;
                _day = 0;
                _timeOfDay = "Night";
                _location = "Gas Station";
                _city = "Ulan-Ude, Republic of Buryatia";
                _season = "Early Autumn";
                _temperatureF = 22;
                break;

            case Phase.WarehouseInterior:
                _choices = new[] { ChoiceBackToLoadingBay, ChoiceWait };
                _selectedIndex = 0;
                _day = 0;
                _timeOfDay = "Night";
                _location = $"{CafeOwnerDialog.WarehouseName} — Inside";
                _city = "Ulan-Ude, Republic of Buryatia";
                _season = "Early Autumn";
                _temperatureF = 22;
                break;

            case Phase.Tent:
                _choices = new[] { ChoiceExitTent, ChoiceDisassembleTent, ChoiceSleep, ChoiceWait };
                _location = "Trash Bag Tent";
                break;
        }

        // Swap the background image for the new phase
        ApplyBackgroundForCurrentPhase();

        if (newPhase == Phase.Opening)
            ClearDroppedItems();
    }

    private void ApplyBackgroundForCurrentPhase()
    {
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
            Phase.WarehouseAftermath => _warehouseAftermathBackground,
            Phase.WarehouseInterior => _warehouseInteriorBackground,
            Phase.GasStation        => _gasStationBackground,
            Phase.ForestEntry  => _forestEntryBackground,
            Phase.Forest       => _forestBackground,
            Phase.ForestStream => _forestStreamBackground,
            Phase.Tent         => _tentBackground,
            _                  => _forestBackground
        };
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
                if (_day < 3)
                {
                    _day = 3;
                    _timeOfDay = "Morning";
                }
                break;
            case Phase.ForestStream:
                _location = "Forest Stream";
                _temperatureF = 17;
                _backgroundTexture = _forestStreamBackground;
                break;
        }

        RefreshOutdoorActionChoices();
    }

    // --- Time of day ---
    /// <summary>
    /// Advances the time of day by the given number of slots.
    /// Wrapping from Late Night to Morning starts a new day.
    /// </summary>

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
        }

        // Temperature drifts with time of day (colder at night) — only outside the apartment
        if (_phase == Phase.Outside || GamePhase.IsTownDistrict(_phase) || GamePhase.IsForestSurvival(_phase))
        {
            if (IsNightTimeSlot())
                _temperatureF = Math.Max(-40, _temperatureF - 2);
            else if (IsMorningTimeSlot())
                _temperatureF = Math.Min(60, _temperatureF + 1);
        }

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
            ? $"A full bottle — {GameItems.BottledWaterMaxSips} sips. Each sip is a small relief."
            : remaining == 1
                ? "One sip left. Drink it before the bottle is empty."
                : $"{remaining} sips left. Each sip is a small relief.";
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
            ? $"A sealed can — {GameItems.CannedSoupMaxServings} servings. Each serving takes the edge off hunger."
            : remaining == 1
                ? "One serving left. Eat it before you toss the can."
                : $"{remaining} servings left. Each serving takes the edge off hunger.";
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
        || _phase is Phase.WarehouseTruck or Phase.WarehouseAmbush or Phase.WarehouseAftermath
        or Phase.WarehouseInterior or Phase.GasStation;

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
        _warehouseAftermathBackground = LoadTextureOrFallback("warehouse-14-aftermath.png", _warehouseAmbushBackground);
        _warehouseClosedDoorTexture = EmbeddedTextureLoader.Load(WarehouseAftermathHotspots.ClosedDoorImageFile);
        _warehouseInteriorBackground = LoadTextureOrFallback("warehouse-14-interior.png", _warehouseAftermathBackground);
        _gasStationBackground = LoadTextureOrFallback("gas-station.png", _commercialDistrictBackground);
        _cafeOwnerPortraitTexture = EmbeddedTextureLoader.Load("cafe-owner-portrait.png");
        _tentBackground      = EmbeddedTextureLoader.Load("tent-interior.png");
        _trashBagTentTexture = EmbeddedTextureLoader.Load("trash-bag-tent.png");
        _titleLogoTexture    = EmbeddedTextureLoader.Load("conscript-title.png");
        _foldedPaperNoteTexture = EmbeddedTextureLoader.Load(GameItems.FoldedPaperNoteFile);
        _crateNoteTexture = EmbeddedTextureLoader.Load(GameItems.CrateNoteFile);
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
        UnloadTextureIfLoaded(ref _warehouseAftermathBackground);
        UnloadTextureIfLoaded(ref _warehouseClosedDoorTexture);
        UnloadTextureIfLoaded(ref _warehouseInteriorBackground);
        UnloadTextureIfLoaded(ref _gasStationBackground);
        UnloadTextureIfLoaded(ref _cafeOwnerPortraitTexture);
        UnloadTextureIfLoaded(ref _tentBackground);
        UnloadTextureIfLoaded(ref _trashBagTentTexture);
        UnloadTextureIfLoaded(ref _titleLogoTexture);
        UnloadTextureIfLoaded(ref _foldedPaperNoteTexture);
        UnloadTextureIfLoaded(ref _crateNoteTexture);
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
            if (_showCrateLootMenu)
            {
                CloseCrateLootMenu();
                return;
            }
            if (_showBodyLootMenu)
            {
                CloseBodyLootMenu();
                return;
            }
            if (_warehouseKeypad.IsOpen)
            {
                CloseWarehouseLock();
                return;
            }
            if (_foldedPaperReader.IsOpen)
            {
                CloseFoldedPaperReader();
                return;
            }
            if (_gasGaugeViewer.IsOpen)
            {
                CloseGasGaugeViewer();
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
            if (_showWarehouseCrateDialog)
            {
                CloseWarehouseCrateDialog();
                return;
            }
            if (_itemUseActive)
            {
                StopItemUseMode();
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
            if (_sceneAreaSelect.IsActive)
            {
                _sceneAreaSelect.Close();
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


        if (_showStoreBuyMenu)
        {
            int shopCount = ShopCatalogs.GetEntries(_shopBuyKind).Length;
            if (shopCount > 0)
            {
                if (InputManager.IsVerticalNavUpPressed())
                {
                    _storeBuyHighlightedIndex = (_storeBuyHighlightedIndex - 1 + shopCount) % shopCount;
                    _storeBuyDetailIndex = _storeBuyHighlightedIndex;
                }

                if (InputManager.IsVerticalNavDownPressed())
                {
                    _storeBuyHighlightedIndex = (_storeBuyHighlightedIndex + 1) % shopCount;
                    _storeBuyDetailIndex = _storeBuyHighlightedIndex;
                }
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

        if (_showCrateLootMenu)
        {
            RefreshCrateLootVisibleList();

            if (InputManager.IsVerticalNavUpPressed())
            {
                if (_crateLootVisibleCount > 0)
                {
                    _crateLootHighlightedIndex = (_crateLootHighlightedIndex - 1 + _crateLootVisibleCount) % _crateLootVisibleCount;
                    _crateLootDetailIndex = _crateLootHighlightedIndex;
                }
            }
            if (InputManager.IsVerticalNavDownPressed())
            {
                if (_crateLootVisibleCount > 0)
                {
                    _crateLootHighlightedIndex = (_crateLootHighlightedIndex + 1) % _crateLootVisibleCount;
                    _crateLootDetailIndex = _crateLootHighlightedIndex;
                }
            }
        }

        if (_showBodyLootMenu)
        {
            RefreshBodyLootVisibleList();

            if (InputManager.IsVerticalNavUpPressed())
            {
                if (_bodyLootVisibleCount > 0)
                {
                    _bodyLootHighlightedIndex = (_bodyLootHighlightedIndex - 1 + _bodyLootVisibleCount) % _bodyLootVisibleCount;
                    _bodyLootDetailIndex = _bodyLootHighlightedIndex;
                }
            }
            if (InputManager.IsVerticalNavDownPressed())
            {
                if (_bodyLootVisibleCount > 0)
                {
                    _bodyLootHighlightedIndex = (_bodyLootHighlightedIndex + 1) % _bodyLootVisibleCount;
                    _bodyLootDetailIndex = _bodyLootHighlightedIndex;
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
            else if (_showItemDialog)
            {
                if (TryPerformItemPanelPrimaryAction())
                    return;
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
            else if (_showWarehouseCrateDialog)
            {
                CloseWarehouseCrateDialog();
            }
            else if (_showStoreBuyMenu)
            {
                if (_storeBuyPurchaseHovered && _storeBuyDetailIndex >= 0)
                    TryBuyShopItem(_storeBuyDetailIndex);
                else if (_storeBuyDetailIndex == _storeBuyHighlightedIndex && _storeBuyDetailIndex >= 0)
                    TryBuyShopItem(_storeBuyDetailIndex);
                else
                    _storeBuyDetailIndex = _storeBuyHighlightedIndex;
            }
            else if (_showGloveBoxMenu)
            {
                int catalogIndex = GetGloveBoxCatalogIndexFromVisibleIndex(_gloveBoxDetailIndex);
                int highlightedCatalogIndex = GetGloveBoxCatalogIndexFromVisibleIndex(_gloveBoxHighlightedIndex);

                if (_gloveBoxTakeAllHovered)
                    TryTakeAllGloveBoxItems();
                else if (_gloveBoxPickupHovered && catalogIndex >= 0)
                    TryTakeGloveBoxItem(catalogIndex);
                else if (_gloveBoxDetailIndex == _gloveBoxHighlightedIndex && highlightedCatalogIndex >= 0)
                    TryTakeGloveBoxItem(highlightedCatalogIndex);
                else
                    _gloveBoxDetailIndex = _gloveBoxHighlightedIndex;
            }
            else if (_showCrateLootMenu)
            {
                int catalogIndex = GetCrateLootCatalogIndexFromVisibleIndex(_crateLootDetailIndex);
                int highlightedCatalogIndex = GetCrateLootCatalogIndexFromVisibleIndex(_crateLootHighlightedIndex);

                if (_crateLootTakeAllHovered)
                    TryTakeAllCrateLootItems();
                else if (_crateLootPickupHovered && catalogIndex >= 0)
                    TryTakeCrateLootItem(catalogIndex);
                else if (_crateLootDetailIndex == _crateLootHighlightedIndex && highlightedCatalogIndex >= 0)
                    TryTakeCrateLootItem(highlightedCatalogIndex);
                else
                    _crateLootDetailIndex = _crateLootHighlightedIndex;
            }
            else if (_showBodyLootMenu)
            {
                int itemIndex = GetBodyLootItemIndexFromVisibleIndex(_bodyLootDetailIndex);
                int highlightedItemIndex = GetBodyLootItemIndexFromVisibleIndex(_bodyLootHighlightedIndex);

                if (_bodyLootTakeAllHovered)
                    TryTakeAllBodyLootItems();
                else if (_bodyLootPickupHovered && itemIndex >= 0)
                    TryTakeBodyLootItem(itemIndex);
                else if (_bodyLootDetailIndex == _bodyLootHighlightedIndex && highlightedItemIndex >= 0)
                    TryTakeBodyLootItem(highlightedItemIndex);
                else
                    _bodyLootDetailIndex = _bodyLootHighlightedIndex;
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
        UpdateHistoryButtonsLayout();
        _undoHovered = CanUndoAction() && Raylib.CheckCollisionPointRec(mouse, _undoButtonRect);
        _redoHovered = CanRedoAction() && Raylib.CheckCollisionPointRec(mouse, _redoButtonRect);
        _restartHovered = Raylib.CheckCollisionPointRec(mouse, _restartButtonRect);
        _debugStartHovered = Raylib.CheckCollisionPointRec(mouse, _debugStartButtonRect);
        _areaSelectHovered = Raylib.CheckCollisionPointRec(mouse, _areaSelectButtonRect);
        _controllerHovered = Raylib.CheckCollisionPointRec(mouse, _controllerButtonRect);
        _copyRoomIdHovered = Raylib.CheckCollisionPointRec(mouse, _copyRoomIdButtonRect);
        if (leftClicked && _undoHovered)
        {
            UndoLastAction();
            return;
        }
        if (leftClicked && _redoHovered)
        {
            RedoLastAction();
            return;
        }
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
        if (leftClicked && _areaSelectHovered)
        {
            if (_sceneAreaSelect.IsActive)
                _sceneAreaSelect.Close();
            else if (CanUseSceneAreaSelect())
                _sceneAreaSelect.Open();
            else
            {
                _actionMessage = "No scene background available here.";
                _actionMessageTimer = 2.5f;
            }

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

        if (leftClicked && _copyRoomIdHovered)
        {
            string roomId = _phase.ToString();
            Raylib.SetClipboardText(roomId);
            _actionMessage = $"{roomId} copied to clipboard";
            _actionMessageTimer = 2.5f;
            return;
        }

        if (_sceneAreaSelect.IsActive)
        {
            GetCinematicArtBounds(out int artX, out int artY, out int artW, out int artH);
            var artBounds = new Rectangle(artX, artY, artW, artH);
            bool leftReleased = Raylib.IsMouseButtonReleased(MouseButton.MOUSE_LEFT_BUTTON);

            SceneAreaSelection? selection = _sceneAreaSelect.Update(
                mouse,
                artBounds,
                leftClicked,
                leftReleased);

            if (selection.HasValue)
            {
                Raylib.SetClipboardText(selection.Value.ClipboardText);
                _actionMessage = selection.Value.DisplayMessage;
                _actionMessageTimer = 4.5f;
            }
            else if (_sceneAreaSelect.SelectionTooSmall)
            {
                _actionMessage = "Selection too small — drag a larger region.";
                _actionMessageTimer = 2.5f;
            }

            return;
        }

        if (_warehouseKeypad.IsOpen)
        {
            bool wasUnlocked = _warehouseKeypad.IsUnlocked;
            _warehouseKeypad.Update(dt, mouse, leftClicked);
            if (!wasUnlocked && _warehouseKeypad.IsUnlocked)
            {
                RecordHistorySnapshot();
                _actionMessage = "The roll-up door rattles and grinds open.";
                _actionMessageTimer = 2.8f;
            }

            return;
        }

        if (_foldedPaperReader.IsOpen)
        {
            _foldedPaperReader.Update(mouse, leftClicked);
            return;
        }

        if (_gasGaugeViewer.IsOpen)
        {
            _gasGaugeViewer.Update(mouse, leftClicked);
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
                !Raylib.CheckCollisionPointRec(mouse, _undoButtonRect) &&
                !Raylib.CheckCollisionPointRec(mouse, _redoButtonRect) &&
                !Raylib.CheckCollisionPointRec(mouse, _restartButtonRect) &&
                !Raylib.CheckCollisionPointRec(mouse, _debugStartButtonRect) &&
                !Raylib.CheckCollisionPointRec(mouse, _areaSelectButtonRect) &&
                !Raylib.CheckCollisionPointRec(mouse, _controllerButtonRect) &&
                !Raylib.CheckCollisionPointRec(mouse, _copyRoomIdButtonRect))
            {
                CloseControllerDebug();
                return;
            }
            return;
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

        // === Warehouse crate dialog (modal) ===
        if (_showWarehouseCrateDialog)
        {
            _warehouseCrateCloseHovered = Raylib.CheckCollisionPointRec(mouse, _warehouseCrateCloseRect);

            if (leftClicked && _warehouseCrateCloseHovered)
            {
                CloseWarehouseCrateDialog();
                return;
            }

            if (leftClicked && !Raylib.CheckCollisionPointRec(mouse, _warehouseCratePanelRect))
            {
                CloseWarehouseCrateDialog();
                return;
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

        // === Item panel (left sidebar) ===
        if (_showItemDialog && AllowsSidebarAndSceneInput())
        {
            UpdateItemPanelHover(mouse);

            if (leftClicked && _dialogActionHovered)
            {
                if (IsDroppedItemDialog)
                    TryPickupDroppedItem();
                else
                    TryPerformItemPanelPrimaryAction();
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
            _gloveBoxTakeAllHovered = Raylib.CheckCollisionPointRec(mouse, _gloveBoxTakeAllRect);
            _gloveBoxPickupHovered = Raylib.CheckCollisionPointRec(mouse, _gloveBoxPickupRect);
        }
        else
        {
            _gloveBoxCloseHovered = false;
            _gloveBoxTakeAllHovered = false;
            _gloveBoxPickupHovered = false;
        }

        if (_showCrateLootMenu)
        {
            _crateLootCloseHovered = Raylib.CheckCollisionPointRec(mouse, _crateLootCloseRect);
            _crateLootTakeAllHovered = Raylib.CheckCollisionPointRec(mouse, _crateLootTakeAllRect);
            _crateLootPickupHovered = Raylib.CheckCollisionPointRec(mouse, _crateLootPickupRect);
        }
        else
        {
            _crateLootCloseHovered = false;
            _crateLootTakeAllHovered = false;
            _crateLootPickupHovered = false;
        }

        if (_showBodyLootMenu)
        {
            _bodyLootCloseHovered = Raylib.CheckCollisionPointRec(mouse, _bodyLootCloseRect);
            _bodyLootTakeAllHovered = Raylib.CheckCollisionPointRec(mouse, _bodyLootTakeAllRect);
            _bodyLootPickupHovered = Raylib.CheckCollisionPointRec(mouse, _bodyLootPickupRect);
        }
        else
        {
            _bodyLootCloseHovered = false;
            _bodyLootTakeAllHovered = false;
            _bodyLootPickupHovered = false;
        }

        if (_itemUseActive)
        {
            UpdateItemUseMode(dt, leftClicked);
        }
        else if (AllowsSidebarAndSceneInput())
        {
            _buildSidebarButtonHovered = _buildSidebarButtonRect.Width > 0 &&
                Raylib.CheckCollisionPointRec(mouse, _buildSidebarButtonRect);
            _huntSidebarButtonHovered = _huntSidebarButtonRect.Width > 0 &&
                Raylib.CheckCollisionPointRec(mouse, _huntSidebarButtonRect);
            _forageSidebarButtonHovered = _forageSidebarButtonRect.Width > 0 &&
                Raylib.CheckCollisionPointRec(mouse, _forageSidebarButtonRect);
            _quitSidebarButtonHovered = _quitSidebarButtonRect.Width > 0 &&
                Raylib.CheckCollisionPointRec(mouse, _quitSidebarButtonRect);

            if (_showItemDialog)
                UpdateItemPanelHover(mouse);

            if (leftClicked && _buildSidebarButtonHovered)
            {
                OpenBuildDialog();
                return;
            }

            if (leftClicked && _huntSidebarButtonHovered)
            {
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

            if (GamePhase.IsInTruckCab(_phase))
            {
                if (!GloveCompartmentHasRemainingLoot())
                    _gloveCompartmentClickRect = default;
                else
                {
                    GetCinematicArtBounds(out int ax, out int ay, out int aw, out int ah);
                    _gloveCompartmentClickRect = ComputeTruckGloveBoxClickRect(_phase, ax, ay, aw, ah);
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

                if (_phase is Phase.DeliveryTruck or Phase.WarehouseTruck)
                {
                    GetCinematicArtBounds(out int gaugeArtX, out int gaugeArtY, out int gaugeArtW, out int gaugeArtH);
                    var gaugeArtBounds = new Rectangle(gaugeArtX, gaugeArtY, gaugeArtW, gaugeArtH);
                    _gasGaugeHotspotHovered = false;
                    _gasGaugeClickRect = ComputeTruckGasGaugeClickRect(_phase, gaugeArtBounds);

                    if (Raylib.CheckCollisionPointRec(mouse, _gasGaugeClickRect))
                    {
                        _gasGaugeHotspotHovered = true;
                        if (leftClicked)
                        {
                            OpenGasGaugeViewer();
                            return;
                        }
                    }
                }
                else
                {
                    _gasGaugeHotspotHovered = false;
                    _gasGaugeClickRect = default;
                }
            }
            else
            {
                _gloveCompartmentClickRect = default;
                _gloveCompartmentHovered = false;
                _gasGaugeHotspotHovered = false;
                _gasGaugeClickRect = default;
            }

            if (_phase == Phase.WarehouseAftermath)
            {
                GetCinematicArtBounds(out int bodyArtX, out int bodyArtY, out int bodyArtW, out int bodyArtH);
                var bodyArtBounds = new Rectangle(bodyArtX, bodyArtY, bodyArtW, bodyArtH);
                _hoveredBodyIndex = -1;

                for (int i = 0; i < WarehouseBodyLootCatalog.BodyCount; i++)
                {
                    if (!BodyHasRemainingLoot(i))
                    {
                        _bodyClickRects[i] = default;
                        continue;
                    }

                    var body = WarehouseBodyLootCatalog.Bodies[i];
                    _bodyClickRects[i] = SceneRegion.ToScreenRect(
                        body.RegionX,
                        body.RegionY,
                        body.RegionW,
                        body.RegionH,
                        bodyArtBounds);

                    if (Raylib.CheckCollisionPointRec(mouse, _bodyClickRects[i]))
                    {
                        _hoveredBodyIndex = i;
                        if (leftClicked)
                        {
                            OpenBodyLootMenu(i);
                            return;
                        }

                        break;
                    }
                }

                _warehouseLockHotspotHovered = false;
                float lockW = WarehouseAftermathHotspots.LockX2 - WarehouseAftermathHotspots.LockX1;
                float lockH = WarehouseAftermathHotspots.LockY2 - WarehouseAftermathHotspots.LockY1;
                _warehouseLockClickRect = SceneRegion.ToScreenRect(
                    WarehouseAftermathHotspots.LockX1,
                    WarehouseAftermathHotspots.LockY1,
                    lockW,
                    lockH,
                    bodyArtBounds);

                if (Raylib.CheckCollisionPointRec(mouse, _warehouseLockClickRect))
                {
                    _warehouseLockHotspotHovered = true;
                    if (leftClicked)
                    {
                        OpenWarehouseLock();
                        return;
                    }
                }

                _warehouseDoorHotspotHovered = false;
                float doorW = WarehouseAftermathHotspots.DoorX2 - WarehouseAftermathHotspots.DoorX1;
                float doorH = WarehouseAftermathHotspots.DoorY2 - WarehouseAftermathHotspots.DoorY1;
                _warehouseDoorClickRect = SceneRegion.ToScreenRect(
                    WarehouseAftermathHotspots.DoorX1,
                    WarehouseAftermathHotspots.DoorY1,
                    doorW,
                    doorH,
                    bodyArtBounds);

                if (Raylib.CheckCollisionPointRec(mouse, _warehouseDoorClickRect))
                {
                    _warehouseDoorHotspotHovered = true;
                    if (leftClicked)
                    {
                        OpenWarehouseDoor();
                        return;
                    }
                }

                _warehouseTruckHotspotHovered = false;
                float truckW = WarehouseAftermathHotspots.TruckX2 - WarehouseAftermathHotspots.TruckX1;
                float truckH = WarehouseAftermathHotspots.TruckY2 - WarehouseAftermathHotspots.TruckY1;
                _warehouseTruckClickRect = SceneRegion.ToScreenRect(
                    WarehouseAftermathHotspots.TruckX1,
                    WarehouseAftermathHotspots.TruckY1,
                    truckW,
                    truckH,
                    bodyArtBounds);

                if (Raylib.CheckCollisionPointRec(mouse, _warehouseTruckClickRect))
                {
                    _warehouseTruckHotspotHovered = true;
                    if (leftClicked)
                    {
                        RecordHistorySnapshot();
                        EnterWarehouseTruck();
                        return;
                    }
                }
            }
            else
            {
                _hoveredBodyIndex = -1;
                Array.Clear(_bodyClickRects);
                _warehouseLockHotspotHovered = false;
                _warehouseLockClickRect = default;
                _warehouseDoorHotspotHovered = false;
                _warehouseDoorClickRect = default;
                _warehouseTruckHotspotHovered = false;
                _warehouseTruckClickRect = default;
            }

            if (_phase == Phase.Opening)
            {
                GetCinematicArtBounds(out int openingArtX, out int openingArtY, out int openingArtW, out int openingArtH);
                var openingArtBounds = new Rectangle(openingArtX, openingArtY, openingArtW, openingArtH);
                _openingWindowHotspotHovered = false;
                float windowW = OpeningHotspots.WindowX2 - OpeningHotspots.WindowX1;
                float windowH = OpeningHotspots.WindowY2 - OpeningHotspots.WindowY1;
                _openingWindowClickRect = SceneRegion.ToScreenRect(
                    OpeningHotspots.WindowX1,
                    OpeningHotspots.WindowY1,
                    windowW,
                    windowH,
                    openingArtBounds);

                if (Raylib.CheckCollisionPointRec(mouse, _openingWindowClickRect))
                {
                    _openingWindowHotspotHovered = true;
                    if (leftClicked)
                    {
                        TryPerformOpeningWindowClick();
                        return;
                    }
                }
            }
            else
            {
                _openingWindowHotspotHovered = false;
                _openingWindowClickRect = default;
            }

            if (_phase == Phase.WarehouseInterior)
            {
                GetCinematicArtBounds(out int interiorArtX, out int interiorArtY, out int interiorArtW, out int interiorArtH);
                var interiorArtBounds = new Rectangle(interiorArtX, interiorArtY, interiorArtW, interiorArtH);

                _warehouseInteriorExitHotspotHovered = false;
                float exitW = WarehouseInteriorHotspots.ExitX2 - WarehouseInteriorHotspots.ExitX1;
                float exitH = WarehouseInteriorHotspots.ExitY2 - WarehouseInteriorHotspots.ExitY1;
                _warehouseInteriorExitClickRect = SceneRegion.ToScreenRect(
                    WarehouseInteriorHotspots.ExitX1,
                    WarehouseInteriorHotspots.ExitY1,
                    exitW,
                    exitH,
                    interiorArtBounds);

                if (Raylib.CheckCollisionPointRec(mouse, _warehouseInteriorExitClickRect))
                {
                    _warehouseInteriorExitHotspotHovered = true;
                    if (leftClicked)
                    {
                        RecordHistorySnapshot();
                        ExitWarehouseToLoadingBay();
                        return;
                    }
                }

                _warehouseCrateHotspotHovered = false;
                float crateW = WarehouseInteriorHotspots.CrateX2 - WarehouseInteriorHotspots.CrateX1;
                float crateH = WarehouseInteriorHotspots.CrateY2 - WarehouseInteriorHotspots.CrateY1;
                _warehouseCrateClickRect = SceneRegion.ToScreenRect(
                    WarehouseInteriorHotspots.CrateX1,
                    WarehouseInteriorHotspots.CrateY1,
                    crateW,
                    crateH,
                    interiorArtBounds);

                if (Raylib.CheckCollisionPointRec(mouse, _warehouseCrateClickRect))
                {
                    _warehouseCrateHotspotHovered = true;
                    if (leftClicked)
                    {
                        if (_warehouseCrateOpened && CrateHasRemainingLoot())
                            OpenCrateLootMenu();
                        else
                            OpenWarehouseCrateDialog();
                        return;
                    }
                }
            }
            else
            {
                _warehouseInteriorExitHotspotHovered = false;
                _warehouseInteriorExitClickRect = default;
                _warehouseCrateHotspotHovered = false;
                _warehouseCrateClickRect = default;
            }

            if (_phase == Phase.Cafe)
            {
                GetCinematicArtBounds(out int cafeArtX, out int cafeArtY, out int cafeArtW, out int cafeArtH);
                var cafeArtBounds = new Rectangle(cafeArtX, cafeArtY, cafeArtW, cafeArtH);
                _cafeBorisHotspotHovered = false;
                UpdateCafeBorisClickRect(cafeArtBounds);

                if (Raylib.CheckCollisionPointRec(mouse, _cafeBorisClickRect))
                {
                    _cafeBorisHotspotHovered = true;
                    if (leftClicked)
                    {
                        OpenCafeOwnerDialog();
                        return;
                    }
                }
            }
            else
            {
                _cafeBorisHotspotHovered = false;
                _cafeBorisClickRect = default;
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

            // === Backpack item click (shows detail in right panel) ===
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
            string[] shopEntries = ShopCatalogs.GetEntries(_shopBuyKind);
            for (int i = 0; i < shopEntries.Length; i++)
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
                TryBuyShopItem(_storeBuyDetailIndex);
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

            if (leftClicked && _gloveBoxTakeAllHovered)
            {
                TryTakeAllGloveBoxItems();
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

        if (_showCrateLootMenu)
        {
            RefreshCrateLootVisibleList();

            for (int i = 0; i < _crateLootVisibleCount; i++)
            {
                if (Raylib.CheckCollisionPointRec(mouse, _crateLootItemRects[i]))
                {
                    _crateLootHighlightedIndex = i;
                    if (leftClicked)
                        _crateLootDetailIndex = i;
                    break;
                }
            }

            if (leftClicked && _crateLootDetailIndex >= 0 &&
                (_crateLootPickupHovered || Raylib.CheckCollisionPointRec(mouse, _crateLootPickupRect)))
            {
                int catalogIndex = GetCrateLootCatalogIndexFromVisibleIndex(_crateLootDetailIndex);
                if (catalogIndex >= 0)
                    TryTakeCrateLootItem(catalogIndex);
                return;
            }

            if (leftClicked && _crateLootTakeAllHovered)
            {
                TryTakeAllCrateLootItems();
                return;
            }

            if (leftClicked && Raylib.CheckCollisionPointRec(mouse, _crateLootCloseRect))
            {
                CloseCrateLootMenu();
                return;
            }

            if (leftClicked && !Raylib.CheckCollisionPointRec(mouse, _crateLootPanelRect))
            {
                CloseCrateLootMenu();
                return;
            }
        }

        if (_showBodyLootMenu)
        {
            RefreshBodyLootVisibleList();

            for (int i = 0; i < _bodyLootVisibleCount; i++)
            {
                if (Raylib.CheckCollisionPointRec(mouse, _bodyLootItemRects[i]))
                {
                    _bodyLootHighlightedIndex = i;
                    if (leftClicked)
                        _bodyLootDetailIndex = i;
                    break;
                }
            }

            if (leftClicked && _bodyLootDetailIndex >= 0 &&
                (_bodyLootPickupHovered || Raylib.CheckCollisionPointRec(mouse, _bodyLootPickupRect)))
            {
                int itemIndex = GetBodyLootItemIndexFromVisibleIndex(_bodyLootDetailIndex);
                if (itemIndex >= 0)
                    TryTakeBodyLootItem(itemIndex);
                return;
            }

            if (leftClicked && _bodyLootTakeAllHovered)
            {
                TryTakeAllBodyLootItems();
                return;
            }

            if (leftClicked && Raylib.CheckCollisionPointRec(mouse, _bodyLootCloseRect))
            {
                CloseBodyLootMenu();
                return;
            }

            if (leftClicked && !Raylib.CheckCollisionPointRec(mouse, _bodyLootPanelRect))
            {
                CloseBodyLootMenu();
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


        if (_showStoreBuyMenu)
            GameStatMath.TickTimedMessage(ref _storeBuyFeedbackTimer, ref _storeBuyFeedback, dt);

        if (_showGloveBoxMenu)
            GameStatMath.TickTimedMessage(ref _gloveBoxFeedbackTimer, ref _gloveBoxFeedback, dt);

        if (_showCrateLootMenu)
            GameStatMath.TickTimedMessage(ref _crateLootFeedbackTimer, ref _crateLootFeedback, dt);

        if (_showBodyLootMenu)
            GameStatMath.TickTimedMessage(ref _bodyLootFeedbackTimer, ref _bodyLootFeedback, dt);

        if (_showBuildDialog)
            GameStatMath.TickTimedMessage(ref _buildFeedbackTimer, ref _buildFeedback, dt);

        // === Update mouse cursor to indicate clickable elements ===
        bool overClickable = false;

        // Top-right utility buttons (always available)
        if (Raylib.CheckCollisionPointRec(mouse, _undoButtonRect) ||
            Raylib.CheckCollisionPointRec(mouse, _redoButtonRect) ||
            Raylib.CheckCollisionPointRec(mouse, _restartButtonRect) ||
            Raylib.CheckCollisionPointRec(mouse, _debugStartButtonRect) ||
            Raylib.CheckCollisionPointRec(mouse, _areaSelectButtonRect) ||
            Raylib.CheckCollisionPointRec(mouse, _controllerButtonRect) ||
            Raylib.CheckCollisionPointRec(mouse, _copyRoomIdButtonRect) ||
            (_showControllerDebug && (
                Raylib.CheckCollisionPointRec(mouse, _controllerDebugCloseRect) ||
                Raylib.CheckCollisionPointRec(mouse, _controllerDebugPrevRect) ||
                Raylib.CheckCollisionPointRec(mouse, _controllerDebugNextRect) ||
                _controllerDebugTabHovered.Any(h => h))))
            overClickable = true;

        if (AllowsSidebarAndSceneInput())
        {
            if (_buildSidebarButtonHovered ||
                _huntSidebarButtonHovered || _forageSidebarButtonHovered || _quitSidebarButtonHovered)
                overClickable = true;

            if (_trashBagTentHovered)
                overClickable = true;

            if (_gloveCompartmentHovered)
                overClickable = true;

            if (_gasGaugeHotspotHovered)
                overClickable = true;

            if (_hoveredBodyIndex >= 0)
                overClickable = true;

            if (_warehouseLockHotspotHovered)
                overClickable = true;

            if (_warehouseDoorHotspotHovered)
                overClickable = true;

            if (_warehouseTruckHotspotHovered)
                overClickable = true;

            if (_warehouseCrateHotspotHovered)
                overClickable = true;

            if (_warehouseInteriorExitHotspotHovered)
                overClickable = true;

            if (_openingWindowHotspotHovered)
                overClickable = true;

            if (_cafeBorisHotspotHovered)
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

        if (_itemUseActive && _itemUsePointerInsideBounds && _warehouseCrateHotspotHovered)
            overClickable = true;

        if (_itemUseActive && _itemUsePointerInsideBounds && _cafeBorisHotspotHovered)
            overClickable = true;

        if (_showItemDialog && AllowsSidebarAndSceneInput())
        {
            if (_dialogActionHovered || _dialogSecondaryActionHovered || _dialogDropHovered)
                overClickable = true;
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
            if (_gloveBoxCloseHovered || _gloveBoxTakeAllHovered || _gloveBoxPickupHovered ||
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

        if (_showCrateLootMenu)
        {
            if (_crateLootCloseHovered || _crateLootTakeAllHovered || _crateLootPickupHovered ||
                Raylib.CheckCollisionPointRec(mouse, _crateLootPanelRect) ||
                !Raylib.CheckCollisionPointRec(mouse, _crateLootPanelRect))
                overClickable = true;

            for (int i = 0; i < _crateLootItemRects.Length; i++)
            {
                if (Raylib.CheckCollisionPointRec(mouse, _crateLootItemRects[i]))
                {
                    overClickable = true;
                    break;
                }
            }
        }

        if (_showBodyLootMenu)
        {
            if (_bodyLootCloseHovered || _bodyLootTakeAllHovered || _bodyLootPickupHovered ||
                Raylib.CheckCollisionPointRec(mouse, _bodyLootPanelRect) ||
                !Raylib.CheckCollisionPointRec(mouse, _bodyLootPanelRect))
                overClickable = true;

            for (int i = 0; i < _bodyLootItemRects.Length; i++)
            {
                if (Raylib.CheckCollisionPointRec(mouse, _bodyLootItemRects[i]))
                {
                    overClickable = true;
                    break;
                }
            }
        }

        if (_warehouseKeypad.IsOpen)
            overClickable = true;

        if (_foldedPaperReader.IsOpen)
            overClickable = true;

        if (_gasGaugeViewer.IsOpen)
            overClickable = true;

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

        if (_showWarehouseCrateDialog)
        {
            if (_warehouseCrateCloseHovered ||
                !Raylib.CheckCollisionPointRec(mouse, _warehouseCratePanelRect))
                overClickable = true;
        }

        if (_showQuitConfirm &&
            (_quitConfirmYesHovered || _quitConfirmNoHovered))
            overClickable = true;

        Raylib.SetMouseCursor(_sceneAreaSelect.IsActive
            ? MouseCursor.MOUSE_CURSOR_CROSSHAIR
            : overClickable
                ? MouseCursor.MOUSE_CURSOR_POINTING_HAND
                : MouseCursor.MOUSE_CURSOR_DEFAULT);
    }

    // --- Choice handlers ---
    private void PerformChoice(int index)
    {
        RecordHistorySnapshot();
        _historyRecordSuppression++;

        try
        {
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

                case Phase.WarehouseAftermath:
                    HandleWarehouseAftermathChoice(index);
                    break;

                case Phase.GasStation:
                    HandleGasStationChoice(index);
                    break;

                case Phase.WarehouseInterior:
                    HandleWarehouseInteriorChoice(index);
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
        finally
        {
            _historyRecordSuppression--;
        }
    }

    private void TryPerformOpeningWindowClick()
    {
        for (int i = 0; i < _choices.Length; i++)
        {
            if (_choices[i] == ChoiceFleeOutWindow)
            {
                PerformChoice(i);
                return;
            }
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
                EnterForestArea(Phase.ForestStream);
                break;

            case ChoiceGoBackToTown:
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
                EnterForestArea(Phase.Forest);
                break;

            case ChoiceBackToForestEntry:
                _actionMessage = "You work your way back toward the edge of town.";
                _actionMessageTimer = 2.5f;
                AdvanceTime();
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
                EnterPhase(Phase.IndustrialDistrict);
                return;

            case ChoiceCommercialDistrict:
                _actionMessage = "You slip east toward the lit shopfronts.";
                _actionMessageTimer = 2.0f;
                AdvanceTime();
                EnterPhase(Phase.CommercialDistrict);
                return;

            case ChoiceConvenienceStore:
                _phaseBeforeStore = Phase.Town;
                _actionMessage = "You push through the heavy glass door into the harsh light.";
                _actionMessageTimer = 1.8f;
                EnterPhase(Phase.Store);
                return;

            case ChoiceBackToCourtyard:
                _actionMessage = "You duck back through the fence into the courtyard behind your block.";
                _actionMessageTimer = 2.0f;
                AdvanceTime();
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
                _actionMessage = "You push through the frosted glass door into the warmth.";
                _actionMessageTimer = 1.8f;
                EnterPhase(Phase.Cafe);
                return;

            case ChoiceBackToTownCenter:
                _actionMessage = "You leave the warehouses behind and return to the central streets.";
                _actionMessageTimer = 2.0f;
                AdvanceTime();
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
        _actionMessageTimer = ActionMessageDuration;
    }

    private void HandleCommercialDistrictChoice(int index)
    {
        if (index < 0 || index >= _choices.Length)
            return;

        switch (_choices[index])
        {
            case ChoiceHeadForForest:
                _actionMessage = "You slip south from the shopfronts and into the dark pines at the edge of town.";
                AdvanceTime();
                EnterPhase(Phase.ForestEntry);
                return;

            case ChoiceBackToTownCenter:
                _actionMessage = "You leave the shopfronts behind and return to the central streets.";
                _actionMessageTimer = 2.0f;
                AdvanceTime();
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

        AdvanceTime(TentSleepTimeSteps);
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
                _gasGaugeFuel = GasGaugeCatalog.EmptyFuel;
                AdvanceTime();
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
                EnterPhase(_warehouseAmbushersDead ? Phase.WarehouseAftermath : Phase.WarehouseAmbush);
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
            case ChoiceGetBackInTruck:
                _actionMessage = "You slide back into the cab and pull the door shut. The bratdvas haven't moved yet.";
                _actionMessageTimer = 2.1f;
                AdvanceTime();
                EnterPhase(Phase.WarehouseTruck);
                return;

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

    private void ThrowLitMolotovAtWarehouseAmbush(int? preferredSlot = null)
    {
        int slot = preferredSlot ?? FindBackpackSlotIndex(GameItems.LitMolotov);
        if (slot < 0 || slot >= _backpack.Length ||
            !string.Equals(_backpack[slot], GameItems.LitMolotov, StringComparison.OrdinalIgnoreCase))
            return;

        RemoveBackpackItemAtSlot(slot);
        _warehouseAmbushersDead = true;
        _actionMessage =
            "You hurl the bottle. It detonates like a gasoline bomb — a roiling fireball " +
            "that lifts the bratdvas off their feet. This wasn't vodka.";
        _actionMessageTimer = 3.4f;
        AdvanceTime();
        EnterPhase(Phase.WarehouseAftermath);
    }

    private void HandleWarehouseAftermathChoice(int index)
    {
        if (index < 0 || index >= _choices.Length)
            return;

        switch (_choices[index])
        {
            case ChoiceGetBackInTruck:
                EnterWarehouseTruck();
                return;

            case ChoiceWalkToGasStation:
                _actionMessage = "You cut across the wet yards toward the sodium glow of a gas station.";
                _actionMessageTimer = 2.4f;
                AdvanceTime();
                EnterPhase(Phase.GasStation);
                return;

            case ChoiceWait:
                PerformIdle();
                return;
        }

        AdvanceTime();
        _actionMessageTimer = ActionMessageDuration;
    }

    private void HandleGasStationChoice(int index)
    {
        if (index < 0 || index >= _choices.Length)
            return;

        switch (_choices[index])
        {
            case ChoiceBrowseKiosk:
                OpenGasStationBuyMenu();
                return;

            case ChoiceBackToLoadingBay:
                _actionMessage = "You trudge back through the rain toward the scorched loading bay.";
                _actionMessageTimer = 2.4f;
                AdvanceTime();
                EnterPhase(Phase.WarehouseAftermath);
                return;

            case ChoiceWait:
                PerformIdle();
                return;
        }

        AdvanceTime();
        _actionMessageTimer = ActionMessageDuration;
    }

    private void HandleWarehouseInteriorChoice(int index)
    {
        if (index < 0 || index >= _choices.Length)
            return;

        switch (_choices[index])
        {
            case ChoiceBackToLoadingBay:
                ExitWarehouseToLoadingBay();
                return;

            case ChoiceWait:
                PerformIdle();
                return;
        }

        AdvanceTime();
        _actionMessageTimer = ActionMessageDuration;
    }

    private void EnterWarehouseTruck()
    {
        if (_phase != Phase.WarehouseAftermath)
            return;

        _actionMessage = "You slide back into the cab, coughing smoke. The yard is quiet except for the rain.";
        _actionMessageTimer = 2.1f;
        AdvanceTime();
        EnterPhase(Phase.WarehouseTruck);
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
        if (!GamePhase.IsInTruckCab(_phase) || !GloveCompartmentHasRemainingLoot())
            return;

        _showGloveBoxMenu = true;
        _gloveBoxHighlightedIndex = 0;
        _gloveBoxDetailIndex = -1;
        _gloveBoxFeedback = "";
        _gloveBoxFeedbackTimer = 0f;
        _gloveBoxCloseHovered = false;
        _gloveBoxTakeAllHovered = false;
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
        _gloveBoxTakeAllHovered = false;
        _gloveBoxPickupHovered = false;
    }

    private bool CanTakeGloveBoxItem(int index)
    {
        if (index < 0 || index >= GloveCompartmentCatalog.EntryCount || _gloveBoxLootTaken[index])
            return false;

        var entry = GloveCompartmentCatalog.Entries[index];
        return _backpack.Any(s => string.IsNullOrEmpty(s));
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

        if (!TryAddToBackpack(entry.Name))
        {
            _gloveBoxFeedback = "Backpack is full.";
            _gloveBoxFeedbackTimer = 1.6f;
            return;
        }

        RecordHistorySnapshot();
        _gloveBoxLootTaken[index] = true;
        _gloveBoxFeedback = $"Took {entry.Name}";
        _gloveBoxFeedbackTimer = 1.2f;

        RefreshGloveBoxVisibleList();
        if (_gloveBoxVisibleCount <= 0)
            CloseGloveBoxMenu();
    }

    private void TryTakeAllGloveBoxItems()
    {
        if (_gloveBoxVisibleCount <= 0)
            return;

        RecordHistorySnapshot();
        int takenCount = 0;
        bool backpackFull = false;
        for (int i = 0; i < GloveCompartmentCatalog.EntryCount; i++)
        {
            if (_gloveBoxLootTaken[i])
                continue;

            var entry = GloveCompartmentCatalog.Entries[i];

            if (!TryAddToBackpack(entry.Name))
            {
                backpackFull = true;
                break;
            }

            _gloveBoxLootTaken[i] = true;
            takenCount++;
        }

        RefreshGloveBoxVisibleList();

        if (takenCount == 0)
        {
            _gloveBoxFeedback = "Backpack is full.";
            _gloveBoxFeedbackTimer = 1.6f;
            return;
        }

        _gloveBoxFeedback = backpackFull ? "Backpack full — took what you could." : "Took everything.";
        _gloveBoxFeedbackTimer = 1.4f;

        if (_gloveBoxVisibleCount <= 0)
            CloseGloveBoxMenu();
    }

    private void ResetCrateLoot() => Array.Clear(_crateLootTaken);

    private bool CrateHasRemainingLoot()
    {
        for (int i = 0; i < _crateLootTaken.Length; i++)
        {
            if (!_crateLootTaken[i])
                return true;
        }

        return false;
    }

    private void RefreshCrateLootVisibleList()
    {
        int count = 0;
        for (int i = 0; i < WarehouseCrateLootCatalog.EntryCount; i++)
        {
            if (!_crateLootTaken[i])
                _crateLootVisibleCatalogIndices[count++] = i;
        }

        _crateLootVisibleCount = count;

        if (_crateLootVisibleCount <= 0)
        {
            _crateLootHighlightedIndex = 0;
            _crateLootDetailIndex = -1;
            return;
        }

        _crateLootHighlightedIndex = Math.Clamp(_crateLootHighlightedIndex, 0, _crateLootVisibleCount - 1);
        if (_crateLootDetailIndex >= _crateLootVisibleCount)
            _crateLootDetailIndex = _crateLootHighlightedIndex;
    }

    private int GetCrateLootCatalogIndexFromVisibleIndex(int visibleIndex) =>
        visibleIndex < 0 || visibleIndex >= _crateLootVisibleCount
            ? -1
            : _crateLootVisibleCatalogIndices[visibleIndex];

    private void OpenCrateLootMenu()
    {
        if (_phase != Phase.WarehouseInterior || !_warehouseCrateOpened || !CrateHasRemainingLoot())
            return;

        _showCrateLootMenu = true;
        _crateLootHighlightedIndex = 0;
        _crateLootDetailIndex = -1;
        _crateLootFeedback = "";
        _crateLootFeedbackTimer = 0f;
        _crateLootCloseHovered = false;
        _crateLootTakeAllHovered = false;
        _crateLootPickupHovered = false;
        RefreshCrateLootVisibleList();
    }

    private void CloseCrateLootMenu()
    {
        _showCrateLootMenu = false;
        _crateLootDetailIndex = -1;
        _crateLootFeedback = "";
        _crateLootFeedbackTimer = 0f;
        _crateLootCloseHovered = false;
        _crateLootTakeAllHovered = false;
        _crateLootPickupHovered = false;
    }

    private bool CanTakeCrateLootItem(int index)
    {
        if (index < 0 || index >= WarehouseCrateLootCatalog.EntryCount || _crateLootTaken[index])
            return false;

        return _backpack.Any(s => string.IsNullOrEmpty(s));
    }

    private void TryTakeCrateLootItem(int index)
    {
        if (index < 0 || index >= WarehouseCrateLootCatalog.EntryCount)
            return;

        if (_crateLootTaken[index])
        {
            _crateLootFeedback = "Already taken.";
            _crateLootFeedbackTimer = 1.6f;
            return;
        }

        var entry = WarehouseCrateLootCatalog.Entries[index];

        if (!TryAddToBackpack(entry.Name))
        {
            _crateLootFeedback = "Backpack is full.";
            _crateLootFeedbackTimer = 1.6f;
            return;
        }

        RecordHistorySnapshot();
        _crateLootTaken[index] = true;
        _crateLootFeedback = $"Took {entry.Name}";
        _crateLootFeedbackTimer = 1.2f;

        RefreshCrateLootVisibleList();
        if (_crateLootVisibleCount <= 0)
            CloseCrateLootMenu();
    }

    private void TryTakeAllCrateLootItems()
    {
        if (_crateLootVisibleCount <= 0)
            return;

        RecordHistorySnapshot();
        int takenCount = 0;
        bool backpackFull = false;
        for (int i = 0; i < WarehouseCrateLootCatalog.EntryCount; i++)
        {
            if (_crateLootTaken[i])
                continue;

            var entry = WarehouseCrateLootCatalog.Entries[i];

            if (!TryAddToBackpack(entry.Name))
            {
                backpackFull = true;
                break;
            }

            _crateLootTaken[i] = true;
            takenCount++;
        }

        RefreshCrateLootVisibleList();

        if (takenCount == 0)
        {
            _crateLootFeedback = "Backpack is full.";
            _crateLootFeedbackTimer = 1.6f;
            return;
        }

        _crateLootFeedback = backpackFull ? "Backpack full — took what you could." : "Took everything.";
        _crateLootFeedbackTimer = 1.4f;

        if (_crateLootVisibleCount <= 0)
            CloseCrateLootMenu();
    }

    private void ResetBodyLoot() => Array.Clear(_bodyLootTaken);

    private bool BodyHasRemainingLoot(int bodyIndex)
    {
        if (bodyIndex < 0 || bodyIndex >= WarehouseBodyLootCatalog.BodyCount)
            return false;

        var items = WarehouseBodyLootCatalog.Bodies[bodyIndex].Items;
        for (int i = 0; i < items.Length; i++)
        {
            if (!_bodyLootTaken[WarehouseBodyLootCatalog.ToGlobalIndex(bodyIndex, i)])
                return true;
        }

        return false;
    }

    private void RefreshBodyLootVisibleList()
    {
        if (_activeBodyIndex < 0 || _activeBodyIndex >= WarehouseBodyLootCatalog.BodyCount)
        {
            _bodyLootVisibleCount = 0;
            _bodyLootHighlightedIndex = 0;
            _bodyLootDetailIndex = -1;
            return;
        }

        int count = 0;
        var items = WarehouseBodyLootCatalog.Bodies[_activeBodyIndex].Items;
        for (int i = 0; i < items.Length; i++)
        {
            if (!_bodyLootTaken[WarehouseBodyLootCatalog.ToGlobalIndex(_activeBodyIndex, i)])
                _bodyLootVisibleCatalogIndices[count++] = i;
        }

        _bodyLootVisibleCount = count;

        if (_bodyLootVisibleCount <= 0)
        {
            _bodyLootHighlightedIndex = 0;
            _bodyLootDetailIndex = -1;
            return;
        }

        _bodyLootHighlightedIndex = Math.Clamp(_bodyLootHighlightedIndex, 0, _bodyLootVisibleCount - 1);
        if (_bodyLootDetailIndex >= _bodyLootVisibleCount)
            _bodyLootDetailIndex = _bodyLootHighlightedIndex;
    }

    private int GetBodyLootItemIndexFromVisibleIndex(int visibleIndex) =>
        visibleIndex < 0 || visibleIndex >= _bodyLootVisibleCount
            ? -1
            : _bodyLootVisibleCatalogIndices[visibleIndex];

    private void OpenBodyLootMenu(int bodyIndex)
    {
        if (_phase != Phase.WarehouseAftermath || !BodyHasRemainingLoot(bodyIndex))
            return;

        _showBodyLootMenu = true;
        _activeBodyIndex = bodyIndex;
        _bodyLootHighlightedIndex = 0;
        _bodyLootDetailIndex = -1;
        _bodyLootFeedback = "";
        _bodyLootFeedbackTimer = 0f;
        _bodyLootCloseHovered = false;
        _bodyLootTakeAllHovered = false;
        _bodyLootPickupHovered = false;
        RefreshBodyLootVisibleList();
    }

    private void CloseBodyLootMenu()
    {
        _showBodyLootMenu = false;
        _activeBodyIndex = -1;
        _bodyLootDetailIndex = -1;
        _bodyLootFeedback = "";
        _bodyLootFeedbackTimer = 0f;
        _bodyLootCloseHovered = false;
        _bodyLootTakeAllHovered = false;
        _bodyLootPickupHovered = false;
    }

    private void OpenWarehouseLock()
    {
        if (_phase != Phase.WarehouseAftermath)
            return;

        _warehouseKeypad.Open();
    }

    private void OpenWarehouseDoor()
    {
        if (_phase != Phase.WarehouseAftermath)
            return;

        if (_warehouseKeypad.IsUnlocked)
        {
            RecordHistorySnapshot();
            EnterWarehouseInterior();
            return;
        }

        _actionMessage = "The roll-up door is locked.";
        _actionMessageTimer = 2.4f;
    }

    private void CloseWarehouseLock() => _warehouseKeypad.Close();

    private void OpenWarehouseCrateDialog()
    {
        if (_phase != Phase.WarehouseInterior)
            return;

        if (_warehouseCrateOpened && CrateHasRemainingLoot())
            return;

        _showWarehouseCrateDialog = true;
        _warehouseCrateCloseHovered = false;
    }

    private void CloseWarehouseCrateDialog()
    {
        _showWarehouseCrateDialog = false;
        _warehouseCrateCloseHovered = false;
    }

    private void TryOpenWarehouseCrateWithCrowbar()
    {
        if (_phase != Phase.WarehouseInterior || _warehouseCrateOpened)
            return;

        if (!HasBackpackItem(GameItems.Crowbar))
            return;

        RecordHistorySnapshot();
        _warehouseCrateOpened = true;
        _actionMessage =
            "You work the crowbar under the lid. Nails shriek and splinter — inside: a note, " +
            "a bottle of vodka, and a rag.";
        _actionMessageTimer = 3.4f;
        AdvanceTime();
        CloseWarehouseCrateDialog();
        StopItemUseMode();
        OpenCrateLootMenu();
    }

    private bool HasBackpackItemAtSlot(int slotIndex, string itemName) =>
        slotIndex >= 0 &&
        slotIndex < _backpack.Length &&
        string.Equals(_backpack[slotIndex], itemName, StringComparison.OrdinalIgnoreCase);

    private bool HasItemUseScene() =>
        _phase != Phase.Death && _backgroundTexture.Id != 0;

    private bool ItemUseCursorOverSealedWarehouseCrate() =>
        _phase == Phase.WarehouseInterior &&
        !_warehouseCrateOpened &&
        _warehouseCrateClickRect.Width > 0 &&
        Raylib.CheckCollisionPointRec(_itemUseCursorPos, _warehouseCrateClickRect);

    private bool ItemUseCursorOverWarehouseCrate() =>
        _phase == Phase.WarehouseInterior &&
        _warehouseCrateClickRect.Width > 0 &&
        Raylib.CheckCollisionPointRec(_itemUseCursorPos, _warehouseCrateClickRect);

    private bool ItemUseCursorOverCafeBoris() =>
        _phase == Phase.Cafe &&
        _cafeBorisClickRect.Width > 0 &&
        Raylib.CheckCollisionPointRec(_itemUseCursorPos, _cafeBorisClickRect);

    private void UpdateCafeBorisClickRect(Rectangle artBounds)
    {
        float borisW = CafeHotspots.BorisX2 - CafeHotspots.BorisX1;
        float borisH = CafeHotspots.BorisY2 - CafeHotspots.BorisY1;
        _cafeBorisClickRect = SceneRegion.ToScreenRect(
            CafeHotspots.BorisX1,
            CafeHotspots.BorisY1,
            borisW,
            borisH,
            artBounds);
    }

    private void DieFromAttackingBoris()
    {
        CloseCafeOwnerDialog();
        EnterDeath(
            "You went for Boris with a weapon.",
            "He put you down before you cleared the counter.");
    }

    private void GetItemUseMovementBounds(out int boundsX, out int boundsY, out int boundsW, out int boundsH)
    {
        GetCinematicArtBounds(out int artX, out int artY, out int artW, out int artH);
        boundsX = 0;
        boundsY = GameConstants.TopBarHeight;
        boundsW = artX + artW;
        boundsH = _screenHeight - GameConstants.ActionBarHeight - boundsY;
    }

    private void StartItemUseMode()
    {
        if (_dialogItemIndex < 0 || !CanShowItemUseAction(_dialogItemName, _dialogItemIndex))
            return;

        if (GetDialogItemAction(_dialogItemName, _dialogItemIndex) != DialogItemAction.Use)
            return;

        if (!HasItemUseScene())
        {
            _actionMessage = "There's no scene here to use that on.";
            _actionMessageTimer = ActionMessageDuration;
            return;
        }

        if (!HasBackpackItemAtSlot(_dialogItemIndex, _dialogItemName))
            return;

        string itemName = _dialogItemName;
        int slotIndex = _dialogItemIndex;
        CloseItemDialog();
        GetItemUseMovementBounds(out int boundsX, out int boundsY, out int boundsW, out int boundsH);
        var movementBounds = new Rectangle(boundsX, boundsY, boundsW, boundsH);
        Vector2 mouse = Raylib.GetMousePosition();
        _itemUsePointerInsideBounds = Raylib.CheckCollisionPointRec(mouse, movementBounds);
        _itemUseActive = true;
        _itemUseItemName = itemName;
        _itemUseSlotIndex = slotIndex;
        _warehouseCrateHotspotHovered = false;
        _cafeBorisHotspotHovered = false;
        if (_itemUsePointerInsideBounds)
        {
            _itemUseCursorPos = mouse;
            Raylib.HideCursor();
        }
        else
            Raylib.ShowCursor();
    }

    private void StopItemUseMode()
    {
        if (!_itemUseActive)
            return;

        _itemUseActive = false;
        _itemUsePointerInsideBounds = false;
        _itemUseItemName = "";
        _itemUseSlotIndex = -1;
        _warehouseCrateHotspotHovered = false;
        _cafeBorisHotspotHovered = false;
        Raylib.ShowCursor();
    }

    private static Vector2 ClampPointToRectangle(Vector2 point, Rectangle bounds)
    {
        float x = Math.Clamp(point.X, bounds.X, bounds.X + bounds.Width);
        float y = Math.Clamp(point.Y, bounds.Y, bounds.Y + bounds.Height);
        return new Vector2(x, y);
    }

    private void UpdateItemUseCursor(float dt, Rectangle movementBounds)
    {
        if (InputManager.IsGamepadConnected)
        {
            _itemUsePointerInsideBounds = true;
            Raylib.HideCursor();

            int pad = InputManager.ActiveGamepad;
            float stickX = Raylib.GetGamepadAxisMovement(pad, GamepadAxis.GAMEPAD_AXIS_LEFT_X);
            float stickY = Raylib.GetGamepadAxisMovement(pad, GamepadAxis.GAMEPAD_AXIS_LEFT_Y);
            if (MathF.Abs(stickX) > ItemUseStickDeadzone || MathF.Abs(stickY) > ItemUseStickDeadzone)
            {
                _itemUseCursorPos.X += stickX * ItemUseMoveSpeed * dt;
                _itemUseCursorPos.Y += stickY * ItemUseMoveSpeed * dt;
                _itemUseCursorPos = ClampPointToRectangle(_itemUseCursorPos, movementBounds);
            }

            return;
        }

        Vector2 mouse = Raylib.GetMousePosition();
        _itemUsePointerInsideBounds = Raylib.CheckCollisionPointRec(mouse, movementBounds);
        if (_itemUsePointerInsideBounds)
        {
            Raylib.HideCursor();
            _itemUseCursorPos = mouse;
        }
        else
            Raylib.ShowCursor();
    }

    private void UpdateWarehouseCrateClickRect(Rectangle artBounds)
    {
        float crateW = WarehouseInteriorHotspots.CrateX2 - WarehouseInteriorHotspots.CrateX1;
        float crateH = WarehouseInteriorHotspots.CrateY2 - WarehouseInteriorHotspots.CrateY1;
        _warehouseCrateClickRect = SceneRegion.ToScreenRect(
            WarehouseInteriorHotspots.CrateX1,
            WarehouseInteriorHotspots.CrateY1,
            crateW,
            crateH,
            artBounds);
    }

    private string GetItemUseFailureMessage(string itemName)
    {
        if (_phase == Phase.Cafe && ItemUseCursorOverCafeBoris())
        {
            return itemName switch
            {
                "Lighter" => "Boris snuffs the flame with two fingers. \"Don't.\"",
                "Phone" or GameItems.BurnerPhone => "Boris glances at the screen and laughs. \"Nobody's coming.\"",
                GameItems.Vodka => "He takes the bottle without thanks and sets it out of reach.",
                GameItems.Rag => "Boris flicks the rag into the sink. \"Not in my place.\"",
                _ => "Boris doesn't move. \"Put that away before I put you away.\""
            };
        }

        if (_phase == Phase.WarehouseInterior && ItemUseCursorOverWarehouseCrate())
        {
            if (string.Equals(itemName, GameItems.Crowbar, StringComparison.OrdinalIgnoreCase))
            {
                if (_warehouseCrateOpened)
                    return "The crate lid is already forced open — nothing left to lever.";

                return "The crowbar skids along the steel bands but finds no purchase.";
            }

            return itemName switch
            {
                GameItems.Knife => "The blade rings off the crate — nothing gives way.",
                "Lighter" => "The flame licks the steel bands and dies. The crate doesn't care.",
                _ => "That won't work on the crate."
            };
        }

        if (string.Equals(itemName, GameItems.Crowbar, StringComparison.OrdinalIgnoreCase))
        {
            if (_phase == Phase.WarehouseInterior)
                return "The crowbar rings off concrete and pallet wood — nothing here gives way.";

            return "Nothing here yields to the crowbar.";
        }

        return itemName switch
        {
            GameItems.Knife => "You sweep the blade through the air. Nothing here needs cutting.",
            "Lighter" => "You flick the wheel. Nothing here needs burning.",
            GameItems.Molotov => "Nothing here to soak and ignite.",
            GameItems.LitMolotov => "There is nowhere safe to throw it here.",
            "Phone" or GameItems.BurnerPhone => "No signal. The screen stays dark.",
            _ => "Nothing here responds to that."
        };
    }

    private void TryItemUseAtCursor()
    {
        if (!HasBackpackItemAtSlot(_itemUseSlotIndex, _itemUseItemName))
        {
            StopItemUseMode();
            return;
        }

        if (string.Equals(_itemUseItemName, GameItems.Crowbar, StringComparison.OrdinalIgnoreCase) &&
            ItemUseCursorOverSealedWarehouseCrate())
        {
            TryOpenWarehouseCrateWithCrowbar();
            return;
        }

        if (ItemUseCursorOverCafeBoris() && GameItems.IsWeapon(_itemUseItemName))
        {
            DieFromAttackingBoris();
            return;
        }

        _actionMessage = GetItemUseFailureMessage(_itemUseItemName);
        _actionMessageTimer = ActionMessageDuration;
        StopItemUseMode();
    }

    private void UpdateItemUseMode(float dt, bool leftClicked)
    {
        if (!HasBackpackItemAtSlot(_itemUseSlotIndex, _itemUseItemName) || !HasItemUseScene())
        {
            StopItemUseMode();
            return;
        }

        GetItemUseMovementBounds(out int boundsX, out int boundsY, out int boundsW, out int boundsH);
        var movementBounds = new Rectangle(boundsX, boundsY, boundsW, boundsH);
        UpdateItemUseCursor(dt, movementBounds);

        _warehouseCrateHotspotHovered = false;
        _warehouseCrateClickRect = default;
        _cafeBorisHotspotHovered = false;
        _cafeBorisClickRect = default;

        if (_itemUsePointerInsideBounds)
        {
            GetCinematicArtBounds(out int artX, out int artY, out int artW, out int artH);
            var artBounds = new Rectangle(artX, artY, artW, artH);

            if (_phase == Phase.WarehouseInterior)
            {
                UpdateWarehouseCrateClickRect(artBounds);
                _warehouseCrateHotspotHovered = ItemUseCursorOverWarehouseCrate();
            }

            if (_phase == Phase.Cafe)
            {
                UpdateCafeBorisClickRect(artBounds);
                _cafeBorisHotspotHovered = ItemUseCursorOverCafeBoris();
            }
        }

        if (_itemUsePointerInsideBounds && (leftClicked || InputManager.IsConfirmPressed()))
            TryItemUseAtCursor();
    }

    private string GetItemUseHintText()
    {
        if (string.Equals(_itemUseItemName, GameItems.Crowbar, StringComparison.OrdinalIgnoreCase) &&
            _phase == Phase.WarehouseInterior && !_warehouseCrateOpened)
            return "Click the crate to pry it open · Esc to cancel";

        return "Click to try it · Esc to cancel";
    }

    private void DrawItemUseOverlay()
    {
        if (!_itemUseActive || !_itemUsePointerInsideBounds)
            return;

        float half = ItemUseIconSize / 2f;
        var iconDest = new Rectangle(
            _itemUseCursorPos.X - half,
            _itemUseCursorPos.Y - half,
            ItemUseIconSize,
            ItemUseIconSize);
        DrawItemIcon(_itemUseItemName, iconDest, Color.WHITE, _itemUseSlotIndex);

        GetCinematicArtBounds(out int artX, out int artY, out int artW, out int artH);
        Font font = _uiFont;
        string hint = GetItemUseHintText();
        const float hintSize = 14f;
        Vector2 hintMeasure = Raylib.MeasureTextEx(font, hint, hintSize, 0.5f);
        float hintX = artX + (artW - hintMeasure.X) / 2f;
        float hintY = artY + artH - hintMeasure.Y - 14f;
        var hintBg = new Rectangle(hintX - 10f, hintY - 4f, hintMeasure.X + 20f, hintMeasure.Y + 8f);
        Raylib.DrawRectangleRec(hintBg, new Color(10, 12, 16, 210));
        Raylib.DrawRectangleLinesEx(hintBg, 1f, Palette.SubtleBorder);
        Raylib.DrawTextEx(font, hint, new Vector2(hintX, hintY), hintSize, 0.5f, Palette.TextPrimary);
    }

    private void EnterWarehouseInterior()
    {
        if (_phase != Phase.WarehouseAftermath)
            return;

        CloseWarehouseLock();
        CloseBodyLootMenu();
        CloseItemDialog();
        CloseWarehouseCrateDialog();
        _actionMessage = "The door lifts. Smoke and rain roll over your boots as you step inside.";
        _actionMessageTimer = 3.2f;
        AdvanceTime();
        EnterPhase(Phase.WarehouseInterior);
    }

    private void ExitWarehouseToLoadingBay()
    {
        if (_phase != Phase.WarehouseInterior)
            return;

        CloseCrateLootMenu();
        CloseWarehouseCrateDialog();
        StopItemUseMode();
        _actionMessage = "You duck back out into the smoking bay.";
        _actionMessageTimer = 2.2f;
        AdvanceTime();
        EnterPhase(Phase.WarehouseAftermath);
    }

    private bool CanTakeBodyLootItem(int itemIndex)
    {
        if (_activeBodyIndex < 0 || itemIndex < 0)
            return false;

        var items = WarehouseBodyLootCatalog.Bodies[_activeBodyIndex].Items;
        if (itemIndex >= items.Length)
            return false;

        int globalIndex = WarehouseBodyLootCatalog.ToGlobalIndex(_activeBodyIndex, itemIndex);
        if (_bodyLootTaken[globalIndex])
            return false;

        var entry = items[itemIndex];
        return _backpack.Any(s => string.IsNullOrEmpty(s));
    }

    private void TryTakeBodyLootItem(int itemIndex)
    {
        if (_activeBodyIndex < 0 || itemIndex < 0)
            return;

        var items = WarehouseBodyLootCatalog.Bodies[_activeBodyIndex].Items;
        if (itemIndex >= items.Length)
            return;

        int globalIndex = WarehouseBodyLootCatalog.ToGlobalIndex(_activeBodyIndex, itemIndex);
        if (_bodyLootTaken[globalIndex])
        {
            _bodyLootFeedback = "Already taken.";
            _bodyLootFeedbackTimer = 1.6f;
            return;
        }

        var entry = items[itemIndex];

        if (!TryAddToBackpack(entry.Name))
        {
            _bodyLootFeedback = "Backpack is full.";
            _bodyLootFeedbackTimer = 1.6f;
            return;
        }

        RecordHistorySnapshot();
        _bodyLootTaken[globalIndex] = true;
        _bodyLootFeedback = $"Took {entry.Name}";
        _bodyLootFeedbackTimer = 1.2f;

        RefreshBodyLootVisibleList();
        if (_bodyLootVisibleCount <= 0)
            CloseBodyLootMenu();
    }

    private void TryTakeAllBodyLootItems()
    {
        if (_activeBodyIndex < 0 || _bodyLootVisibleCount <= 0)
            return;

        RecordHistorySnapshot();
        var items = WarehouseBodyLootCatalog.Bodies[_activeBodyIndex].Items;
        int takenCount = 0;
        bool backpackFull = false;
        for (int i = 0; i < items.Length; i++)
        {
            int globalIndex = WarehouseBodyLootCatalog.ToGlobalIndex(_activeBodyIndex, i);
            if (_bodyLootTaken[globalIndex])
                continue;

            var entry = items[i];

            if (!TryAddToBackpack(entry.Name))
            {
                backpackFull = true;
                break;
            }

            _bodyLootTaken[globalIndex] = true;
            takenCount++;
        }

        RefreshBodyLootVisibleList();

        if (takenCount == 0)
        {
            _bodyLootFeedback = "Backpack is full.";
            _bodyLootFeedbackTimer = 1.6f;
            return;
        }

        _bodyLootFeedback = backpackFull ? "Backpack full — took what you could." : "Took everything.";
        _bodyLootFeedbackTimer = 1.4f;

        if (_bodyLootVisibleCount <= 0)
            CloseBodyLootMenu();
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
                break;
            case Phase.Town:
                _actionMessage = "You flatten yourself against a wall and watch the street. Nothing moves.";
                break;
            case Phase.IndustrialDistrict:
                _actionMessage = "You press into the shadow of a loading bay and listen. The yards are still.";
                break;
            case Phase.CommercialDistrict:
                _actionMessage = "You linger in an alley between shopfronts. The street stays empty.";
                break;
            case Phase.Store:
                _actionMessage = "You linger by the shelves, pretending to read labels.";
                break;
            case Phase.Cafe:
                _actionMessage = "You keep your head down. The owner hasn't stopped watching you.";
                break;
            case Phase.DeliveryTruck:
                _actionMessage = "The engine rumbles under you. The yards are a few minutes away.";
                break;
            case Phase.WarehouseTruck:
                _actionMessage = "You sit in the cab and watch the bay through the windshield.";
                break;
            case Phase.WarehouseAmbush:
                _actionMessage = "You hold still, listening to the rain and the men breathing in the dark.";
                break;
            case Phase.WarehouseAftermath:
                _actionMessage = "You stay low beside the truck. The fire crackles; the bratdvas don't move.";
                break;
            case Phase.GasStation:
                _actionMessage = "You stand under the awning, listening to rain drum on the pumps.";
                break;
            case Phase.WarehouseInterior:
                _actionMessage = "You stand in the aisle between the pallets, listening to rain on the roof.";
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
        _actionMessageTimer = ActionMessageDuration;
    }

    private void UpdateTopRightButtonsLayout()
    {
        const float size = 20f;
        const float gap = 6f;
        const float colGap = 6f;
        const float margin = 26f;
        float col0X = _screenWidth - margin - size;
        float col1X = col0X - colGap - size;

        _restartButtonRect = new Rectangle(col0X, 10f, size, size);
        _debugStartButtonRect = new Rectangle(col0X, 10f + (size + gap), size, size);
        _areaSelectButtonRect = new Rectangle(col0X, 10f + (size + gap) * 2f, size, size);
        _controllerButtonRect = new Rectangle(col0X, 10f + (size + gap) * 3f, size, size);
        _copyRoomIdButtonRect = new Rectangle(col1X, 10f, size, size);
    }

    private float TopRightToolbarLeftEdge() =>
        _copyRoomIdButtonRect.Width > 0 ? _copyRoomIdButtonRect.X : _restartButtonRect.X;

    private void UpdateHistoryButtonsLayout()
    {
        const float size = 36f;
        const float gap = 8f;
        const float leftX = 26f;
        const float titleRowY = 14f;
        const float titleLogoHeight = 38f;
        float y = titleRowY + titleLogoHeight + 6f;

        _undoButtonRect = new Rectangle(leftX, y, size, size);
        _redoButtonRect = new Rectangle(leftX + size + gap, y, size, size);
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
        CloseItemDialog();
        CloseStoreBuyMenu();
        CloseGloveBoxMenu();
        CloseCrateLootMenu();
        CloseBodyLootMenu();
        CloseWarehouseLock();
        CloseFoldedPaperReader();
        CloseGasGaugeViewer();
        CloseBuildDialog();
        CloseForageDialog();
        CloseCafeOwnerDialog();
        CloseWarehouseCrateDialog();
        StopItemUseMode();
        CloseControllerDebug();
        CloseQuitConfirm();
        _sceneAreaSelect.Close();
    }

    private void ClearActionHistory()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }

    private bool CanUndoAction() => _undoStack.Count > 0;

    private bool CanRedoAction() => _redoStack.Count > 0;

    private static List<DroppedItem> CloneDroppedItems(IEnumerable<DroppedItem> items) =>
        items.Select(d => new DroppedItem
        {
            Name = d.Name,
            Charges = d.Charges,
            Room = d.Room,
            TurnsRemaining = d.TurnsRemaining,
            AnchorIndex = d.AnchorIndex
        }).ToList();

    private GameStateSnapshot CaptureStateSnapshot() => new()
    {
        Phase = _phase,
        PhaseOutdoorBeforeTent = _phaseOutdoorBeforeTent,
        PhaseBeforeStore = _phaseBeforeStore,
        PhaseBeforeCafe = _phaseBeforeCafe,
        BorisDeliveryJobActive = _borisDeliveryJobActive,
        WarehouseAmbushersDead = _warehouseAmbushersDead,
        FoldedPaperMessageRead = _foldedPaperMessageRead,
        NoteMessageRead = _noteMessageRead,
        GasGaugeFuel = _gasGaugeFuel,
        WarehouseCrateOpened = _warehouseCrateOpened,
        WarehouseKeypadUnlocked = _warehouseKeypad.IsUnlocked,
        HasTrashBagTent = _hasTrashBagTent,
        TentBuiltInPhase = _tentBuiltInPhase,
        CafeOwnerDialogStage = _cafeOwnerDialogStage,
        Day = _day,
        TimeOfDay = _timeOfDay,
        Location = _location,
        City = _city,
        Season = _season,
        TemperatureF = _temperatureF,
        Backpack = (string?[])_backpack.Clone(),
        BackpackItemCharges = (int?[])_backpackItemCharges.Clone(),
        DroppedItems = CloneDroppedItems(_droppedItems),
        GloveBoxLootTaken = (bool[])_gloveBoxLootTaken.Clone(),
        BodyLootTaken = (bool[])_bodyLootTaken.Clone(),
        CrateLootTaken = (bool[])_crateLootTaken.Clone(),
        Choices = (string[])_choices.Clone(),
        SelectedIndex = _selectedIndex,
        ActionMessage = _actionMessage,
        ActionMessageTimer = _actionMessageTimer,
        DeathLine1 = _deathLine1,
        DeathLine2 = _deathLine2
    };

    private void RestoreStateSnapshot(GameStateSnapshot snapshot)
    {
        _isRestoringHistory = true;

        try
        {
            CloseAllOverlays();

            _phase = snapshot.Phase;
            _phaseOutdoorBeforeTent = snapshot.PhaseOutdoorBeforeTent;
            _phaseBeforeStore = snapshot.PhaseBeforeStore;
            _phaseBeforeCafe = snapshot.PhaseBeforeCafe;
            _borisDeliveryJobActive = snapshot.BorisDeliveryJobActive;
            _warehouseAmbushersDead = snapshot.WarehouseAmbushersDead;
            _foldedPaperMessageRead = snapshot.FoldedPaperMessageRead;
            _noteMessageRead = snapshot.NoteMessageRead;
            _gasGaugeFuel = snapshot.GasGaugeFuel;
            _warehouseCrateOpened = snapshot.WarehouseCrateOpened;
            _hasTrashBagTent = snapshot.HasTrashBagTent;
            _tentBuiltInPhase = snapshot.TentBuiltInPhase;
            _cafeOwnerDialogStage = snapshot.CafeOwnerDialogStage;
            _day = snapshot.Day;
            _timeOfDay = snapshot.TimeOfDay;
            _location = snapshot.Location;
            _city = snapshot.City;
            _season = snapshot.Season;
            _temperatureF = snapshot.TemperatureF;
            _backpack = (string?[])snapshot.Backpack.Clone();
            _backpackItemCharges = (int?[])snapshot.BackpackItemCharges.Clone();
            _droppedItems.Clear();
            _droppedItems.AddRange(CloneDroppedItems(snapshot.DroppedItems));
            Array.Copy(snapshot.GloveBoxLootTaken, _gloveBoxLootTaken, _gloveBoxLootTaken.Length);
            Array.Copy(snapshot.BodyLootTaken, _bodyLootTaken, _bodyLootTaken.Length);
            Array.Copy(snapshot.CrateLootTaken, _crateLootTaken, _crateLootTaken.Length);
            _choices = (string[])snapshot.Choices.Clone();
            _selectedIndex = snapshot.SelectedIndex;
            _actionMessage = snapshot.ActionMessage;
            _actionMessageTimer = snapshot.ActionMessageTimer;
            _deathLine1 = snapshot.DeathLine1;
            _deathLine2 = snapshot.DeathLine2;

            _warehouseKeypad.RestoreUnlockedState(snapshot.WarehouseKeypadUnlocked);
            ApplyBackgroundForCurrentPhase();

            if (_choices.Length == 0)
                _selectedIndex = 0;
            else
                _selectedIndex = Math.Clamp(_selectedIndex, 0, _choices.Length - 1);
        }
        finally
        {
            _isRestoringHistory = false;
        }
    }

    private void RecordHistorySnapshot()
    {
        if (_isRestoringHistory || _historyRecordSuppression > 0)
            return;

        _undoStack.Add(CaptureStateSnapshot());
        if (_undoStack.Count > MaxHistoryDepth)
            _undoStack.RemoveAt(0);
        _redoStack.Clear();
    }

    private void UndoLastAction()
    {
        if (!CanUndoAction())
            return;

        _redoStack.Add(CaptureStateSnapshot());
        RestoreStateSnapshot(_undoStack[^1]);
        _undoStack.RemoveAt(_undoStack.Count - 1);
    }

    private void RedoLastAction()
    {
        if (!CanRedoAction())
            return;

        _undoStack.Add(CaptureStateSnapshot());
        RestoreStateSnapshot(_redoStack[^1]);
        _redoStack.RemoveAt(_redoStack.Count - 1);
    }

    private bool BlocksActionBarNavigation() =>
        _showStoreBuyMenu || _showGloveBoxMenu || _showCrateLootMenu || _showBodyLootMenu
        || _showBuildDialog || _showForageDialog || _showCafeOwnerDialog || _showWarehouseCrateDialog
        || _itemUseActive || _showControllerDebug || _showQuitConfirm
        || _sceneAreaSelect.IsActive || _warehouseKeypad.IsOpen || _foldedPaperReader.IsOpen
        || _gasGaugeViewer.IsOpen;

    private bool AllowsSidebarAndSceneInput() =>
        !_showStoreBuyMenu && !_showGloveBoxMenu && !_showCrateLootMenu && !_showBodyLootMenu
        && !_showBuildDialog && !_showForageDialog && !_showCafeOwnerDialog
        && !_showWarehouseCrateDialog && !_itemUseActive && !_showQuitConfirm
        && !_sceneAreaSelect.IsActive && !_warehouseKeypad.IsOpen && !_foldedPaperReader.IsOpen
        && !_gasGaugeViewer.IsOpen;

    private bool CanUseSceneAreaSelect() =>
        _phase != Phase.Death && _backgroundTexture.Id != 0;

    private void RestartGame()
    {
        ClearActionHistory();
        _actionMessage = "";
        _actionMessageTimer = 0f;
        _selectedIndex = 0;
        CloseAllOverlays();
        _hasTrashBagTent = false;
        _tentBuiltInPhase = null;
        _borisDeliveryJobActive = false;
        _warehouseAmbushersDead = false;
        _warehouseCrateOpened = false;
        ResetGloveCompartmentLoot();
        ResetCrateLoot();
        ResetBodyLoot();
        _foldedPaperMessageRead = false;
        _noteMessageRead = false;
        _gasGaugeFuel = GasGaugeCatalog.EmptyFuel;
        _warehouseKeypad.Reset();
        _buildFeedback = "";
        ResetDeathLines();
        ClearDroppedItems();
        EnterPhase(Phase.Opening);
    }

    /// <summary>
    /// Jump to the gas station after the warehouse aftermath.
    /// </summary>
    private void DebugStartGame()
    {
        ClearActionHistory();
        CloseAllOverlays();
        _hasTrashBagTent = false;
        _tentBuiltInPhase = null;
        _buildFeedback = "";
        ResetDeathLines();
        _backpack = new string?[BackpackSlotCount]
        {
            "Knife", "Lighter", "Phone", GameItems.Crowbar, null, null, null, null, null, null, null, null
        };
        _backpackItemCharges = new int?[BackpackSlotCount];

        _phaseBeforeCafe = Phase.IndustrialDistrict;
        _borisDeliveryJobActive = true;
        _warehouseAmbushersDead = true;
        _warehouseCrateOpened = false;
        ResetGloveCompartmentLoot();
        ResetCrateLoot();
        ResetBodyLoot();
        _foldedPaperMessageRead = false;
        _noteMessageRead = false;
        _gasGaugeFuel = GasGaugeCatalog.EmptyFuel;
        _warehouseKeypad.Reset();
        EnterPhase(Phase.GasStation);
    }

    // --- Inventory & ground items ---
    private void OpenItemDialog(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _backpack.Length) return;
        string? item = _backpack[slotIndex];
        if (string.IsNullOrEmpty(item)) return;

        if (_showItemDialog && _dialogItemIndex == slotIndex && _dialogDroppedItemIndex < 0)
        {
            CloseItemDialog();
            return;
        }

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

        if (_showItemDialog && _dialogDroppedItemIndex == droppedIndex)
        {
            CloseItemDialog();
            return;
        }

        _dialogItemIndex = -1;
        _dialogDroppedItemIndex = droppedIndex;
        _dialogItemName = dropped.Name;
        _showItemDialog = true;
        ResetItemDialogHover();
    }

    private void ResetItemDialogHover()
    {
        _dialogActionHovered = false;
        _dialogSecondaryActionHovered = false;
        _dialogDropHovered = false;
        _dialogActionRect = default;
        _dialogSecondaryActionRect = default;
        _dialogDropRect = default;
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

        RecordHistorySnapshot();

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

        RecordHistorySnapshot();

        if (!TryAddToBackpack(dropped.Name, dropped.Charges))
        {
            _actionMessage = "Backpack is full — make space before picking this up.";
            _actionMessageTimer = ActionMessageDuration;
            return;
        }

        _droppedItems.RemoveAt(_dialogDroppedItemIndex);
        _actionMessage = $"You pick up the {dropped.Name}.";
        _actionMessageTimer = ActionMessageDuration;
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
        RecordHistorySnapshot();
        CloseCafeOwnerDialog();
        _borisDeliveryJobActive = true;
        ResetGloveCompartmentLoot();
        _gasGaugeFuel = GasGaugeCatalog.AlmostEmptyFuel;
        _actionMessage = "Boris slides keys across the counter. \"Get in the truck. " +
                         CafeOwnerDialog.WarehouseName + ", bay three. Move.\"";
        _actionMessageTimer = 2.8f;
        AdvanceTime();
        EnterPhase(Phase.DeliveryTruck);
    }

    private void DeclineBorisDeliveryJob()
    {
        RecordHistorySnapshot();
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

        RecordHistorySnapshot();
        CloseForageDialog();

        string item = ForageOptionItems[optionIndex];
        AdvanceTime();

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

        AdvanceTime();

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

        RecordHistorySnapshot();
        AdvanceTime();

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
            _actionMessage = stored
                ? "You catch a raccoon. You eat what you can, then stash the rest."
                : "You catch a raccoon. You eat what you can, but your pack is full — you leave the rest.";
        }
        else
        {
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
        EnterPhase(Phase.Tent);
    }

    private void ExitTent()
    {
        if (_phase != Phase.Tent)
            return;

        _actionMessage = "You push back out into the cold air.";
        _actionMessageTimer = 2f;
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

        RecordHistorySnapshot();
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

        RecordHistorySnapshot();
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

        RecordHistorySnapshot();
        _hasTrashBagTent = true;
        _tentBuiltInPhase = _phase;
        RefreshOutdoorActionChoices();

        int bagsSlot = FindBackpackSlotIndex(GameItems.TrashBags);
        int tapeSlot = FindBackpackSlotIndex(GameItems.DuctTape);
        bool materialsRemain = (bagsSlot >= 0 && GetBackpackSlotCharges(bagsSlot, GameItems.TrashBags) > 0) ||
                               (tapeSlot >= 0 && GetBackpackSlotCharges(tapeSlot, GameItems.DuctTape) > 0);

        _buildFeedback = materialsRemain
            ? "Shelter pitched — bags and tape only partly used."
            : "You rig a crude shelter from plastic and tape.";
        _buildFeedbackTimer = BuildFeedbackDuration;
        _actionMessage = "Trash bag tent pitched.";
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

        RecordHistorySnapshot();
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
            RefreshOutdoorActionChoices();
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
        FillBottle,
        LightMolotov,
        ThrowLitMolotov,
        ReadPaper,
        Use
    }

    private bool CanThrowLitMolotovAtAmbush() => _phase == Phase.WarehouseAmbush;

    private bool CanLightMolotovFromDialog(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _backpack.Length)
            return false;

        if (!string.Equals(_backpack[slotIndex], GameItems.Molotov, StringComparison.OrdinalIgnoreCase))
            return false;

        return FindBackpackSlotIndex("Lighter") >= 0;
    }

    private DialogItemAction GetDialogItemAction(string itemName, int slotIndex)
    {
        if (string.Equals(itemName, GameItems.LitMolotov, StringComparison.OrdinalIgnoreCase) &&
            CanThrowLitMolotovAtAmbush())
            return DialogItemAction.ThrowLitMolotov;

        if (CanLightMolotovFromDialog(slotIndex))
            return DialogItemAction.LightMolotov;

        if (string.Equals(itemName, GameItems.BottledWater, StringComparison.OrdinalIgnoreCase) &&
            GetBackpackSlotCharges(slotIndex, GameItems.BottledWater) > 0)
            return DialogItemAction.DrinkWater;

        if (string.Equals(itemName, GameItems.CannedSoup, StringComparison.OrdinalIgnoreCase) &&
            GetBackpackSlotCharges(slotIndex, GameItems.CannedSoup) > 0)
            return DialogItemAction.EatSoup;

        if (string.Equals(itemName, GameItems.EmptyBottle, StringComparison.OrdinalIgnoreCase) &&
            _phase == Phase.ForestStream)
            return DialogItemAction.FillBottle;

        if (GameItems.IsFoldedPaper(itemName))
            return DialogItemAction.ReadPaper;

        if (GameItems.IsExcludedFromUse(itemName))
            return DialogItemAction.None;

        return DialogItemAction.Use;
    }

    private bool CanShowItemUseAction(string itemName, int slotIndex) =>
        CanDrinkFromDialogSlot(slotIndex) ||
        CanFillBottleAtStream(slotIndex) ||
        !GameItems.IsExcludedFromUse(itemName);

    private static string GetDialogItemActionLabel(DialogItemAction action) =>
        action switch
        {
            DialogItemAction.DrinkWater => "DRINK",
            DialogItemAction.EatSoup => "EAT",
            DialogItemAction.FillBottle => "FILL",
            DialogItemAction.LightMolotov => "LIGHT",
            DialogItemAction.ThrowLitMolotov => "THROW",
            DialogItemAction.ReadPaper => "READ",
            DialogItemAction.Use => "USE",
            _ => "USE"
        };

    private static bool IsDialogPrimaryAction(DialogItemAction action) =>
        action is DialogItemAction.EatSoup or DialogItemAction.LightMolotov or DialogItemAction.ThrowLitMolotov
            or DialogItemAction.ReadPaper or DialogItemAction.Use;

    private void TryPerformDialogItemAction(DialogItemAction action)
    {
        if (_dialogItemIndex < 0 || _dialogItemIndex >= _backpack.Length) return;

        if (action == DialogItemAction.Use)
        {
            StartItemUseMode();
            return;
        }

        RecordHistorySnapshot();
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
            case DialogItemAction.LightMolotov:
                TryLightMolotovFromItemDialog();
                return;
            case DialogItemAction.ThrowLitMolotov:
                TryThrowLitMolotovFromItemDialog();
                return;
            case DialogItemAction.ReadPaper:
                TryReadFoldedPaper();
                return;
            default:
                return;
        }
    }

    private void TryReadFoldedPaper()
    {
        if (!GameItems.IsFoldedPaper(_dialogItemName))
            return;

        _activeNoteReadItemName = _dialogItemName;

        if (string.Equals(_dialogItemName, GameItems.Note, StringComparison.OrdinalIgnoreCase))
            _noteMessageRead = true;
        else
            _foldedPaperMessageRead = true;

        string title = string.Equals(_dialogItemName, GameItems.Note, StringComparison.OrdinalIgnoreCase)
            ? "NOTE"
            : "FOLDED NOTE";
        _foldedPaperReader.Open(title);
    }

    private Texture2D GetActiveNoteReadTexture() =>
        string.Equals(_activeNoteReadItemName, GameItems.Note, StringComparison.OrdinalIgnoreCase)
            ? _crateNoteTexture
            : _foldedPaperNoteTexture;

    private void CloseFoldedPaperReader()
    {
        _foldedPaperReader.Close();
        _activeNoteReadItemName = "";
    }

    private void OpenGasGaugeViewer()
    {
        if (_phase is not (Phase.DeliveryTruck or Phase.WarehouseTruck))
            return;

        _gasGaugeViewer.Open();
    }

    private void CloseGasGaugeViewer() => _gasGaugeViewer.Close();

    private string GetFoldedPaperDialogText()
    {
        if (string.Equals(_dialogItemName, GameItems.Note, StringComparison.OrdinalIgnoreCase))
        {
            if (!_noteMessageRead)
            {
                return "A creased half-sheet folded into the straw packing. Block letters, no signature.\n\n" +
                    "Press READ to study the note.";
            }

            return "You've studied the note. Press READ again to look at it.";
        }

        if (!_foldedPaperMessageRead)
        {
            return "A half-sheet torn from a ledger, creased and smudged. Someone wrote this in a hurry.\n\n" +
                "Press READ to study the note.";
        }

        return "You've studied the note. Press READ again to look at it.";
    }

    private void TryLightMolotovFromItemDialog()
    {
        if (_dialogItemIndex < 0 || !CanLightMolotovFromDialog(_dialogItemIndex))
            return;

        int slot = _dialogItemIndex;
        RemoveBackpackItemAtSlot(slot);
        CompactBackpack();

        if (!TryAddToBackpack(GameItems.LitMolotov))
        {
            TryAddToBackpack(GameItems.Molotov);
            _actionMessage = "Backpack is full — make space before lighting it.";
            _actionMessageTimer = ActionMessageDuration;
            return;
        }

        CloseItemDialog();
        _actionMessage = "You touch the lighter to the rag. The bottle catches with a hungry hiss.";
        _actionMessageTimer = ActionMessageDuration;
    }

    private void TryThrowLitMolotovFromItemDialog()
    {
        if (!CanThrowLitMolotovAtAmbush() || _dialogItemIndex < 0)
            return;

        string? item = _backpack[_dialogItemIndex];
        if (!string.Equals(item, GameItems.LitMolotov, StringComparison.OrdinalIgnoreCase))
            return;

        int slot = _dialogItemIndex;
        CloseItemDialog();
        ThrowLitMolotovAtWarehouseAmbush(slot);
    }

    private void TryDrinkBottledWater()
    {
        if (_dialogItemIndex < 0 || _dialogItemIndex >= _backpack.Length) return;
        if (!CanDrinkFromDialogSlot(_dialogItemIndex)) return;

        int remaining = GetBackpackSlotCharges(_dialogItemIndex, GameItems.BottledWater) - 1;

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

    private void OpenShopBuyMenu(ShopKind kind)
    {
        _shopBuyKind = kind;
        _showStoreBuyMenu = true;
        _storeBuyHighlightedIndex = 0;
        _storeBuyDetailIndex = -1;
        _storeBuyFeedback = "";
        _storeBuyFeedbackTimer = 0f;
        _storeBuyCloseHovered = false;
        _storeBuyPurchaseHovered = false;
    }

    private void OpenStoreBuyMenu() => OpenShopBuyMenu(ShopKind.Store);

    private void OpenGasStationBuyMenu() => OpenShopBuyMenu(ShopKind.GasStation);

    private void CloseStoreBuyMenu()
    {
        _showStoreBuyMenu = false;
        _storeBuyDetailIndex = -1;
        _storeBuyFeedback = "";
        _storeBuyFeedbackTimer = 0f;
        _storeBuyCloseHovered = false;
        _storeBuyPurchaseHovered = false;
    }

    private bool CanBuyShopItem(int index)
    {
        string[] entries = ShopCatalogs.GetEntries(_shopBuyKind);
        if (index < 0 || index >= entries.Length)
            return false;

        if (!_backpack.Any(s => string.IsNullOrEmpty(s)))
            return false;

        string name = entries[index];
        if (_shopBuyKind == ShopKind.GasStation &&
            string.Equals(name, GameItems.GasCan, StringComparison.OrdinalIgnoreCase) &&
            HasBackpackItem(GameItems.GasCan))
        {
            return false;
        }

        return true;
    }

    private void TryBuyShopItem(int index)
    {
        string[] entries = ShopCatalogs.GetEntries(_shopBuyKind);
        if (index < 0 || index >= entries.Length)
            return;

        string name = entries[index];

        if (!CanBuyShopItem(index))
        {
            _storeBuyFeedback = _shopBuyKind == ShopKind.GasStation &&
                string.Equals(name, GameItems.GasCan, StringComparison.OrdinalIgnoreCase) &&
                HasBackpackItem(GameItems.GasCan)
                ? "You already have a gas can."
                : "Backpack is full.";
            _storeBuyFeedbackTimer = 1.6f;
            return;
        }

        if (!TryAddToBackpack(name))
        {
            _storeBuyFeedback = "Backpack is full.";
            _storeBuyFeedbackTimer = 1.6f;
            return;
        }

        RecordHistorySnapshot();

        _storeBuyFeedback = $"Bought {name}";
        _storeBuyFeedbackTimer = 1.2f;
    }

    private void DrawUndoButton() =>
        GameDialogUi.DrawToolbarIconButton(
            _undoButtonRect,
            _undoHovered,
            GameToolbarIcons.DrawBack,
            CanUndoAction());

    private void DrawRedoButton() =>
        GameDialogUi.DrawToolbarIconButton(
            _redoButtonRect,
            _redoHovered,
            GameToolbarIcons.DrawForward,
            CanRedoAction());

    private void DrawRestartButton() =>
        GameDialogUi.DrawToolbarIconButton(_restartButtonRect, _restartHovered, GameToolbarIcons.DrawRestart);

    private void DrawDebugStartButton() =>
        GameDialogUi.DrawToolbarTextButton(_debugStartButtonRect, _debugStartHovered, _uiFont, "DBG", 10f);

    private void DrawAreaSelectButton() =>
        GameDialogUi.DrawToolbarIconButton(
            _areaSelectButtonRect,
            _sceneAreaSelect.IsActive || _areaSelectHovered,
            GameToolbarIcons.DrawReticle);

    private void DrawControllerButton() =>
        GameDialogUi.DrawToolbarIconButton(
            _controllerButtonRect,
            _showControllerDebug || _controllerHovered,
            GameToolbarIcons.DrawController);

    private void DrawCopyRoomIdButton() =>
        GameDialogUi.DrawToolbarTextButton(_copyRoomIdButtonRect, _copyRoomIdHovered, _uiFont, "ID", 10f);

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

    private void DrawSceneAreaSelectOverlay()
    {
        GetCinematicArtBounds(out int artX, out int artY, out int artW, out int artH);
        _sceneAreaSelect.Draw(_uiFont, new Rectangle(artX, artY, artW, artH));
    }

    private void DrawTopRightButtons()
    {
        DrawCopyRoomIdButton();
        DrawRestartButton();
        DrawDebugStartButton();
        DrawAreaSelectButton();
        DrawControllerButton();
    }

    private void DrawHistoryButtons()
    {
        DrawUndoButton();
        DrawRedoButton();
    }

    // =====================================================================
    // ITEM DIALOG (modal) — use / examine / close per item
    // =====================================================================
    private string GetItemDialogBody(bool isGround, DialogItemAction itemAction, bool canDrink, bool canFill)
    {
        if (isGround)
        {
            int turns = _droppedItems[_dialogDroppedItemIndex].TurnsRemaining;
            string groundLine = turns == 1
                ? "On the ground here. About one turn left before you lose track of it."
                : $"On the ground here. About {turns} turns left before you lose track of it.";
            if (ShopCatalogs.GetFlavorTextForItem(_dialogItemName) is string shopFlavor)
                return groundLine + "\n\n" + shopFlavor;
            return groundLine;
        }

        if (ShopCatalogs.GetFlavorTextForItem(_dialogItemName) is string flavor)
            return flavor;

        if (GameItems.IsFoldedPaper(_dialogItemName))
            return GetFoldedPaperDialogText();

        int slot = _dialogItemIndex;
        return itemAction switch
        {
            DialogItemAction.LightMolotov =>
                "Vodka in a bottle with a rag wick. One spark from your lighter and it's ready to throw.",
            DialogItemAction.Use when string.Equals(_dialogItemName, GameItems.Crowbar, StringComparison.OrdinalIgnoreCase) =>
                "A short steel crowbar with old paint on the curve. Select USE and guide it onto something that might give way.",
            DialogItemAction.Use =>
                "Select USE and guide it onto something in the scene.",
            DialogItemAction.ThrowLitMolotov =>
                "The rag is soaked and burning. One throw into the bratdvas and this bottle stops being vodka.",
            DialogItemAction.EatSoup => GetCannedSoupDialogText(slot),
            _ when canDrink => GetBottledWaterDialogText(slot),
            _ when canFill && string.Equals(_dialogItemName, GameItems.EmptyBottle, StringComparison.OrdinalIgnoreCase) =>
                "An empty plastic bottle. The stream is right here — you could fill it.",
            _ => string.Equals(_dialogItemName, GameItems.EmptyBottle, StringComparison.OrdinalIgnoreCase)
                ? "An empty plastic bottle. Nothing left to drink."
                : string.Equals(_dialogItemName, GameItems.EmptyCan, StringComparison.OrdinalIgnoreCase)
                    ? "An empty can. Nothing left to eat."
                    : "Select USE to try it, or DROP to leave it here."
        };
    }

    private void UpdateItemPanelHover(Vector2 mouse)
    {
        bool isGround = IsDroppedItemDialog;
        bool canDrink = !isGround && CanDrinkFromDialogSlot(_dialogItemIndex);
        bool canFill = !isGround && CanFillBottleAtStream(_dialogItemIndex);

        _dialogActionHovered = _dialogActionRect.Width > 0 &&
            Raylib.CheckCollisionPointRec(mouse, _dialogActionRect) &&
            (isGround || CanShowItemUseAction(_dialogItemName, _dialogItemIndex));
        _dialogSecondaryActionHovered = _dialogSecondaryActionRect.Width > 0 &&
            canDrink && canFill &&
            Raylib.CheckCollisionPointRec(mouse, _dialogSecondaryActionRect);
        _dialogDropHovered = _dialogDropRect.Width > 0 &&
            Raylib.CheckCollisionPointRec(mouse, _dialogDropRect);
    }

    private bool TryPerformItemPanelPrimaryAction()
    {
        if (IsDroppedItemDialog)
        {
            TryPickupDroppedItem();
            return true;
        }

        bool canDrink = CanDrinkFromDialogSlot(_dialogItemIndex);
        bool canFill = CanFillBottleAtStream(_dialogItemIndex);
        DialogItemAction itemAction = GetDialogItemAction(_dialogItemName, _dialogItemIndex);

        if (canDrink)
        {
            TryPerformDialogItemAction(DialogItemAction.DrinkWater);
            return true;
        }

        if (canFill)
        {
            TryPerformDialogItemAction(DialogItemAction.FillBottle);
            return true;
        }

        if (IsDialogPrimaryAction(itemAction))
        {
            TryPerformDialogItemAction(itemAction);
            return true;
        }

        return false;
    }

    private int DrawItemPanel(int startY, int x)
    {
        Font font = _uiFont;
        int available = GameConstants.SidebarWidth - GameConstants.SidebarPadding * 2;
        const int panelPad = 12;
        int innerX = x + panelPad;
        int innerW = available - panelPad * 2;
        int contentY = startY + panelPad;

        bool isGround = IsDroppedItemDialog;
        DialogItemAction itemAction = isGround
            ? DialogItemAction.None
            : GetDialogItemAction(_dialogItemName, _dialogItemIndex);
        bool canDrink = !isGround && CanDrinkFromDialogSlot(_dialogItemIndex);
        bool canFill = !isGround && CanFillBottleAtStream(_dialogItemIndex);
        bool canAct = !isGround && CanShowItemUseAction(_dialogItemName, _dialogItemIndex);

        const int iconSize = 72;
        int iconX = innerX + (innerW - iconSize) / 2;
        int iconY = contentY;

        string title = _dialogItemName.ToUpperInvariant();
        int titleSize = 20;
        int titleY = iconY + iconSize + 12;

        const float bodySpacing = 0.55f;
        int bodySize = 15;
        int bodyLineHeight = 20;
        int textY = titleY + 26;
        string body = GetItemDialogBody(isGround, itemAction, canDrink, canFill);
        var (bodyLines, _) = GameTextLayout.WrapForBox(body, font, bodySize, bodySpacing, innerW, bodyLineHeight);
        foreach (string line in bodyLines)
            textY += string.IsNullOrEmpty(line) ? bodyLineHeight / 2 : bodyLineHeight;

        if (GameItems.IsBuildingMaterial(_dialogItemName))
            textY += 22;

        const int btnH = 36;
        const int btnGap = 6;
        int btnY = textY + 10;
        int buttonCount = isGround ? 1
            : canDrink && canFill ? 3
            : canAct ? 2
            : 1;
        int panelBottom = btnY + buttonCount * btnH + (buttonCount - 1) * btnGap + panelPad;
        int panelH = panelBottom - startY;
        var panelRect = new Rectangle(x, startY, available, panelH);

        Raylib.DrawRectangleRounded(panelRect, 0.06f, 8, Palette.CardBg);
        Raylib.DrawRectangleRoundedLines(panelRect, 0.06f, 8, 1.5f, Palette.CardBorder);

        var iconFrame = new Rectangle(iconX - 2, iconY - 2, iconSize + 4, iconSize + 4);
        Raylib.DrawRectangleRounded(iconFrame, 0.12f, 8, new Color(22, 20, 17, 255));
        Raylib.DrawRectangleRoundedLines(iconFrame, 0.12f, 8, 1f, Palette.SubtleBorder);
        DrawItemIcon(_dialogItemName, new Rectangle(iconX, iconY, iconSize, iconSize), Color.WHITE,
            GetDialogSlotIndex(), GetDialogChargesOverride());

        int titleW = (int)Raylib.MeasureTextEx(font, title, titleSize, 0.7f).X;
        Raylib.DrawTextEx(font, title,
            new Vector2(innerX + (innerW - titleW) / 2, titleY),
            titleSize, 0.7f, Palette.TextPrimary);

        textY = titleY + 26;
        foreach (string line in bodyLines)
        {
            Raylib.DrawTextEx(font, line, new Vector2(innerX, textY), bodySize, bodySpacing, Palette.TextSecondary);
            textY += string.IsNullOrEmpty(line) ? bodyLineHeight / 2 : bodyLineHeight;
        }

        if (ShopCatalogs.GetItemHintForItem(_dialogItemName) is string shopHint)
        {
            textY += 4;
            Raylib.DrawTextEx(font, shopHint, new Vector2(innerX, textY), 13, 0.45f, Palette.TextDim);
            textY += 18;
        }

        btnY = textY + 10;
        _dialogSecondaryActionRect = new Rectangle(0, 0, 0, 0);
        _dialogDropRect = new Rectangle(0, 0, 0, 0);

        if (isGround)
        {
            _dialogActionRect = new Rectangle(innerX, btnY, innerW, btnH);
            GameDialogUi.DrawDialogButton(_dialogActionRect, "PICK UP", _dialogActionHovered, font);
        }
        else if (canDrink && canFill)
        {
            _dialogActionRect = new Rectangle(innerX, btnY, innerW, btnH);
            btnY += btnH + btnGap;
            _dialogSecondaryActionRect = new Rectangle(innerX, btnY, innerW, btnH);
            btnY += btnH + btnGap;
            _dialogDropRect = new Rectangle(innerX, btnY, innerW, btnH);
            GameDialogUi.DrawDialogButton(_dialogActionRect, "DRINK", _dialogActionHovered, font);
            GameDialogUi.DrawDialogButton(_dialogSecondaryActionRect, "FILL", _dialogSecondaryActionHovered, font);
            GameDialogUi.DrawDialogButton(_dialogDropRect, "DROP", _dialogDropHovered, font);
        }
        else if (canAct)
        {
            string actionLabel = canDrink
                ? "DRINK"
                : canFill
                    ? "FILL"
                    : GetDialogItemActionLabel(itemAction);
            _dialogActionRect = new Rectangle(innerX, btnY, innerW, btnH);
            btnY += btnH + btnGap;
            _dialogDropRect = new Rectangle(innerX, btnY, innerW, btnH);
            GameDialogUi.DrawDialogButton(_dialogActionRect, actionLabel, _dialogActionHovered, font);
            GameDialogUi.DrawDialogButton(_dialogDropRect, "DROP", _dialogDropHovered, font);
        }
        else
        {
            _dialogActionRect = new Rectangle(0, 0, 0, 0);
            _dialogDropRect = new Rectangle(innerX, btnY, innerW, btnH);
            GameDialogUi.DrawDialogButton(_dialogDropRect, "DROP", _dialogDropHovered, font);
        }

        return panelBottom;
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
                : "Shelter pitched")
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

        string title = ShopCatalogs.GetTitle(_shopBuyKind);
        int titleSize = 28;
        Raylib.DrawTextEx(font, title,
            new Vector2(panelX + 24, panelY + 18),
            titleSize, 0.8f, Palette.TextPrimary);

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

        string[] shopEntries = ShopCatalogs.GetEntries(_shopBuyKind);
        for (int i = 0; i < shopEntries.Length; i++)
        {
            string name = shopEntries[i];

            int rowY = contentTop + i * rowHeight;
            bool canBuy = CanBuyShopItem(i);
            bool rowHovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), _storeBuyItemRects[i]);
            bool rowHighlighted = i == _storeBuyHighlightedIndex;
            bool rowConfirmed = _storeBuyDetailIndex >= 0 && i == _storeBuyDetailIndex;

            _storeBuyItemRects[i] = new Rectangle(listX, rowY, listW, rowHeight - 4);

            if (rowConfirmed)
                Raylib.DrawRectangle(listX, rowY, listW, rowHeight - 4, new Color(62, 58, 48, 200));
            else if (rowHovered || rowHighlighted)
                Raylib.DrawRectangle(listX, rowY, listW, rowHeight - 4, new Color(48, 46, 40, 180));

            Color tint = canBuy ? Color.WHITE : new Color(120, 118, 112, 255);
            int iconY = rowY + (rowHeight - 4 - iconSize) / 2;
            Raylib.DrawRectangle(listX + 6, iconY - 1, iconSize + 2, iconSize + 2, new Color(18, 17, 15, 255));
            DrawItemIcon(name, new Rectangle(listX + 7, iconY, iconSize, iconSize), tint);

            Color nameColor = canBuy ? Palette.TextPrimary : Palette.TextMuted;
            Raylib.DrawTextEx(font, name, new Vector2(listX + 42, rowY + 6), 18, 0.6f, nameColor);
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

        string[] shopEntries = ShopCatalogs.GetEntries(_shopBuyKind);
        string name = shopEntries[_storeBuyDetailIndex];
        bool canBuy = CanBuyShopItem(_storeBuyDetailIndex);

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

        int textY = iconY + iconSize + 36;
        string flavor = ShopCatalogs.GetFlavorText(_shopBuyKind, name);
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
        string itemHint = ShopCatalogs.FormatItemHint(_shopBuyKind, name);
        Raylib.DrawTextEx(font, itemHint, new Vector2(x + 4, textY), 15, 0.5f, Palette.TextDim);
        textY += 22;

        if (!canBuy)
        {
            string blocked = _shopBuyKind == ShopKind.GasStation &&
                string.Equals(name, GameItems.GasCan, StringComparison.OrdinalIgnoreCase) &&
                HasBackpackItem(GameItems.GasCan)
                ? "You already have a gas can."
                : "Backpack is full.";
            Raylib.DrawTextEx(font, blocked, new Vector2(x + 4, textY), 15, 0.5f, new Color(200, 130, 110, 255));
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
        int btnH = 32;
        int takeAllW = 112;
        int closeW = 88;
        int btnGap = 8;
        int btnRowW = takeAllW + btnGap + closeW;
        int btnY = panelBottom - btnH;
        int btnRowX = listX + (listW - btnRowW) / 2;
        int dividerX = listX + listW + 8;
        int detailX = dividerX + 9;
        int detailW = panelX + panelW - 20 - detailX;

        Raylib.DrawLine(dividerX, contentTop, dividerX, panelBottom, Palette.SubtleBorder);
        Raylib.DrawLine(listX, btnY - 6, listX + listW, btnY - 6, Palette.SubtleBorder);

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

        _gloveBoxTakeAllRect = new Rectangle(btnRowX, btnY, takeAllW, btnH);
        _gloveBoxCloseRect = new Rectangle(btnRowX + takeAllW + btnGap, btnY, closeW, btnH);
        GameDialogUi.DrawDialogButton(_gloveBoxTakeAllRect, "TAKE ALL", _gloveBoxTakeAllHovered, font);
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

    // =====================================================================
    // WAREHOUSE CRATE LOOT (modal — take items after prying open)
    // =====================================================================
    private void DrawCrateLootMenu()
    {
        RefreshCrateLootVisibleList();

        int screenW = _screenWidth;
        int screenH = _screenHeight;

        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 160));

        int panelW = 720;
        int panelH = 360;
        int panelX = (screenW - panelW) / 2;
        int panelY = (screenH - panelH) / 2 - 10;

        _crateLootPanelRect = new Rectangle(panelX, panelY, panelW, panelH);

        Raylib.DrawRectangle(panelX, panelY, panelW, panelH, Palette.CardBg);
        Raylib.DrawRectangleLines(panelX, panelY, panelW, panelH, Palette.CardBorder);

        Font font = _uiFont;

        Raylib.DrawTextEx(font, WarehouseCrateDialog.Title,
            new Vector2(panelX + 24, panelY + 18), 28, 0.8f, Palette.TextPrimary);

        string hint = "Take what you can carry";
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
        int btnH = 32;
        int takeAllW = 112;
        int closeW = 88;
        int btnGap = 8;
        int btnRowW = takeAllW + btnGap + closeW;
        int btnY = panelBottom - btnH;
        int btnRowX = listX + (listW - btnRowW) / 2;
        int dividerX = listX + listW + 8;
        int detailX = dividerX + 9;
        int detailW = panelX + panelW - 20 - detailX;

        Raylib.DrawLine(dividerX, contentTop, dividerX, panelBottom, Palette.SubtleBorder);
        Raylib.DrawLine(listX, btnY - 6, listX + listW, btnY - 6, Palette.SubtleBorder);

        int rowHeight = 44;
        const int iconSize = 28;

        for (int i = 0; i < _crateLootVisibleCount; i++)
        {
            int catalogIndex = _crateLootVisibleCatalogIndices[i];
            var entry = WarehouseCrateLootCatalog.Entries[catalogIndex];
            bool canTake = CanTakeCrateLootItem(catalogIndex);

            int rowY = contentTop + i * rowHeight;
            _crateLootItemRects[i] = new Rectangle(listX, rowY, listW, rowHeight - 4);
            bool rowHovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), _crateLootItemRects[i]);
            bool rowHighlighted = i == _crateLootHighlightedIndex;
            bool rowConfirmed = _crateLootDetailIndex >= 0 && i == _crateLootDetailIndex;

            if (rowConfirmed)
                Raylib.DrawRectangle(listX, rowY, listW, rowHeight - 4, new Color(62, 58, 48, 200));
            else if (rowHovered || rowHighlighted)
                Raylib.DrawRectangle(listX, rowY, listW, rowHeight - 4, new Color(48, 46, 40, 180));

            Color tint = canTake ? Color.WHITE : new Color(120, 118, 112, 255);
            int iconY = rowY + (rowHeight - 4 - iconSize) / 2;
            Raylib.DrawRectangle(listX + 6, iconY - 1, iconSize + 2, iconSize + 2, new Color(18, 17, 15, 255));
            DrawItemIcon(entry.IconItemName, new Rectangle(listX + 7, iconY, iconSize, iconSize), tint);

            Raylib.DrawTextEx(font, entry.Name, new Vector2(listX + 42, rowY + 6), 18, 0.6f, Palette.TextPrimary);
        }

        DrawCrateLootDetailPanel(font, detailX, contentTop, detailW, panelBottom - contentTop);

        if (_crateLootFeedbackTimer > 0f && !string.IsNullOrEmpty(_crateLootFeedback))
        {
            int fbW = (int)Raylib.MeasureTextEx(font, _crateLootFeedback, 17, 0.5f).X;
            Raylib.DrawTextEx(font, _crateLootFeedback,
                new Vector2(detailX + (detailW - fbW) / 2, panelBottom - 48),
                17, 0.5f, Palette.TextSecondary);
        }

        _crateLootTakeAllRect = new Rectangle(btnRowX, btnY, takeAllW, btnH);
        _crateLootCloseRect = new Rectangle(btnRowX + takeAllW + btnGap, btnY, closeW, btnH);
        GameDialogUi.DrawDialogButton(_crateLootTakeAllRect, "TAKE ALL", _crateLootTakeAllHovered, font);
        GameDialogUi.DrawDialogButton(_crateLootCloseRect, "CLOSE", _crateLootCloseHovered, font);
    }

    private void DrawCrateLootDetailPanel(Font font, int x, int y, int w, int h)
    {
        if (_crateLootDetailIndex < 0)
        {
            string hint = "Select an item";
            int hintSize = 20;
            int hintW = (int)Raylib.MeasureTextEx(font, hint, hintSize, 0.6f).X;
            Raylib.DrawTextEx(font, hint,
                new Vector2(x + (w - hintW) / 2, y + h / 2 - 12),
                hintSize, 0.6f, Palette.TextMuted);
            _crateLootPickupRect = new Rectangle(0, 0, 0, 0);
            return;
        }

        int catalogIndex = GetCrateLootCatalogIndexFromVisibleIndex(_crateLootDetailIndex);
        if (catalogIndex < 0)
        {
            _crateLootDetailIndex = -1;
            _crateLootPickupRect = new Rectangle(0, 0, 0, 0);
            return;
        }

        var entry = WarehouseCrateLootCatalog.Entries[catalogIndex];
        bool canTake = CanTakeCrateLootItem(catalogIndex);

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
        _crateLootPickupRect = new Rectangle(btnX, btnY, btnW, btnH);

        if (canTake)
            GameDialogUi.DrawDialogButton(_crateLootPickupRect, "TAKE", _crateLootPickupHovered, font);
        else
        {
            Raylib.DrawRectangleRec(_crateLootPickupRect, new Color(24, 26, 30, 255));
            Raylib.DrawRectangleLinesEx(_crateLootPickupRect, 1f, Palette.SubtleBorder);
            int labelSize = 18;
            int labelW = (int)Raylib.MeasureTextEx(font, "TAKE", labelSize, 0.55f).X;
            Raylib.DrawTextEx(font, "TAKE",
                new Vector2(btnX + (btnW - labelW) / 2f, btnY + 8),
                labelSize, 0.55f, Palette.TextMuted);
        }
    }

    // =====================================================================
    // WAREHOUSE BODY LOOT (modal — search bratdvas, glove-box layout)
    // =====================================================================
    private void DrawBodyLootMenu()
    {
        if (_activeBodyIndex < 0 || _activeBodyIndex >= WarehouseBodyLootCatalog.BodyCount)
            return;

        RefreshBodyLootVisibleList();

        var body = WarehouseBodyLootCatalog.Bodies[_activeBodyIndex];
        int screenW = _screenWidth;
        int screenH = _screenHeight;

        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 160));

        int panelW = 720;
        int panelH = 360;
        int panelX = (screenW - panelW) / 2;
        int panelY = (screenH - panelH) / 2 - 10;

        _bodyLootPanelRect = new Rectangle(panelX, panelY, panelW, panelH);

        Raylib.DrawRectangle(panelX, panelY, panelW, panelH, Palette.CardBg);
        Raylib.DrawRectangleLines(panelX, panelY, panelW, panelH, Palette.CardBorder);

        Font font = _uiFont;

        Raylib.DrawTextEx(font, body.Title,
            new Vector2(panelX + 24, panelY + 18), 28, 0.8f, Palette.TextPrimary);

        int hintW = (int)Raylib.MeasureTextEx(font, body.SearchHint, 18, 0.55f).X;
        Raylib.DrawTextEx(font, body.SearchHint,
            new Vector2(panelX + panelW - 24 - hintW, panelY + 22),
            18, 0.55f, Palette.TextSecondary);

        int headerBottom = panelY + 50;
        Raylib.DrawLine(panelX + 20, headerBottom, panelX + panelW - 20, headerBottom, Palette.SubtleBorder);

        int listX = panelX + 16;
        int listW = 268;
        int contentTop = headerBottom + 10;
        int panelBottom = panelY + panelH - 12;
        int btnH = 32;
        int takeAllW = 112;
        int closeW = 88;
        int btnGap = 8;
        int btnRowW = takeAllW + btnGap + closeW;
        int btnY = panelBottom - btnH;
        int btnRowX = listX + (listW - btnRowW) / 2;
        int dividerX = listX + listW + 8;
        int detailX = dividerX + 9;
        int detailW = panelX + panelW - 20 - detailX;

        Raylib.DrawLine(dividerX, contentTop, dividerX, panelBottom, Palette.SubtleBorder);
        Raylib.DrawLine(listX, btnY - 6, listX + listW, btnY - 6, Palette.SubtleBorder);

        int rowHeight = 44;
        const int iconSize = 28;

        for (int i = 0; i < _bodyLootVisibleCount; i++)
        {
            int itemIndex = _bodyLootVisibleCatalogIndices[i];
            var entry = body.Items[itemIndex];
            bool canTake = CanTakeBodyLootItem(itemIndex);

            int rowY = contentTop + i * rowHeight;
            _bodyLootItemRects[i] = new Rectangle(listX, rowY, listW, rowHeight - 4);
            bool rowHovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), _bodyLootItemRects[i]);
            bool rowHighlighted = i == _bodyLootHighlightedIndex;
            bool rowConfirmed = _bodyLootDetailIndex >= 0 && i == _bodyLootDetailIndex;

            if (rowConfirmed)
                Raylib.DrawRectangle(listX, rowY, listW, rowHeight - 4, new Color(62, 58, 48, 200));
            else if (rowHovered || rowHighlighted)
                Raylib.DrawRectangle(listX, rowY, listW, rowHeight - 4, new Color(48, 46, 40, 180));

            Color tint = canTake ? Color.WHITE : new Color(120, 118, 112, 255);
            int iconY = rowY + (rowHeight - 4 - iconSize) / 2;
            Raylib.DrawRectangle(listX + 6, iconY - 1, iconSize + 2, iconSize + 2, new Color(18, 17, 15, 255));
            DrawItemIcon(entry.IconItemName, new Rectangle(listX + 7, iconY, iconSize, iconSize), tint);

            Raylib.DrawTextEx(font, entry.Name, new Vector2(listX + 42, rowY + 6), 18, 0.6f, Palette.TextPrimary);
        }

        DrawBodyLootDetailPanel(font, detailX, contentTop, detailW, panelBottom - contentTop);

        if (_bodyLootFeedbackTimer > 0f && !string.IsNullOrEmpty(_bodyLootFeedback))
        {
            int fbW = (int)Raylib.MeasureTextEx(font, _bodyLootFeedback, 17, 0.5f).X;
            Raylib.DrawTextEx(font, _bodyLootFeedback,
                new Vector2(detailX + (detailW - fbW) / 2, panelBottom - 48),
                17, 0.5f, Palette.TextSecondary);
        }

        _bodyLootTakeAllRect = new Rectangle(btnRowX, btnY, takeAllW, btnH);
        _bodyLootCloseRect = new Rectangle(btnRowX + takeAllW + btnGap, btnY, closeW, btnH);
        GameDialogUi.DrawDialogButton(_bodyLootTakeAllRect, "TAKE ALL", _bodyLootTakeAllHovered, font);
        GameDialogUi.DrawDialogButton(_bodyLootCloseRect, "CLOSE", _bodyLootCloseHovered, font);
    }

    private void DrawBodyLootDetailPanel(Font font, int x, int y, int w, int h)
    {
        if (_activeBodyIndex < 0 || _bodyLootDetailIndex < 0)
        {
            string hint = "Select an item";
            int hintSize = 20;
            int hintW = (int)Raylib.MeasureTextEx(font, hint, hintSize, 0.6f).X;
            Raylib.DrawTextEx(font, hint,
                new Vector2(x + (w - hintW) / 2, y + h / 2 - 12),
                hintSize, 0.6f, Palette.TextMuted);
            _bodyLootPickupRect = new Rectangle(0, 0, 0, 0);
            return;
        }

        int itemIndex = GetBodyLootItemIndexFromVisibleIndex(_bodyLootDetailIndex);
        if (itemIndex < 0)
        {
            _bodyLootDetailIndex = -1;
            _bodyLootPickupRect = new Rectangle(0, 0, 0, 0);
            return;
        }

        var entry = WarehouseBodyLootCatalog.Bodies[_activeBodyIndex].Items[itemIndex];
        bool canTake = CanTakeBodyLootItem(itemIndex);

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
        _bodyLootPickupRect = new Rectangle(btnX, btnY, btnW, btnH);

        if (canTake)
            GameDialogUi.DrawDialogButton(_bodyLootPickupRect, "TAKE", _bodyLootPickupHovered, font);
        else
        {
            Raylib.DrawRectangleRec(_bodyLootPickupRect, new Color(24, 26, 30, 255));
            Raylib.DrawRectangleLinesEx(_bodyLootPickupRect, 1f, Palette.SubtleBorder);
            int labelSize = 18;
            int labelW = (int)Raylib.MeasureTextEx(font, "TAKE", labelSize, 0.55f).X;
            Raylib.DrawTextEx(font, "TAKE",
                new Vector2(btnX + (btnW - labelW) / 2f, btnY + 8),
                labelSize, 0.55f, Palette.TextMuted);
        }
    }

    private void DrawWarehouseBodyHotspots(int artX, int artY, int artW, int artH)
    {
        var artBounds = new Rectangle(artX, artY, artW, artH);

        for (int i = 0; i < WarehouseBodyLootCatalog.BodyCount; i++)
        {
            if (!BodyHasRemainingLoot(i))
                continue;

            var body = WarehouseBodyLootCatalog.Bodies[i];
            Rectangle r = SceneRegion.ToScreenRect(
                body.RegionX,
                body.RegionY,
                body.RegionW,
                body.RegionH,
                artBounds);

            bool hovered = _hoveredBodyIndex == i;
            Color fill = hovered
                ? new Color(200, 185, 120, 36)
                : new Color(200, 185, 120, 14);
            Raylib.DrawRectangleRec(r, fill);
            Color border = hovered
                ? new Color(220, 200, 130, 200)
                : new Color(180, 165, 110, 90);
            Raylib.DrawRectangleLinesEx(r, hovered ? 2f : 1f, border);

            if (hovered)
            {
                const string label = "SEARCH BODY";
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
    }

    private void DrawWarehouseTruckHotspot(int artX, int artY, int artW, int artH)
    {
        var artBounds = new Rectangle(artX, artY, artW, artH);
        float truckW = WarehouseAftermathHotspots.TruckX2 - WarehouseAftermathHotspots.TruckX1;
        float truckH = WarehouseAftermathHotspots.TruckY2 - WarehouseAftermathHotspots.TruckY1;
        Rectangle r = SceneRegion.ToScreenRect(
            WarehouseAftermathHotspots.TruckX1,
            WarehouseAftermathHotspots.TruckY1,
            truckW,
            truckH,
            artBounds);

        bool hovered = _warehouseTruckHotspotHovered;
        Color fill = hovered
            ? new Color(140, 165, 200, 40)
            : new Color(140, 165, 200, 16);
        Raylib.DrawRectangleRec(r, fill);
        Color border = hovered
            ? new Color(170, 195, 230, 200)
            : new Color(120, 145, 180, 90);
        Raylib.DrawRectangleLinesEx(r, hovered ? 2f : 1f, border);

        if (hovered)
        {
            const string label = "TRUCK";
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

    private void DrawTruckGasGaugeHotspot(int artX, int artY, int artW, int artH)
    {
        if (_phase is not (Phase.DeliveryTruck or Phase.WarehouseTruck))
            return;

        var artBounds = new Rectangle(artX, artY, artW, artH);
        Rectangle r = ComputeTruckGasGaugeClickRect(_phase, artBounds);

        bool hovered = _gasGaugeHotspotHovered;
        Color fill = hovered
            ? new Color(200, 185, 120, 40)
            : new Color(200, 185, 120, 16);
        Raylib.DrawRectangleRec(r, fill);
        Color border = hovered
            ? new Color(220, 200, 130, 200)
            : new Color(180, 165, 110, 90);
        Raylib.DrawRectangleLinesEx(r, hovered ? 2f : 1f, border);

        if (hovered)
        {
            const string label = "FUEL GAUGE";
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

    private void DrawWarehouseClosedDoorOverlay(int artX, int artY, int artW, int artH)
    {
        if (_warehouseClosedDoorTexture.Id == 0)
            return;

        var artBounds = new Rectangle(artX, artY, artW, artH);
        float doorW = WarehouseAftermathHotspots.DoorX2 - WarehouseAftermathHotspots.DoorX1;
        float doorH = WarehouseAftermathHotspots.DoorY2 - WarehouseAftermathHotspots.DoorY1;
        Rectangle dst = SceneRegion.ToScreenRect(
            WarehouseAftermathHotspots.DoorX1,
            WarehouseAftermathHotspots.DoorY1,
            doorW,
            doorH,
            artBounds);

        Rectangle src = new Rectangle(0, 0, _warehouseClosedDoorTexture.Width, _warehouseClosedDoorTexture.Height);
        Color tint = GetOutdoorTimeOfDayTint();
        Raylib.DrawTexturePro(_warehouseClosedDoorTexture, src, dst, Vector2.Zero, 0f, tint);
    }

    private void DrawWarehouseLockHotspot(int artX, int artY, int artW, int artH)
    {
        var artBounds = new Rectangle(artX, artY, artW, artH);
        float lockW = WarehouseAftermathHotspots.LockX2 - WarehouseAftermathHotspots.LockX1;
        float lockH = WarehouseAftermathHotspots.LockY2 - WarehouseAftermathHotspots.LockY1;
        Rectangle r = SceneRegion.ToScreenRect(
            WarehouseAftermathHotspots.LockX1,
            WarehouseAftermathHotspots.LockY1,
            lockW,
            lockH,
            artBounds);

        bool hovered = _warehouseLockHotspotHovered;
        Color fill = hovered
            ? new Color(140, 165, 200, 40)
            : new Color(140, 165, 200, 16);
        Raylib.DrawRectangleRec(r, fill);
        Color border = hovered
            ? new Color(170, 195, 230, 200)
            : new Color(120, 145, 180, 90);
        Raylib.DrawRectangleLinesEx(r, hovered ? 2f : 1f, border);

        if (hovered)
        {
            const string label = "KEYPAD";
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

    private void DrawWarehouseDoorHotspot(int artX, int artY, int artW, int artH)
    {
        var artBounds = new Rectangle(artX, artY, artW, artH);
        float doorW = WarehouseAftermathHotspots.DoorX2 - WarehouseAftermathHotspots.DoorX1;
        float doorH = WarehouseAftermathHotspots.DoorY2 - WarehouseAftermathHotspots.DoorY1;
        Rectangle r = SceneRegion.ToScreenRect(
            WarehouseAftermathHotspots.DoorX1,
            WarehouseAftermathHotspots.DoorY1,
            doorW,
            doorH,
            artBounds);

        bool hovered = _warehouseDoorHotspotHovered;
        Color fill = hovered
            ? new Color(140, 165, 200, 40)
            : new Color(140, 165, 200, 16);
        Raylib.DrawRectangleRec(r, fill);
        Color border = hovered
            ? new Color(170, 195, 230, 200)
            : new Color(120, 145, 180, 90);
        Raylib.DrawRectangleLinesEx(r, hovered ? 2f : 1f, border);

        if (hovered)
        {
            string label = _warehouseKeypad.IsUnlocked ? "ENTER" : "DOOR";
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

    private void DrawCafeBorisHotspot(int artX, int artY, int artW, int artH)
    {
        if (_phase != Phase.Cafe)
            return;

        var artBounds = new Rectangle(artX, artY, artW, artH);
        UpdateCafeBorisClickRect(artBounds);
        Rectangle r = _cafeBorisClickRect;

        bool hovered = _cafeBorisHotspotHovered;
        Color fill = hovered
            ? new Color(200, 185, 120, 40)
            : new Color(200, 185, 120, 16);
        Raylib.DrawRectangleRec(r, fill);
        Color border = hovered
            ? new Color(220, 200, 130, 200)
            : new Color(180, 165, 110, 90);
        Raylib.DrawRectangleLinesEx(r, hovered ? 2f : 1f, border);

        if (hovered)
        {
            const string label = "BORIS";
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

    private void DrawOpeningWindowHotspot(int artX, int artY, int artW, int artH)
    {
        var artBounds = new Rectangle(artX, artY, artW, artH);
        float windowW = OpeningHotspots.WindowX2 - OpeningHotspots.WindowX1;
        float windowH = OpeningHotspots.WindowY2 - OpeningHotspots.WindowY1;
        Rectangle r = SceneRegion.ToScreenRect(
            OpeningHotspots.WindowX1,
            OpeningHotspots.WindowY1,
            windowW,
            windowH,
            artBounds);

        bool hovered = _openingWindowHotspotHovered;
        Color fill = hovered
            ? new Color(140, 165, 200, 40)
            : new Color(140, 165, 200, 16);
        Raylib.DrawRectangleRec(r, fill);
        Color border = hovered
            ? new Color(170, 195, 230, 200)
            : new Color(120, 145, 180, 90);
        Raylib.DrawRectangleLinesEx(r, hovered ? 2f : 1f, border);

        if (hovered)
        {
            const string label = "WINDOW";
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

    private void DrawWarehouseInteriorExitHotspot(int artX, int artY, int artW, int artH)
    {
        var artBounds = new Rectangle(artX, artY, artW, artH);
        float exitW = WarehouseInteriorHotspots.ExitX2 - WarehouseInteriorHotspots.ExitX1;
        float exitH = WarehouseInteriorHotspots.ExitY2 - WarehouseInteriorHotspots.ExitY1;
        Rectangle r = SceneRegion.ToScreenRect(
            WarehouseInteriorHotspots.ExitX1,
            WarehouseInteriorHotspots.ExitY1,
            exitW,
            exitH,
            artBounds);

        bool hovered = _warehouseInteriorExitHotspotHovered;
        Color fill = hovered
            ? new Color(140, 165, 200, 40)
            : new Color(140, 165, 200, 16);
        Raylib.DrawRectangleRec(r, fill);
        Color border = hovered
            ? new Color(170, 195, 230, 200)
            : new Color(120, 145, 180, 90);
        Raylib.DrawRectangleLinesEx(r, hovered ? 2f : 1f, border);

        if (hovered)
        {
            const string label = "EXIT";
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

    private void DrawWarehouseCrateHotspot(int artX, int artY, int artW, int artH)
    {
        var artBounds = new Rectangle(artX, artY, artW, artH);
        float crateW = WarehouseInteriorHotspots.CrateX2 - WarehouseInteriorHotspots.CrateX1;
        float crateH = WarehouseInteriorHotspots.CrateY2 - WarehouseInteriorHotspots.CrateY1;
        Rectangle r = SceneRegion.ToScreenRect(
            WarehouseInteriorHotspots.CrateX1,
            WarehouseInteriorHotspots.CrateY1,
            crateW,
            crateH,
            artBounds);

        bool hovered = _warehouseCrateHotspotHovered;
        Color fill = hovered
            ? new Color(140, 165, 200, 40)
            : new Color(140, 165, 200, 16);
        Raylib.DrawRectangleRec(r, fill);
        Color border = hovered
            ? new Color(170, 195, 230, 200)
            : new Color(120, 145, 180, 90);
        Raylib.DrawRectangleLinesEx(r, hovered ? 2f : 1f, border);

        if (hovered)
        {
            string label = !_warehouseCrateOpened
                ? "CRATE"
                : CrateHasRemainingLoot()
                    ? "SEARCH"
                    : "OPENED";
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

    private void DrawWarehouseCrateDialog()
    {
        int screenW = _screenWidth;
        int screenH = _screenHeight;

        GameDialogUi.DrawModalBackdrop(screenW, screenH);

        bool hasCrowbar = HasBackpackItem(GameItems.Crowbar);
        string body = WarehouseCrateDialog.GetBodyText(hasCrowbar, _warehouseCrateOpened);

        Font font = _uiFont;
        int panelW = 440;
        const float bodySpacing = 0.6f;
        int bodySize = 16;
        int bodyLineHeight = 22;
        int textMaxW = panelW - 48;
        var (bodyLines, bodyHeight) = GameTextLayout.WrapForBox(body, font, bodySize, bodySpacing, textMaxW, bodyLineHeight);
        int panelH = Math.Max(220, 124 + bodyHeight + 60);
        int panelX = (screenW - panelW) / 2;
        int panelY = (screenH - panelH) / 2 - 20;

        _warehouseCratePanelRect = new Rectangle(panelX, panelY, panelW, panelH);

        Raylib.DrawRectangle(panelX, panelY, panelW, panelH, Palette.CardBg);
        Raylib.DrawRectangleLines(panelX, panelY, panelW, panelH, Palette.CardBorder);

        string title = WarehouseCrateDialog.Title;
        int titleSize = 26;
        int titleW = (int)Raylib.MeasureTextEx(font, title, titleSize, 0.8f).X;
        Raylib.DrawTextEx(font, title,
            new Vector2(panelX + (panelW - titleW) / 2, panelY + 18),
            titleSize, 0.8f, Palette.TextPrimary);

        Raylib.DrawLine(panelX + 40, panelY + 52, panelX + panelW - 40, panelY + 52, Palette.SubtleBorder);

        int textY = panelY + 64;
        foreach (string line in bodyLines)
        {
            int lineW = (int)Raylib.MeasureTextEx(font, line, bodySize, bodySpacing).X;
            Raylib.DrawTextEx(font, line,
                new Vector2(panelX + (panelW - lineW) / 2, textY),
                bodySize, bodySpacing, Palette.TextSecondary);
            textY += string.IsNullOrEmpty(line) ? bodyLineHeight / 2 : bodyLineHeight;
        }

        int btnH = 36;
        int btnY = panelY + panelH - 52;
        int btnW = 100;
        int startX = panelX + (panelW - btnW) / 2;

        _warehouseCrateCloseRect = new Rectangle(startX, btnY, btnW, btnH);
        GameDialogUi.DrawDialogButton(_warehouseCrateCloseRect, "CLOSE", _warehouseCrateCloseHovered, font);
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
            case Phase.WarehouseAftermath:
            case Phase.WarehouseInterior:
            case Phase.GasStation:
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

        if (_showStoreBuyMenu)
        {
            DrawStoreBuyMenu();
        }

        if (_showGloveBoxMenu)
        {
            DrawGloveBoxMenu();
        }

        if (_showCrateLootMenu)
        {
            DrawCrateLootMenu();
        }

        if (_showBodyLootMenu)
        {
            DrawBodyLootMenu();
        }

        if (_warehouseKeypad.IsOpen)
        {
            _warehouseKeypad.Draw(_uiFont, _screenWidth, _screenHeight);
        }

        if (_foldedPaperReader.IsOpen)
        {
            _foldedPaperReader.Draw(_uiFont, GetActiveNoteReadTexture(), _screenWidth, _screenHeight);
        }

        if (_gasGaugeViewer.IsOpen)
        {
            _gasGaugeViewer.Draw(_uiFont, _gasGaugeFuel, _screenWidth, _screenHeight);
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

        if (_showWarehouseCrateDialog)
        {
            DrawWarehouseCrateDialog();
        }

        if (_showQuitConfirm)
        {
            DrawQuitConfirmDialog();
        }


        if (_showControllerDebug)
        {
            DrawControllerDebugScreen();
            DrawHistoryButtons();
            DrawTopRightButtons();
        }

        if (_sceneAreaSelect.IsActive)
            DrawSceneAreaSelectOverlay();

        if (_itemUseActive)
            DrawItemUseOverlay();

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

        DrawHistoryButtons();

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

        float rightEdge = TopRightToolbarLeftEdge() - 10f;
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
    // LEFT SIDEBAR — Backpack
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
        int cy = y + 28;
        cy = DrawBackpack(cy, tx);

        if (_showItemDialog)
        {
            cy += 16;
            DrawItemPanel(cy, tx);
        }
    }

    // =====================================================================
    // RIGHT PANEL
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
        DrawBuildSidebarButton(cy, tx);
        cy += 44;

        if (GamePhase.IsForestSurvival(_phase))
        {
            DrawHuntSidebarButton(cy, tx);
            cy += 44;
            DrawForageSidebarButton(cy, tx);
            cy += 44;
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

        if (GamePhase.ShowsSceneNarrative(_phase))
        {
            const int logTopGap = 16;
            const int logBottomGap = 16;
            int logY = cy + logTopGap;
            int availableW = w - GameConstants.SidebarPadding * 2;
            int maxLogHeight = quitY - logY - logBottomGap;

            if (maxLogHeight >= NarrativeMinHeight)
                DrawSidebarNarrative(tx, logY, availableW, maxLogHeight, GetSceneNarrative());
        }
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
        string cap = $"{filled}/{BackpackSlotCount}";
        int capW = (int)Raylib.MeasureTextEx(font, cap, 14, 0.5f).X;
        Raylib.DrawTextEx(font, cap,
            new Vector2(x + available - capW, startY + 1), 14, 0.5f, Palette.TextDim);

        startY += 18;

        // Subtle underline
        Raylib.DrawLine(x, startY - 2, x + 42, startY - 2, Palette.SubtleBorder);
        startY += 8;

        // === Visual backpack body ===
        const int cols = BackpackColumns;
        const int rows = BackpackRows;
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
                bool selected = _showItemDialog && !IsDroppedItemDialog && _dialogItemIndex == idx;

                // Pocket background
                var pocket = occupied
                    ? new Color(58, 50, 40, 255)
                    : new Color(18, 17, 15, 255);
                Raylib.DrawRectangle(sx, sy, slot, slot, pocket);

                // Inner border (pocket stitching)
                var pocketBorder = selected
                    ? Palette.ButtonSelectedBorder
                    : occupied ? new Color(75, 62, 48, 255) : Palette.SubtleBorder;
                Raylib.DrawRectangleLines(sx + 1, sy + 1, slot - 2, slot - 2, pocketBorder);
                if (selected)
                    Raylib.DrawRectangleLinesEx(new Rectangle(sx, sy, slot, slot), 1.5f, Palette.ButtonSelectedBorder);

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
            Phase.WarehouseAftermath => WarehouseAftermathNarrative,
            Phase.WarehouseInterior  => WarehouseInteriorNarrative,
            Phase.GasStation         => GasStationNarrative,
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

        if (_phase == Phase.WarehouseAftermath && !_warehouseKeypad.IsUnlocked)
            DrawWarehouseClosedDoorOverlay(artX, artY, artW, artH);

        if (GamePhase.IsInTruckCab(_phase))
        {
            DrawTruckGloveCompartmentHotspot(artX, artY, artW, artH);
            DrawTruckGasGaugeHotspot(artX, artY, artW, artH);
        }

        if (_phase == Phase.WarehouseAftermath)
        {
            DrawWarehouseBodyHotspots(artX, artY, artW, artH);
            DrawWarehouseDoorHotspot(artX, artY, artW, artH);
            DrawWarehouseLockHotspot(artX, artY, artW, artH);
            DrawWarehouseTruckHotspot(artX, artY, artW, artH);
        }

        if (_phase == Phase.WarehouseInterior)
        {
            DrawWarehouseInteriorExitHotspot(artX, artY, artW, artH);
            DrawWarehouseCrateHotspot(artX, artY, artW, artH);
        }

        if (_phase == Phase.Cafe)
            DrawCafeBorisHotspot(artX, artY, artW, artH);

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

        // Dropped items on top of scene overlays so they stay visible and clickable
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

        DrawOpeningWindowHotspot(artX, artY, artW, artH);

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

        DrawHistoryButtons();
        DrawTopRightButtons();
    }

    private static Rectangle ComputeTruckGasGaugeClickRect(Phase phase, Rectangle artBounds)
    {
        float x1;
        float y1;
        float x2;
        float y2;

        if (phase == Phase.WarehouseTruck)
        {
            x1 = WarehouseTruckHotspots.GasGaugeX1;
            y1 = WarehouseTruckHotspots.GasGaugeY1;
            x2 = WarehouseTruckHotspots.GasGaugeX2;
            y2 = WarehouseTruckHotspots.GasGaugeY2;
        }
        else
        {
            x1 = DeliveryTruckHotspots.GasGaugeX1;
            y1 = DeliveryTruckHotspots.GasGaugeY1;
            x2 = DeliveryTruckHotspots.GasGaugeX2;
            y2 = DeliveryTruckHotspots.GasGaugeY2;
        }

        return SceneRegion.ToScreenRect(x1, y1, x2 - x1, y2 - y1, artBounds);
    }

    private static Rectangle ComputeTruckGloveBoxClickRect(Phase phase, int artX, int artY, int artW, int artH)
    {
        var artBounds = new Rectangle(artX, artY, artW, artH);
        float gloveX1;
        float gloveY1;
        float gloveX2;
        float gloveY2;

        if (phase == Phase.WarehouseTruck)
        {
            gloveX1 = WarehouseTruckHotspots.GloveBoxX1;
            gloveY1 = WarehouseTruckHotspots.GloveBoxY1;
            gloveX2 = WarehouseTruckHotspots.GloveBoxX2;
            gloveY2 = WarehouseTruckHotspots.GloveBoxY2;
        }
        else
        {
            gloveX1 = DeliveryTruckHotspots.GloveBoxX1;
            gloveY1 = DeliveryTruckHotspots.GloveBoxY1;
            gloveX2 = DeliveryTruckHotspots.GloveBoxX2;
            gloveY2 = DeliveryTruckHotspots.GloveBoxY2;
        }

        return SceneRegion.ToScreenRect(
            gloveX1,
            gloveY1,
            gloveX2 - gloveX1,
            gloveY2 - gloveY1,
            artBounds);
    }

    private void DrawTruckGloveCompartmentHotspot(int artX, int artY, int artW, int artH)
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

    private const int NarrativeHorizontalPadding = 14;
    private const int NarrativeVerticalPadding = 14;
    private const int NarrativeMinHeight = 36;

    /// <summary>
    /// Draws the scene narrative log in the right sidebar.
    /// </summary>
    private void DrawSidebarNarrative(int panelX, int startY, int availableWidth, int maxHeight, string narrativeText)
    {
        Font font = _uiFont;
        float fontSize = LayoutConstants.NarrativeLongSize;
        float spacing = 0.9f;
        int lineHeight = (int)(fontSize * 1.42f);
        int blankLineHeight = lineHeight / 2;

        int textMaxWidth = availableWidth - NarrativeHorizontalPadding * 2;

        var (wrappedLines, textHeight) = GameTextLayout.WrapForBox(
            narrativeText,
            font,
            fontSize,
            spacing,
            textMaxWidth,
            lineHeight);

        int cardH = Math.Min(textHeight + NarrativeVerticalPadding * 2, maxHeight);
        int cardX = panelX;
        int cardY = startY;

        Raylib.DrawRectangle(cardX, cardY, availableWidth, cardH, Palette.NarrativeCardBg);
        Raylib.DrawRectangleLines(cardX, cardY, availableWidth, cardH, Palette.NarrativeCardBorder);

        int textLeft = cardX + NarrativeHorizontalPadding;
        int textTop = cardY + NarrativeVerticalPadding;
        int textBottom = cardY + cardH - NarrativeVerticalPadding;

        int y = textTop;
        for (int i = 0; i < wrappedLines.Count; i++)
        {
            int advance = string.IsNullOrEmpty(wrappedLines[i]) ? blankLineHeight : lineHeight;
            if (y + advance > textBottom)
                break;

            Raylib.DrawTextEx(
                font,
                wrappedLines[i],
                new Vector2(textLeft, y),
                fontSize,
                spacing,
                Palette.TextPrimary);

            y += advance;
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















}
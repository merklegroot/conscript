namespace Conscript;

/// <summary>
/// Immutable capture of gameplay state for undo/redo.
/// </summary>
internal sealed class GameStateSnapshot
{
    public required Game.Phase Phase { get; init; }
    public required Game.Phase PhaseOutdoorBeforeTent { get; init; }
    public required Game.Phase PhaseBeforeStore { get; init; }
    public required Game.Phase PhaseBeforeCafe { get; init; }
    public required bool BorisDeliveryJobActive { get; init; }
    public required bool WarehouseAmbushersDead { get; init; }
    public required bool FoldedPaperMessageRead { get; init; }
    public required bool WarehouseCrateOpened { get; init; }
    public required bool WarehouseKeypadUnlocked { get; init; }
    public required bool HasTrashBagTent { get; init; }
    public required Game.Phase? TentBuiltInPhase { get; init; }
    public required CafeOwnerDialog.Stage CafeOwnerDialogStage { get; init; }
    public required int Day { get; init; }
    public required string TimeOfDay { get; init; }
    public required string Location { get; init; }
    public required string City { get; init; }
    public required string Season { get; init; }
    public required int TemperatureF { get; init; }
    public required int Money { get; init; }
    public required int Health { get; init; }
    public required int Energy { get; init; }
    public required int Satiation { get; init; }
    public required int Hydration { get; init; }
    public required string Status { get; init; }
    public required int Comfort { get; init; }
    public required int Concealment { get; init; }
    public required int EnvHealthDelta { get; init; }
    public required int EnvEnergyDelta { get; init; }
    public required int EnvSatiationDelta { get; init; }
    public required int EnvHydrationDelta { get; init; }
    public required int EnvComfortDelta { get; init; }
    public required int ActionHealthDelta { get; init; }
    public required int ActionEnergyDelta { get; init; }
    public required int ActionSatiationDelta { get; init; }
    public required int ActionHydrationDelta { get; init; }
    public required int ActionComfortDelta { get; init; }
    public required float ActionDeltaTimer { get; init; }
    public required string?[] Backpack { get; init; }
    public required int?[] BackpackItemCharges { get; init; }
    public required List<DroppedItem> DroppedItems { get; init; }
    public required bool[] GloveBoxLootTaken { get; init; }
    public required bool[] BodyLootTaken { get; init; }
    public required string[] Choices { get; init; }
    public required int SelectedIndex { get; init; }
    public required bool NarrativeCollapsed { get; init; }
    public required string ActionMessage { get; init; }
    public required float ActionMessageTimer { get; init; }
    public required string DeathLine1 { get; init; }
    public required string DeathLine2 { get; init; }
}

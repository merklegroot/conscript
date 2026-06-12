namespace Conscript;

internal static class GamePhase
{
    public static bool IsForestSurvival(Game.Phase phase) =>
        phase is Game.Phase.ForestEntry or Game.Phase.Forest or Game.Phase.ForestStream;

    public static bool IsTownDistrict(Game.Phase phase) =>
        phase is Game.Phase.Town or Game.Phase.IndustrialDistrict or Game.Phase.CommercialDistrict;

    public static bool IsOutdoor(Game.Phase phase) =>
        phase is Game.Phase.Outside or Game.Phase.ForestEntry or Game.Phase.Forest or Game.Phase.ForestStream
        || IsTownDistrict(phase);

    public static bool IsOutdoorsSurvival(Game.Phase phase) =>
        phase is Game.Phase.Outside or Game.Phase.ForestEntry or Game.Phase.Forest or Game.Phase.ForestStream
        || IsTownDistrict(phase);

    public static bool IsInTruckCab(Game.Phase phase) =>
        phase is Game.Phase.DeliveryTruck or Game.Phase.WarehouseTruck;

    public static bool ShowsSceneNarrative(Game.Phase phase) =>
        phase is Game.Phase.Opening or Game.Phase.Outside or Game.Phase.Town
            or Game.Phase.IndustrialDistrict or Game.Phase.CommercialDistrict or Game.Phase.Store
            or Game.Phase.Cafe or Game.Phase.CafeBasement or Game.Phase.DeliveryTruck or Game.Phase.WarehouseTruck
            or Game.Phase.WarehouseAmbush or Game.Phase.WarehouseAftermath or Game.Phase.WarehouseInterior
            or Game.Phase.GasStation
            or Game.Phase.ForestEntry or Game.Phase.Forest
            or Game.Phase.ForestStream or Game.Phase.Tent;
}

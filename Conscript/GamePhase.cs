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
}

namespace Conscript;

internal static class GamePhase
{
    public static bool IsForestSurvival(Game.Phase phase) =>
        phase is Game.Phase.Forest or Game.Phase.ForestStream;

    public static bool IsOutdoor(Game.Phase phase) =>
        phase is Game.Phase.Outside or Game.Phase.Forest or Game.Phase.ForestStream;

    public static bool IsOutdoorsSurvival(Game.Phase phase) =>
        phase is Game.Phase.Outside or Game.Phase.Forest or Game.Phase.ForestStream;
}

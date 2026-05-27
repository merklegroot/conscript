namespace Conscript;

/// <summary>Static comfort and concealment tables for outdoor survival.</summary>
internal static class SurvivalEnvironment
{
    /// <summary>Steady outdoor discomfort while wearing a winter coat (maps to 1–3 arrows).</summary>
    public static int OutdoorComfortPenaltyForTemp(int tempF)
    {
        if (tempF >= 40) return -2;
        if (tempF >= 22) return -4;
        if (tempF >= 12) return -8;
        if (tempF >= 0) return -12;
        return -18;
    }

    public static int OutdoorComfortPerActionPenalty(int tempF) =>
        tempF >= 22 ? -1 : tempF >= 12 ? -2 : -3;

    /// <summary>Location-based hide rating before time-of-day modifiers.</summary>
    public static int ConcealmentForPhase(Game.Phase phase) => phase switch
    {
        Game.Phase.Opening => 35,
        Game.Phase.Outside => 12,
        Game.Phase.Town => 20,
        Game.Phase.Store => 8,
        Game.Phase.ForestEntry => 42,
        Game.Phase.Forest => 78,
        Game.Phase.ForestStream => 68,
        Game.Phase.Tent => 92,
        Game.Phase.Death => 0,
        _ => 50
    };
}

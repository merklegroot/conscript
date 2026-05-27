namespace Conscript;

internal static class GameStatMath
{
    public static int ClampStat(int value) => Math.Max(0, Math.Min(100, value));

    public static int StatArrowCount(int delta)
    {
        int abs = Math.Abs(delta);
        if (abs == 0) return 0;
        if (abs <= 4) return 1;
        if (abs <= 10) return 2;
        return 3;
    }

    public static void TickTimedMessage(ref float timer, ref string message, float deltaSeconds)
    {
        if (timer <= 0f)
            return;

        timer -= deltaSeconds;
        if (timer <= 0f)
            message = "";
    }
}

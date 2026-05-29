namespace Conscript;

internal static class GameStatMath
{
    public static void TickTimedMessage(ref float timer, ref string message, float deltaSeconds)
    {
        if (timer <= 0f)
            return;

        timer -= deltaSeconds;
        if (timer <= 0f)
            message = "";
    }
}

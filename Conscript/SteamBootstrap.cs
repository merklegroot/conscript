using Conscript.Constants;
using Steamworks;

namespace Conscript;

/// <summary>
/// Initializes Steamworks against App ID 480 (Spacewar), Valve's standard SDK test app.
/// Safe to call when Steam is closed — the game still runs without Steam features.
/// </summary>
public static class SteamBootstrap
{
    public static bool IsInitialized { get; private set; }

    public static void TryInit()
    {
        if (IsInitialized)
            return;

        try
        {
            SteamClient.Init(GameConstants.SteamAppId);
            IsInitialized = true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"Steam unavailable (App ID {GameConstants.SteamAppId}): {ex.Message}");
        }
    }

    public static void Shutdown()
    {
        if (!IsInitialized)
            return;

        SteamClient.Shutdown();
        IsInitialized = false;
    }
}

public static class IdentityFactory
{
    // If STEAM_ENABLED is defined, we’ll try to init Steam; otherwise fall back to LocalDev.
    public static IPlayerIdentity Create(uint steamAppId, out bool steamInitialized)
    {
        steamInitialized = false;

#if STEAM_ENABLED
        try
        {
            // Steamworks.NET init
            if (!Steamworks.SteamAPI.IsSteamRunning())
                throw new System.Exception("Steam client not running.");

            if (!Steamworks.SteamAPI.Init())
                throw new System.Exception("SteamAPI.Init failed.");

            steamInitialized = true;
            return new SteamIdentity();
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogWarning(
                $"[Identity] Steam init failed, falling back to LocalDev. {e.Message}"
            );
        }
#endif

#if DEV_FAKE_STEAM
        return new LocalDevIdentity("dev-micah");
#else
        return new LocalDevIdentity("dev-unknown");
#endif
    }
}

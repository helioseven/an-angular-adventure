#if STEAM_ENABLED
using Steamworks;

public sealed class SteamIdentity : IPlayerIdentity
{
    public bool IsSteam => true;
    public string Platform => "steam";
    public string SteamIdOrDevId => SteamUser.GetSteamID().m_SteamID.ToString();
}
#endif

public interface IPlayerIdentity
{
    bool IsSteam { get; }
    string Platform { get; } // "steam" | "local"
    string SteamIdOrDevId { get; } // 7656... or "dev-micah"
}

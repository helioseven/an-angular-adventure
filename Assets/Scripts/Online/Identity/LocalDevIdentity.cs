public sealed class LocalDevIdentity : IPlayerIdentity
{
    public bool IsSteam => false;
    public string Platform => "local";
    public string SteamIdOrDevId { get; }

    public LocalDevIdentity(string devId = "dev-micah") => SteamIdOrDevId = devId;
}

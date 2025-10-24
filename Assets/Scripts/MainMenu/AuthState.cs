using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// ===== Shared auth state =====
public static class AuthState
{
    public static string SteamId { get; set; } = "";
    public static string PersonaName { get; set; } = "";
    public static string AvatarUrl { get; set; } = "";
    public static string Jwt { get; set; } = "";

    public static event Action OnChanged;

    public static void RaiseChanged() => OnChanged?.Invoke();

    public static void SetSteamId(string value)
    {
        PlayerPrefs.SetString("steamid", value);
        PlayerPrefs.Save();
        RaiseChanged();
    }

    public static void SetJwt(string token)
    {
        Jwt = token;
        PlayerPrefs.SetString("steam.jwt", token);
        PlayerPrefs.Save();
        RaiseChanged();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitFromPrefs()
    {
        SteamId = PlayerPrefs.GetString("steam.steamid", SteamId);
        PersonaName = PlayerPrefs.GetString("steam.name", PersonaName);
        AvatarUrl = PlayerPrefs.GetString("steam.avatar", AvatarUrl);
        Jwt = PlayerPrefs.GetString("steam.jwt", "");
    }
}

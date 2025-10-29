using System;
using UnityEngine;

/// <summary>
/// Shared authentication state for the entire app.
/// Holds current Steam ID, persona name, avatar URL, and Supabase JWT.
/// Persists between sessions using PlayerPrefs.
/// </summary>
public static class AuthState
{
    public static string SteamId { get; private set; } = "";
    public static string PersonaName { get; private set; } = "";
    public static string AvatarUrl { get; private set; } = "";
    public static string Jwt { get; private set; } = "";

    public static event Action OnChanged;

    public static void RaiseChanged() => OnChanged?.Invoke();

    // --- Setters that also persist ---
    public static void SetSteamId(string value)
    {
        SteamId = value;
        PlayerPrefs.SetString("steam.steamid", value);
        PlayerPrefs.Save();
        RaiseChanged();
    }

    public static void SetPersonaName(string value)
    {
        PersonaName = value;
        PlayerPrefs.SetString("steam.name", value);
        PlayerPrefs.Save();
        RaiseChanged();
    }

    public static void SetAvatarUrl(string value)
    {
        AvatarUrl = value;
        PlayerPrefs.SetString("steam.avatar", value);
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

    /// <summary>
    /// Allows grouped updates (avoids multiple RaiseChanged() calls)
    /// </summary>
    public static void SetSteamProfile(string steamId, string personaName, string avatarUrl)
    {
        SteamId = steamId ?? SteamId;
        PersonaName = personaName ?? PersonaName;
        AvatarUrl = avatarUrl ?? AvatarUrl;

        PlayerPrefs.SetString("steam.steamid", SteamId);
        PlayerPrefs.SetString("steam.name", PersonaName);
        PlayerPrefs.SetString("steam.avatar", AvatarUrl);
        PlayerPrefs.Save();

        RaiseChanged();
    }

    // --- Initialization from PlayerPrefs ---
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitFromPrefs()
    {
        SteamId = PlayerPrefs.GetString("steam.steamid", "");
        PersonaName = PlayerPrefs.GetString("steam.name", "");
        AvatarUrl = PlayerPrefs.GetString("steam.avatar", "");
        Jwt = PlayerPrefs.GetString("steam.jwt", "");

        if (!string.IsNullOrEmpty(SteamId))
            Debug.Log($"[AuthState] Loaded from prefs: {PersonaName} ({SteamId})");

        RaiseChanged();
    }

    /// <summary>
    /// Clears stored data (useful for testing/logout).
    /// </summary>
    public static void Clear()
    {
        SteamId = "";
        PersonaName = "";
        AvatarUrl = "";
        Jwt = "";

        PlayerPrefs.DeleteKey("steam.steamid");
        PlayerPrefs.DeleteKey("steam.name");
        PlayerPrefs.DeleteKey("steam.avatar");
        PlayerPrefs.DeleteKey("steam.jwt");
        PlayerPrefs.Save();

        RaiseChanged();
    }
}

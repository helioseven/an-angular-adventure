using System;
using UnityEngine;

/// <summary>
/// Shared authentication state for the entire app.
/// Holds current Steam ID, persona name, avatar URL, and Supabase JWT.
/// Persists between sessions using PlayerPrefs, and remains alive across scenes.
/// </summary>
public class AuthState : MonoBehaviour
{
    public static AuthState Instance { get; private set; }

    public string SteamId { get; private set; } = "";
    public string PersonaName { get; private set; } = "";
    public string AvatarUrl { get; private set; } = "";
    public string Jwt { get; private set; } = "";

    public event Action OnChanged;

    // jwt token expiry helper
    public double TokenExpiryUnix { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load everything *except* JWT
        SteamId = PlayerPrefs.GetString("steam.steamid", "");
        PersonaName = PlayerPrefs.GetString("steam.name", "");
        AvatarUrl = PlayerPrefs.GetString("steam.avatar", "");

        // Forget old JWT so we always reauth on fresh launch
        Jwt = "";

        Debug.Log("[AuthState] Initialized (JWT cleared for fresh session)");
    }

    private void RaiseChanged() => OnChanged?.Invoke();

    // --- Setters that also persist ---
    public void SetSteamId(string value)
    {
        SteamId = value;
        PlayerPrefs.SetString("steam.steamid", value);
        PlayerPrefs.Save();
        RaiseChanged();
    }

    public void SetPersonaName(string value)
    {
        PersonaName = value;
        PlayerPrefs.SetString("steam.name", value);
        PlayerPrefs.Save();
        RaiseChanged();
    }

    public void SetAvatarUrl(string value)
    {
        AvatarUrl = value;
        PlayerPrefs.SetString("steam.avatar", value);
        PlayerPrefs.Save();
        RaiseChanged();
    }

    public void SetJwt(string token)
    {
        Jwt = token;
        PlayerPrefs.SetString("steam.jwt", token);
        PlayerPrefs.Save();
        RaiseChanged();
    }

    public void SetSteamProfile(string steamId, string personaName, string avatarUrl)
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

    private void InitFromPrefs()
    {
        SteamId = PlayerPrefs.GetString("steam.steamid", "");
        PersonaName = PlayerPrefs.GetString("steam.name", "");
        AvatarUrl = PlayerPrefs.GetString("steam.avatar", "");
        Jwt = PlayerPrefs.GetString("steam.jwt", "");

        if (!string.IsNullOrEmpty(SteamId))
            Debug.Log($"[AuthState] Loaded from prefs: {PersonaName} ({SteamId})");

        RaiseChanged();
    }

    public void Clear()
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

    public bool IsTokenExpired()
    {
        if (TokenExpiryUnix <= 0)
            return false;
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds() > TokenExpiryUnix;
    }
}

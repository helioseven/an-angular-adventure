using System;
using System.Text;
using UnityEngine;

/// <summary>
/// Shared authentication state for the entire app.
/// Holds current Steam ID, persona name, avatar URL, and Supabase JWT.
/// In-memory auth state for the entire app. Does not persist between sessions.
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void ExpireJwtNow()
    {
        if (string.IsNullOrEmpty(Jwt))
            return;
        TokenExpiryUnix = 1;
    }
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Fresh launch each run; nothing is cached across sessions
        Jwt = "";

        Debug.Log("[AuthState] Initialized (no persistence)");
    }

    private void RaiseChanged() => OnChanged?.Invoke();

    // --- Setters that also persist ---
    public void SetSteamId(string value)
    {
        SteamId = value;
        RaiseChanged();
    }

    public void SetPersonaName(string value)
    {
        PersonaName = value;
        RaiseChanged();
    }

    public void SetAvatarUrl(string value)
    {
        AvatarUrl = value;
        RaiseChanged();
    }

    public void SetJwt(string token)
    {
        Jwt = token;
        if (TryGetJwtExpiry(token, out long exp))
            TokenExpiryUnix = exp;
        else
            TokenExpiryUnix = 0;
        RaiseChanged();
    }

    public void SetSteamProfile(string steamId, string personaName, string avatarUrl)
    {
        SteamId = steamId ?? SteamId;
        PersonaName = personaName ?? PersonaName;
        AvatarUrl = avatarUrl ?? AvatarUrl;

        RaiseChanged();
    }

    public void Clear()
    {
        SteamId = "";
        PersonaName = "";
        AvatarUrl = "";
        Jwt = "";
        TokenExpiryUnix = 0;

        RaiseChanged();
    }

    public bool IsTokenExpired()
    {
        if (TokenExpiryUnix <= 0)
            return false;
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds() > TokenExpiryUnix;
    }

    [Serializable]
    private class JwtPayload
    {
        public long exp;
    }

    private static bool TryGetJwtExpiry(string token, out long exp)
    {
        exp = 0;
        if (string.IsNullOrEmpty(token))
            return false;

        string[] parts = token.Split('.');
        if (parts.Length < 2)
            return false;

        string payload = parts[1].Replace('-', '+').Replace('_', '/');
        int pad = payload.Length % 4;
        if (pad == 2)
            payload += "==";
        else if (pad == 3)
            payload += "=";
        else if (pad != 0)
            return false;

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(payload);
        }
        catch (FormatException)
        {
            return false;
        }

        string json = Encoding.UTF8.GetString(bytes);
        JwtPayload parsed = JsonUtility.FromJson<JwtPayload>(json);
        if (parsed == null || parsed.exp <= 0)
            return false;

        exp = parsed.exp;
        return true;
    }
}

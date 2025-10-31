using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Displays the authenticated user's Steam info from AuthState.Instance.
/// Automatically updates when AuthState changes and rebuilds from saved prefs if needed.
/// </summary>
public class AccountInfoPanel : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text nameTMP;
    public TMP_Text idTMP;
    public RawImage avatarImage;
    public Vector2 avatarSize = new(128, 128);
    public Texture2D fallbackAvatar;

    private Coroutine avatarCo;

    private void OnEnable()
    {
        // Subscribe to updates and draw immediately
        AuthState.Instance.OnChanged += Refresh;
        LoadFromAuthOrPrefs();
    }

    private void OnDisable()
    {
        AuthState.Instance.OnChanged -= Refresh;
    }

    /// <summary>
    /// Rebuilds state from AuthState (or PlayerPrefs if AuthState is empty).
    /// </summary>
    private void LoadFromAuthOrPrefs()
    {
        // Pull latest known data
        string steamId = AuthState.Instance.SteamId;
        string persona = AuthState.Instance.PersonaName;
        string avatar = AuthState.Instance.AvatarUrl;

        // If nothing in memory, restore from saved prefs
        if (string.IsNullOrEmpty(steamId))
            steamId = PlayerPrefs.GetString("steam.steamid", "(no id)");
        if (string.IsNullOrEmpty(persona))
            persona = PlayerPrefs.GetString("steam.name", "(unknown)");
        if (string.IsNullOrEmpty(avatar))
            avatar = PlayerPrefs.GetString("steam.avatar", "");

        // Update AuthState so everything else stays consistent
        if (string.IsNullOrEmpty(AuthState.Instance.SteamId) && !string.IsNullOrEmpty(steamId))
            AuthState.Instance.SetSteamProfile(steamId, persona, avatar);

        // Draw UI now
        Refresh();
    }

    private void Refresh()
    {
        string name = string.IsNullOrEmpty(AuthState.Instance.PersonaName)
            ? "(unknown)"
            : AuthState.Instance.PersonaName;
        string id = string.IsNullOrEmpty(AuthState.Instance.SteamId)
            ? "(no id)"
            : AuthState.Instance.SteamId;
        string avatarUrl = AuthState.Instance.AvatarUrl;

        if (nameTMP)
            nameTMP.SetText(name);
        if (idTMP)
            idTMP.SetText(id);

        if (avatarImage)
        {
            if (avatarCo != null)
                StopCoroutine(avatarCo);

            if (!string.IsNullOrEmpty(avatarUrl))
                avatarCo = StartCoroutine(LoadAvatar(avatarUrl));
            else
                ApplyFallbackAvatar();
        }

        Debug.Log($"[AccountInfoPanel] Updated from AuthState → {name} ({id})");
    }

    private IEnumerator LoadAvatar(string url)
    {
        using var req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var tex = DownloadHandlerTexture.GetContent(req);
            avatarImage.texture = tex;
            avatarImage.SetNativeSize();
            avatarImage.rectTransform.sizeDelta = avatarSize;
            Debug.Log("[AccountInfoPanel] Avatar loaded from URL.");
        }
        else
        {
            Debug.LogWarning($"[AccountInfoPanel] Avatar load failed: {req.error}");
            ApplyFallbackAvatar();
        }
    }

    private void ApplyFallbackAvatar()
    {
        if (avatarImage && fallbackAvatar)
        {
            avatarImage.texture = fallbackAvatar;
            avatarImage.SetNativeSize();
            avatarImage.rectTransform.sizeDelta = avatarSize;
        }
    }
}

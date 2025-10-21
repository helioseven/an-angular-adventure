using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

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
        AuthState.OnChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        AuthState.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        var displayName = string.IsNullOrEmpty(AuthState.PersonaName)
            ? "(unknown)"
            : AuthState.PersonaName;
        var sid = string.IsNullOrEmpty(AuthState.SteamId) ? "—" : AuthState.SteamId;
        var avatarUrl = AuthState.AvatarUrl;

        if (nameTMP)
            nameTMP.SetText(displayName);
        if (idTMP)
            idTMP.SetText(sid);

        if (avatarImage)
        {
            if (avatarCo != null)
                StopCoroutine(avatarCo);
            if (!string.IsNullOrEmpty(avatarUrl))
                avatarCo = StartCoroutine(LoadAvatar(avatarUrl));
            else
                ApplyFallback();
        }

        Debug.Log(
            $"[AccountInfoPanel] UI set → name='{displayName}', id='{sid}', avatar='{avatarUrl}'"
        );
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
        }
        else
        {
            Debug.LogWarning($"Avatar load failed: {req.error}");
            ApplyFallback();
        }
    }

    private void ApplyFallback()
    {
        if (avatarImage && fallbackAvatar)
        {
            avatarImage.texture = fallbackAvatar;
            avatarImage.SetNativeSize();
            avatarImage.rectTransform.sizeDelta = avatarSize;
        }
    }
}

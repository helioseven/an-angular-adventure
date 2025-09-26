using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AccountInfoPanel : MonoBehaviour
{
    [Header("Steam")]
    [Tooltip("Your Steam AppID for runtime init (only used if STEAM_ENABLED is defined).")]
    public uint steamAppId = 480; // TODO: set to your real AppID for Steam builds

    [Header("Supabase Config")]
    [Tooltip("If left null, will auto-load Resources/SupabaseConfig.asset")]
    public SupabaseConfig config; // ScriptableObject (see step 2)

    [Header("UI (TextMeshPro preferred)")]
    public TMP_Text nameTMP;
    public TMP_Text idTMP;
    public RawImage avatarImage;

    private bool _steamInitialized;

    private void OnEnable()
    {
        _ = Refresh(); // fire-and-forget
    }

    private async Task Refresh()
    {
        try
        {
            if (config == null)
            {
                config = Resources.Load<SupabaseConfig>("SupabaseConfig");
                if (config == null)
                {
                    Debug.LogError("Missing SupabaseConfig asset in Resources.");
                    return;
                }
            }

#if UNITY_EDITOR
            // Optionally pull secrets from OS env so you don't store keys in assets
            if (string.IsNullOrEmpty(config.bearerToken))
                config.bearerToken = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY") ?? "";

            if (config.sendDevMockHeaders && string.IsNullOrEmpty(config.devToken))
                config.devToken = Environment.GetEnvironmentVariable("DEV_TOKEN") ?? "";
#endif

            string id = GetIdentity(out _steamInitialized);

            // Call your edge function
            var json = await PostJsonAsync(
                config.functionUrl,
                $"{{\"steamid\":\"{id}\"}}",
                config.bearerToken,
#if UNITY_EDITOR
                config.sendDevMockHeaders,
                config.devToken
#else
                false,
                null
#endif
            );

            // Parse expected shape: { ok, steamid, data: { response: { players: [ ... ] } } }
            var root = JsonUtility.FromJson<SteamSummaryWrapper>(json);
            if (root?.data?.response?.players == null || root.data.response.players.Length == 0)
            {
                Debug.LogWarning("No players returned from Steam.");
                SetTexts("(unknown)", id);
                return;
            }

            var p = root.data.response.players[0];

            // Populate UI
            SetTexts(p.personaname, p.steamid);

            if (!string.IsNullOrEmpty(p.avatarfull) && avatarImage != null)
                StartCoroutine(LoadAvatar(p.avatarfull));
        }
        catch (Exception ex)
        {
            Debug.LogError($"AccountInfoPanel Refresh error: {ex}");
        }
    }

    private void SetTexts(string persona, string steamid)
    {
        nameTMP.SetText(persona);
        idTMP.SetText(steamid);
    }

    private IEnumerator LoadAvatar(string url)
    {
        using var req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var tex = DownloadHandlerTexture.GetContent(req);
            if (avatarImage)
                avatarImage.texture = tex;
        }
        else
        {
            Debug.LogWarning($"Avatar load failed: {req.error}");
        }
    }

    private async Task<string> PostJsonAsync(
        string url,
        string body,
        string bearer,
        bool devMock,
        string devToken
    )
    {
        using var req = new UnityWebRequest(url, "POST");
        byte[] bytes = Encoding.UTF8.GetBytes(body);
        req.uploadHandler = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrEmpty(bearer))
            req.SetRequestHeader("Authorization", $"Bearer {bearer}");

#if UNITY_EDITOR
        if (devMock)
        {
            req.SetRequestHeader("X-Dev-Mock", "true");
            if (!string.IsNullOrEmpty(devToken))
                req.SetRequestHeader("X-Dev-Token", devToken);
        }
#endif

        var op = req.SendWebRequest();
        while (!op.isDone)
            await Task.Yield();

        if (req.result != UnityWebRequest.Result.Success)
            throw new Exception(
                $"HTTP {req.responseCode}: {req.error} :: {req.downloadHandler.text}"
            );

        return req.downloadHandler.text ?? "{}";
    }

    private string GetIdentity(out bool steamInitialized)
    {
        steamInitialized = false;

#if STEAM_ENABLED
        try
        {
            // Steamworks.NET
            if (!Steamworks.SteamAPI.IsSteamRunning())
                throw new Exception("Steam client not running.");

            if (!Steamworks.SteamAPI.Init())
                throw new Exception("SteamAPI.Init failed.");

            steamInitialized = true;
            return Steamworks.SteamUser.GetSteamID().m_SteamID.ToString();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Steam init failed, using dev ID. {e.Message}");
        }
#endif

        // Editor/dev fallback ID (everyone allowed)
        return "dev-micah";
    }

    private void OnDisable()
    {
#if STEAM_ENABLED
        if (_steamInitialized)
        {
            try
            {
                Steamworks.SteamAPI.Shutdown();
            }
            catch { }
            _steamInitialized = false;
        }
#endif
    }

    // ---------- JSON wrappers (for JsonUtility) ----------
    [Serializable]
    public class SteamSummaryWrapper
    {
        public SteamData data;
    }

    [Serializable]
    public class SteamData
    {
        public SteamResponse response;
    }

    [Serializable]
    public class SteamResponse
    {
        public SteamPlayer[] players;
    }

    [Serializable]
    public class SteamPlayer
    {
        public string steamid;
        public string personaname;
        public string avatarfull;
    }
}

using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SupabaseTest : MonoBehaviour
{
    [Header("Supabase Edge Function")]
    [SerializeField]
    private string supabaseUrl =
        "https://nswnjhegifaudsgjyrwf.supabase.co/functions/v1/steam-partner";

    [Header("Supabase REST base (for /rest/v1/users)")]
    [SerializeField]
    private string supabaseRestBase = "https://nswnjhegifaudsgjyrwf.supabase.co/rest/v1";

    [Header("Anon Key (for REST upsert)")]
    [TextArea]
    [SerializeField]
    private string anonKey = "";

#if UNITY_EDITOR
    [Header("DEV ONLY - Editor Bearer Token  (Unity Editor Development)")]
    [TextArea]
    [SerializeField]
    private string editorBearerToken = "";
#endif

    [Header("DEV ONLY - Test SteamID (used if none provided elsewhere)")]
    [SerializeField]
    private string testSteamId = "";

    // ===== DTOs that match both shapes =====
    [Serializable]
    private class SteamRequest
    {
        public string steamid;
    }

    [Serializable]
    private class Player
    {
        public string steamid,
            personaname,
            profileurl,
            avatar,
            avatarmedium,
            avatarfull;
    }

    [Serializable]
    private class ResponseInner
    {
        public Player[] players;
    }

    [Serializable]
    private class DataOuter
    {
        public ResponseInner response;
    }

    [Serializable]
    private class RespRoot
    {
        public bool ok;
        public string steamid;
        public string token;
        public DataOuter data;
        public string error;
        public string details;
    }

    // ===== DTO for user upsert =====
    [Serializable]
    private class UserUpsert
    {
        public string steam_id;
        public string display_name;
        public string avatar_url;
    }

    private void Start()
    {
        StartCoroutine(PostSteamId(testSteamId));
    }

    public IEnumerator PostSteamId(string steamId)
    {
        var sid = string.IsNullOrWhiteSpace(steamId) ? testSteamId : steamId;
        var body = new SteamRequest { steamid = sid };
        var json = JsonUtility.ToJson(body);

        using var req = new UnityWebRequest(supabaseUrl, UnityWebRequest.kHttpVerbPOST)
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer(),
        };
        req.SetRequestHeader("Content-Type", "application/json");

#if UNITY_EDITOR
        if (!string.IsNullOrWhiteSpace(editorBearerToken))
            req.SetRequestHeader("Authorization", $"Bearer {editorBearerToken}");
#endif

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(
                $"[steam-partner] {req.responseCode} {req.error}\n{req.downloadHandler.text}"
            );
            yield break;
        }

        var text = req.downloadHandler.text;
        Debug.Log("Success - Response: " + text);

        RespRoot resp;
        try
        {
            resp = JsonUtility.FromJson<RespRoot>(text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"JSON parse error: {ex.Message}");
            yield break;
        }

        if (resp == null || !resp.ok)
        {
            Debug.LogError($"steam-partner not ok. error={resp?.error} details={resp?.details}");
            yield break;
        }

        if (!string.IsNullOrEmpty(resp.steamid))
            AuthState.SteamId = resp.steamid;
        if (!string.IsNullOrEmpty(resp.token))
            PlayerPrefs.SetString("steam.jwt", resp.token);

        var players =
            resp.data != null && resp.data.response != null ? resp.data.response.players : null;
        if (players != null && players.Length > 0)
        {
            var p = players[0];
            AuthState.SteamId = string.IsNullOrEmpty(AuthState.SteamId)
                ? p.steamid
                : AuthState.SteamId;
            AuthState.PersonaName = string.IsNullOrEmpty(p.personaname)
                ? AuthState.PersonaName
                : p.personaname;
            AuthState.AvatarUrl =
                !string.IsNullOrEmpty(p.avatarfull) ? p.avatarfull
                : !string.IsNullOrEmpty(p.avatarmedium) ? p.avatarmedium
                : !string.IsNullOrEmpty(p.avatar) ? p.avatar
                : AuthState.AvatarUrl;
        }

        PlayerPrefs.SetString("steam.steamid", AuthState.SteamId ?? "");
        PlayerPrefs.SetString("steam.name", AuthState.PersonaName ?? "");
        PlayerPrefs.SetString("steam.avatar", AuthState.AvatarUrl ?? "");
        PlayerPrefs.Save();

        AuthState.RaiseChanged();
        var who = string.IsNullOrEmpty(AuthState.PersonaName)
            ? "(no persona yet)"
            : AuthState.PersonaName;
        Debug.Log($"Steam OK → {who} ({AuthState.SteamId})");

        // ===== Call upsert after Steam success =====
        if (!string.IsNullOrEmpty(resp.token))
        {
            yield return StartCoroutine(UpsertUser(resp.token));
        }
        else
        {
            Debug.LogWarning("[users upsert] Skipped — no JWT present.");
        }
    }

    // ====== Upsert coroutine ======
    private IEnumerator UpsertUser(string jwt)
    {
        var user = new UserUpsert
        {
            steam_id = AuthState.SteamId,
            display_name = AuthState.PersonaName,
            avatar_url = AuthState.AvatarUrl,
        };

        var json = JsonUtility.ToJson(user);
        var url = $"{supabaseRestBase}/users?select=*";

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer(),
        };

        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("apikey", anonKey);
        req.SetRequestHeader("Authorization", $"Bearer {jwt}");
        req.SetRequestHeader("Prefer", "resolution=merge-duplicates");

        Debug.Log("[users upsert] Sending user info to Supabase...");
        Debug.Log("[users upsert] URL: " + url);
        Debug.Log("[users upsert] Body: " + json);

        yield return req.SendWebRequest();

        Debug.Log($"[users upsert] Response: {req.responseCode} {req.result}");
        Debug.Log($"[users upsert] Text: {req.downloadHandler.text}");

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[users upsert] Failed → {req.error}");
            yield break;
        }

        Debug.Log("[users upsert] OK!");
    }
}

// ===== Minimal shared auth state =====
public static class AuthState
{
    public static string SteamId { get; set; } = "";
    public static string PersonaName { get; set; } = "";
    public static string AvatarUrl { get; set; } = "";

    public static event Action OnChanged;

    public static void RaiseChanged() => OnChanged?.Invoke();

    public static void SetSteamId(string value)
    {
        PlayerPrefs.SetString("steamid", value);
        PlayerPrefs.Save();
        RaiseChanged();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitFromPrefs()
    {
        SteamId = PlayerPrefs.GetString("steam.steamid", SteamId);
        PersonaName = PlayerPrefs.GetString("steam.name", PersonaName);
        AvatarUrl = PlayerPrefs.GetString("steam.avatar", AvatarUrl);
    }
}

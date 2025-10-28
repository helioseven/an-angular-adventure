using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SupabaseTest : MonoBehaviour
{
    [Header("DEV ONLY - Test SteamID (used if none provided elsewhere)")]
    [SerializeField]
    private string testSteamId = "";

    private string supabaseSteamPartnerEdgeFunctionUrl =
        "https://nswnjhegifaudsgjyrwf.supabase.co/functions/v1/steam-partner";

    private string supabaseRestBase = "https://nswnjhegifaudsgjyrwf.supabase.co/rest/v1";
    private string supabasePublicAnonAPIKey =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im5zd25qaGVnaWZhdWRzZ2p5cndmIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDI3ODg3MDEsImV4cCI6MjA1ODM2NDcwMX0.c6JxmTv5DUD2ZeocXg1S1MFR_fPSK7RzB_CV4swO4sM";

    // === Singleton setup ===
    public static SupabaseTest Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ===== DTOs =====
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

    [Serializable]
    private class UserUpsert
    {
        public string steam_id;
        public string display_name;
        public string avatar_url;
    }

    // === Public controller entry points ===
    public void BeginPostSteamId(string steamId)
    {
        StartCoroutine(PostSteamId(steamId));
    }

    public IEnumerator PostSteamId(string steamId)
    {
        var sid = string.IsNullOrWhiteSpace(steamId) ? testSteamId : steamId;
        var body = new SteamRequest { steamid = sid };
        var json = JsonUtility.ToJson(body);

        using var req = new UnityWebRequest(
            supabaseSteamPartnerEdgeFunctionUrl,
            UnityWebRequest.kHttpVerbPOST
        )
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer(),
        };
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", $"Bearer {supabasePublicAnonAPIKey}");

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

        if (resp == null)
        {
            Debug.LogError("[steam-partner] Response was null");
            yield break;
        }

        if (resp.data?.response?.players != null)
        {
            foreach (var pl in resp.data.response.players)
            {
                Debug.Log($"Player: {pl.personaname} ({pl.steamid})\nAvatar: {pl.avatarfull}");
            }
        }

        if (resp == null || !resp.ok)
        {
            Debug.LogError($"[steam-partner] not ok. error={resp?.error} details={resp?.details}");
            yield break;
        }

        // --- Update JWT if present ---
        if (!string.IsNullOrEmpty(resp.token))
        {
            AuthState.SetJwt(resp.token);

            // Sneak peek for debugging (shows start & end only)
            string jwt = resp.token;
            string preview =
                jwt.Length > 20 ? $"{jwt.Substring(0, 10)}...{jwt.Substring(jwt.Length - 8)}" : jwt;

            Debug.Log($"[JWT OK] {preview}");
        }

        // --- Extract Steam player info ---
        var players = resp.data?.response?.players;
        if (players != null && players.Length > 0)
        {
            var p = players[0];
            string avatar =
                !string.IsNullOrEmpty(p.avatarfull) ? p.avatarfull
                : !string.IsNullOrEmpty(p.avatarmedium) ? p.avatarmedium
                : !string.IsNullOrEmpty(p.avatar) ? p.avatar
                : "";

            AuthState.SetSteamProfile(p.steamid, p.personaname, avatar);
            Debug.Log($"[SupabaseTest] Updated AuthState → {p.personaname} ({p.steamid})");
        }
        else
        {
            Debug.LogWarning("[SupabaseTest] No players returned from Steam data.");
        }

        // --- Sanity check ---
        Debug.Log($"[SupabaseTest] Steam OK → {AuthState.PersonaName} ({AuthState.SteamId})");

        // --- Continue with upsert if JWT present ---
        if (!string.IsNullOrEmpty(AuthState.Jwt))
        {
            Debug.Log("[SupabaseTest] Upserting user...");
            yield return StartCoroutine(UpsertUser(AuthState.Jwt));
        }
        else
        {
            Debug.LogWarning("[SupabaseTest] Skipped upsert — no JWT present.");
        }

        if (!string.IsNullOrEmpty(resp.token))
            yield return StartCoroutine(UpsertUser(resp.token));
        else
            Debug.LogWarning("[users upsert] Skipped — no JWT present.");
    }

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
        req.SetRequestHeader("apikey", supabasePublicAnonAPIKey);
        req.SetRequestHeader("Authorization", $"Bearer {jwt}");
        req.SetRequestHeader("Prefer", "resolution=merge-duplicates");

        Debug.Log("[users upsert] Sending user info to Supabase...");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[users upsert] Failed → {req.error}");
            yield break;
        }

        Debug.Log("[users upsert] OK!");
    }
}

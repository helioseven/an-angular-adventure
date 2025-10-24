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

    // private string supabaseBaseUrl = "https://nswnjhegifaudsgjyrwf.supabase.co";
    private string supabaseSteamPartnerEdgeFunctionUrl =
        "https://nswnjhegifaudsgjyrwf.supabase.co/functions/v1/steam-partner";

    private string supabaseRestBase = "https://nswnjhegifaudsgjyrwf.supabase.co/rest/v1";
    private string supabasePublicAnonAPIKey =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im5zd25qaGVnaWZhdWRzZ2p5cndmIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDI3ODg3MDEsImV4cCI6MjA1ODM2NDcwMX0.c6JxmTv5DUD2ZeocXg1S1MFR_fPSK7RzB_CV4swO4sM";

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

        if (
            resp != null
            && resp.data != null
            && resp.data.response != null
            && resp.data.response.players != null
        )
        {
            Debug.Log(
                $"[steam-partner] Got {resp.data.response.players.Length} player(s) back from Steam API."
            );
            foreach (var pl in resp.data.response.players)
            {
                Debug.Log(
                    $"Player: {pl.personaname} ({pl.steamid})\nAvatar: {pl.avatarfull}\nProfile: {pl.profileurl}"
                );
            }
        }
        else
        {
            Debug.LogWarning("[steam-partner] No players returned in data.response.players");
        }

        if (resp == null || !resp.ok)
        {
            Debug.LogError($"steam-partner not ok. error={resp?.error} details={resp?.details}");
            yield break;
        }

        if (!string.IsNullOrEmpty(resp.steamid))
            AuthState.SteamId = resp.steamid;
        if (!string.IsNullOrEmpty(resp.token))
            AuthState.SetJwt(resp.token);

        var players =
            resp.data != null && resp.data.response != null ? resp.data.response.players : null;
        if (players != null && players.Length > 0)
        {
            var p = players[0];

            Debug.Log(p);
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
            Debug.Log("About to shoot off the upsert after succccuess:))))");
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
        req.SetRequestHeader("apikey", supabasePublicAnonAPIKey);
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

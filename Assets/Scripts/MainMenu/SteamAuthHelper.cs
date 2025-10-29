using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Calls the Supabase Edge Function that verifies a Steam session ticket
/// and returns a signed JWT. Works both in Editor and runtime builds.
/// </summary>
public static class SteamAuthHelper
{
    // Public anon key (safe to embed for client-side access)
    private const string SupabaseAnonKey =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im5zd25qaGVnaWZhdWRzZ2p5cndmIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDI3ODg3MDEsImV4cCI6MjA1ODM2NDcwMX0.c6JxmTv5DUD2ZeocXg1S1MFR_fPSK7zB_CV4swO4sM";

    /// <summary>
    /// Authenticate the Steam session ticket with the configured Supabase Edge Function.
    /// </summary>
    /// <param name="steamId">SteamID64 from Steamworks.</param>
    /// <param name="ticketHex">Hex string of the Steam session ticket.</param>
    /// <param name="isDev">If true, call localhost instead of production Supabase URL.</param>
    /// <param name="onComplete">Callback invoked with true if auth succeeds, false otherwise.</param>
    public static IEnumerator AuthenticateWithEdge(
        string steamId,
        string ticketHex,
        Action<bool> onComplete
    )
    {
        // 🧩 1. Editor bypass
        if (Application.isEditor)
        {
            Debug.Log("[SteamAuthHelper] Editor bypass: treating edge auth as successful.");
            onComplete?.Invoke(true);
            yield break;
        }

        // in the future we could set up local edge functions for fully local development and jwt minting without any supabase
        // example url = "http://localhost:54321/functions/v1/steam-partner"
        // 🧩 2. Target URL
        string url = "https://nswnjhegifaudsgjyrwf.supabase.co/functions/v1/steam-partner";

        // 🧠 send both steamid and ticket (ticket optional for now)
        var jsonBody = $"{{\"steamid\":\"{steamId}\",\"ticket\":\"{ticketHex}\"}}";
        var body = Encoding.UTF8.GetBytes(jsonBody);

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
        {
            uploadHandler = new UploadHandlerRaw(body),
            downloadHandler = new DownloadHandlerBuffer(),
        };

        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("apikey", SupabaseAnonKey);

        Debug.Log($"[SteamAuthHelper] Sending edge auth request to: {url}");
        yield return req.SendWebRequest();

        string responseText = req.downloadHandler?.text ?? string.Empty;
        Debug.Log($"[EdgeAuth] {req.responseCode}: {responseText}");

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning(
                $"[SteamAuthHelper] Edge auth failed. code={req.responseCode} result={req.result}."
            );
            onComplete?.Invoke(false);
            yield break;
        }

        // 🧩 3. Success
        onComplete?.Invoke(true);
    }
}

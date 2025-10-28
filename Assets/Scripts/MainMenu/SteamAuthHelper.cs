using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Helper to call your Supabase Edge Function to validate Steam session tickets.
/// - Bypasses in the Editor (immediately returns success)
/// - Sends Authorization header for non-editor calls
///
/// NOTE: SupabaseTest is responsible for PostSteamId / upsert flows. This helper only
/// verifies the ticket with the edge function and returns a boolean result via the callback.
/// </summary>
public static class SteamAuthHelper
{
    // Dev-time anon key (dev convenience). In production prefer injecting from config.
    private const string supabasePublicAnonAPIKey =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im5zd25qaGVnaWZhdWRzZ2p5cndmIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDI3ODg3MDEsImV4cCI6MjA1ODM2NDcwMX0.c6JxmTv5DUD2ZeocXg1S1MFR_fPSK7zB_CV4swO4sM";

    /// <summary>
    /// Authenticate the Steam session ticket with the configured Edge Function.
    /// - ticketHex: hex string of the Steam session ticket
    /// - isDev: if true, will target the local edge function URL (useful for local testing)
    /// - onComplete: callback invoked with true if edge accepted the ticket, false otherwise
    ///
    /// Behavior:
    /// - If running inside the Unity Editor, this function immediately calls onComplete(true) and exits.
    /// - Otherwise it POSTs to the Edge Function with Content-Type: application/json and Authorization header.
    /// </summary>
    public static IEnumerator AuthenticateWithEdge(
        string ticketHex,
        bool isDev,
        System.Action<bool> onComplete
    )
    {
        // Editor bypass: immediately succeed (SupabaseTest will handle the PostSteamId upsert flow)
        if (Application.isEditor)
        {
            Debug.Log("[SteamAuthHelper] Editor bypass: treating edge auth as successful.");
            onComplete?.Invoke(true);
            yield break;
        }

        string url = isDev
            ? "http://localhost:54321/functions/v1/steam-partner"
            : "https://nswnjhegifaudsgjyrwf.supabase.co/functions/v1/steam-partner";

        var jsonBody = $"{{\"ticket\":\"{ticketHex}\"}}";
        var body = Encoding.UTF8.GetBytes(jsonBody);

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            // Required by Supabase Edge functions – include the anon API key for now.
            req.SetRequestHeader("Authorization", $"Bearer {supabasePublicAnonAPIKey}");
            req.SetRequestHeader("apikey", supabasePublicAnonAPIKey);

            Debug.Log($"[SteamAuthHelper] Sending edge auth request to: {url}");
            yield return req.SendWebRequest();

            bool ok = req.result == UnityWebRequest.Result.Success;
            string responseText = req.downloadHandler != null ? req.downloadHandler.text : "";

            Debug.Log($"[EdgeAuth] {req.responseCode}: {responseText}");

            if (!ok)
            {
                Debug.LogWarning(
                    $"[SteamAuthHelper] Edge auth failed. code={req.responseCode} result={req.result}."
                );
                onComplete?.Invoke(false);
                yield break;
            }

            // If the edge returned success (200), we consider it OK.
            onComplete?.Invoke(true);
        }
    }
}

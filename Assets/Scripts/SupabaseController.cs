using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class SupabaseLevelDTO
{
    public string name;
    public string[] data;
}

public class SupabaseController : MonoBehaviour
{
    public static SupabaseController Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private const string SUPABASE_URL = "https://nswnjhegifaudsgjyrwf.supabase.co";
    private const string SUPABASE_API_KEY =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im5zd25qaGVnaWZhdWRzZ2p5cndmIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDI3ODg3MDEsImV4cCI6MjA1ODM2NDcwMX0.c6JxmTv5DUD2ZeocXg1S1MFR_fPSK7RzB_CV4swO4sM";

    public IEnumerator SaveLevel(SupabaseLevelDTO level)
    {
        string jsonBody = JsonUtility.ToJson(level);

        UnityWebRequest request = new UnityWebRequest($"{SUPABASE_URL}/rest/v1/levels", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("apikey", SUPABASE_API_KEY);
        request.SetRequestHeader("Authorization", "Bearer " + SUPABASE_API_KEY);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Prefer", "return=minimal");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Level saved to Supabase!");
        }
        else
        {
            Debug.LogError(
                "Error saving level: " + request.error + "\n" + request.downloadHandler.text
            );
        }
    }

    public IEnumerator FetchPublishedLevels(Action<List<LevelInfo>> onComplete)
    {
        string url = $"{SUPABASE_URL}/rest/v1/levels?select=id,name,created_at";

        Debug.Log("Fetching Published Levels: " + url);

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("apikey", SUPABASE_API_KEY);
        request.SetRequestHeader("Authorization", $"Bearer {SUPABASE_API_KEY}");

        yield return request.SendWebRequest();

        var results = new List<LevelInfo>();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var json = request.downloadHandler.text;
                var array = JArray.Parse(json);

                foreach (var entry in array)
                {
                    results.Add(
                        new LevelInfo
                        {
                            id = entry["id"]?.ToString(),
                            name = entry["name"]?.ToString(),
                            isLocal = false,
                            created_at = DateTime.Parse(entry["created_at"]?.ToString()),
                        }
                    );
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SupabaseController] JSON parse error: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"[SupabaseController] Fetch failed: {request.error}");
        }

        onComplete?.Invoke(results);
    }

    public IEnumerator LoadLevel(string levelId, System.Action<SupabaseLevelDTO> onSuccess)
    {
        string url = $"{SUPABASE_URL}/rest/v1/levels?id=eq.{levelId}&select=*";
        Debug.Log("Request URL: " + url);

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("apikey", SUPABASE_API_KEY);
        request.SetRequestHeader("Authorization", "Bearer " + SUPABASE_API_KEY);
        request.SetRequestHeader("Accept", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            Debug.Log(json);

            // Supabase returns a JSON array even if there's only one match
            json = json.TrimStart('[').TrimEnd(']');

            if (!string.IsNullOrEmpty(json))
            {
                SupabaseLevelDTO payload = JsonUtility.FromJson<SupabaseLevelDTO>(json);
                onSuccess?.Invoke(payload);
            }
            else
            {
                Debug.LogWarning("Level not found.");
            }
        }
        else
        {
            Debug.LogError(
                "Error loading level: " + request.error + "\n" + request.downloadHandler.text
            );
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class SupabaseLevelDTO
{
    public string name;
    public string[] data;
}

public class SupabaseEditController : MonoBehaviour
{
    public static SupabaseEditController Instance { get; private set; }

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

    public IEnumerator LoadLevel(string levelName, System.Action<SupabaseLevelDTO> onSuccess)
    {
        string url = $"{SUPABASE_URL}/rest/v1/levels?name=eq.{UnityWebRequest.EscapeURL(levelName)}&select=*";

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("apikey", SUPABASE_API_KEY);
        request.SetRequestHeader("Authorization", "Bearer " + SUPABASE_API_KEY);
        request.SetRequestHeader("Accept", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;

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
            Debug.LogError("Error loading level: " + request.error + "\n" + request.downloadHandler.text);
        }
    }

}

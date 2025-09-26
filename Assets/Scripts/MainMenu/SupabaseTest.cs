using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class SupabaseTest : MonoBehaviour
{
    private const string SupabaseUrl =
        "https://nswnjhegifaudsgjyrwf.supabase.co/functions/v1/steam-partner";
    private const string SupabaseKey =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im5zd25qaGVnaWZhdWRzZ2p5cndmIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDI3ODg3MDEsImV4cCI6MjA1ODM2NDcwMX0.c6JxmTv5DUD2ZeocXg1S1MFR_fPSK7RzB_CV4swO4sM";

    void Start()
    {
        StartCoroutine(PostSteamId("76561198000000000"));
    }

    IEnumerator PostSteamId(string steamId)
    {
        var json = JsonUtility.ToJson(new SteamRequest { steamid = steamId });
        var request = new UnityWebRequest(SupabaseUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {SupabaseKey}");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
            Debug.LogError($"Error: {request.error} - {request.downloadHandler.text}");
        else
            Debug.Log($"Response: {request.downloadHandler.text}");
    }

    [System.Serializable]
    public class SteamRequest
    {
        public string steamid;
    }
}

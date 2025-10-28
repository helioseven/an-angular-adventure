using Steamworks;
using UnityEngine;

public class SteamInitTest : MonoBehaviour
{
    void Start()
    {
        if (SteamAPI.Init())
            Debug.Log($"✅ Steam initialized: {SteamUser.GetSteamID()}");
        else
            Debug.LogError("❌ Steam failed to initialize.");
    }
}

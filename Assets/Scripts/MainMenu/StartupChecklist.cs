using System.Collections;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Displays the full startup/auth checklist on the main menu.
/// Works in Editor (dev bypass) and in real Steam builds.
/// Uses AuthState as the single source of truth.
///
/// NOTE: ASCII-only for now. Add emojis or TMP sprites later if desired.
/// </summary>
public class StartupChecklist : MonoBehaviour
{
    [Header("Checklist UI")]
    [SerializeField]
    private TMP_Text stepSteamInit;

    [SerializeField]
    private TMP_Text stepSessionTicket;

    [SerializeField]
    private TMP_Text stepEdgeAuth;

    [SerializeField]
    private TMP_Text stepJWTVerified;

    [SerializeField]
    private TMP_Text finalUserInfo;

    [SerializeField]
    private RawImage avatarImage;

    private bool steamInitialized;
    private bool isEditor => Application.isEditor;
    private string ticketHex = "";

    private void Start()
    {
        AuthState.OnChanged += RefreshUI;
        StartCoroutine(RunStartupSequence());
    }

    private void OnDestroy()
    {
        AuthState.OnChanged -= RefreshUI;
    }

    private void OnApplicationQuit()
    {
        if (steamInitialized)
        {
            SteamAPI.Shutdown();
            steamInitialized = false;
        }
    }

    private IEnumerator RunStartupSequence()
    {
        stepSteamInit.text = "Steam Init: ...";
        stepSessionTicket.text = "Session Ticket: ...";
        stepEdgeAuth.text = "Edge Auth: ...";
        stepJWTVerified.text = "JWT Verified: ...";
        finalUserInfo.text = "";

        yield return StartCoroutine(InitializeSteam());
        yield return StartCoroutine(GetSessionTicket());
        yield return StartCoroutine(AuthenticateAndFetchUser());
    }

    // --- Step 1: Steam Init ---
    private IEnumerator InitializeSteam()
    {
        if (isEditor)
        {
            steamInitialized = true;
            stepSteamInit.text = "Steam Init: y (Editor bypass)";
            yield break;
        }

        try
        {
            steamInitialized = SteamAPI.Init();
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Steam] Init exception: " + e);
            steamInitialized = false;
        }

        stepSteamInit.text = steamInitialized ? "Steam Init: y" : "Steam Init: x";
        yield return null;
    }

    // --- Step 2: Session Ticket ---
    private IEnumerator GetSessionTicket()
    {
        if (!steamInitialized)
        {
            stepSessionTicket.text = "Session Ticket: x (Steam not ready)";
            yield break;
        }

        if (isEditor)
        {
            ticketHex = "DEV_TICKET";
            stepSessionTicket.text = "Session Ticket: y (Bypassed)";
            yield break;
        }

        byte[] ticketData = new byte[2048];
        uint ticketSize = 0;
        var identity = new SteamNetworkingIdentity();

        HAuthTicket handle = SteamUser.GetAuthSessionTicket(
            ticketData,
            ticketData.Length,
            out ticketSize,
            ref identity
        );

        if (handle == HAuthTicket.Invalid || ticketSize == 0)
        {
            stepSessionTicket.text = "Session Ticket: x (Invalid)";
            Debug.LogError("[Steam] Failed to get auth session ticket.");
            yield break;
        }

        ticketHex = System
            .BitConverter.ToString(ticketData, 0, (int)ticketSize)
            .Replace("-", string.Empty);
        stepSessionTicket.text = $"Session Ticket: y ({ticketSize} bytes)";
        Debug.Log($"[Steam] Got session ticket ({ticketSize} bytes)");
        yield return null;
    }

    // --- Step 3: Edge Auth + Supabase JWT ---
    private IEnumerator AuthenticateAndFetchUser()
    {
        bool edgeOk = false;
        string steamIdText = isEditor
            ? SupabaseTest.Instance.testSteamId
            : SteamUser.GetSteamID().ToString();

        // In Editor, bypass edge auth entirely.
        if (isEditor)
        {
            stepEdgeAuth.text = "Edge Auth: y (Editor bypass)";
            edgeOk = true;
        }
        else
        {
            yield return StartCoroutine(
                SteamAuthHelper.AuthenticateWithEdge(steamIdText, ticketHex, ok => edgeOk = ok)
            );
            stepEdgeAuth.text = edgeOk ? "Edge Auth: y" : "Edge Auth: x";
        }

        if (!edgeOk)
        {
            stepJWTVerified.text = "JWT Verified: x";
            finalUserInfo.text = "Edge Auth failed.";
            yield break;
        }

        // Step 4: Call SupabaseTest to mint JWT and populate AuthState
        yield return StartCoroutine(SupabaseTest.Instance.PostSteamId(steamIdText));

        if (!string.IsNullOrEmpty(AuthState.Jwt))
        {
            // Sneak peek: first 10 + last 8 chars only
            string jwt = AuthState.Jwt;
            string preview =
                jwt.Length > 20 ? $"{jwt.Substring(0, 10)}...{jwt.Substring(jwt.Length - 8)}" : jwt;

            stepJWTVerified.text = $"JWT Verified: y ({preview})";
            Debug.Log($"[StartupChecklist] JWT verified → {preview}");
        }
        else
        {
            stepJWTVerified.text = "JWT Verified: x (No token)";
            Debug.LogWarning("[StartupChecklist] No JWT returned from SupabaseTest.");
        }

        // Step 5: Update UI and avatar
        RefreshUI();

        if (!string.IsNullOrEmpty(AuthState.AvatarUrl))
        {
            yield return StartCoroutine(LoadAvatarFromUrl(AuthState.AvatarUrl));
        }
        else if (!isEditor)
        {
            yield return StartCoroutine(LoadSteamAvatar(SteamUser.GetSteamID()));
        }
    }

    // --- Helper: Load Avatar from Steam ---
    private IEnumerator LoadSteamAvatar(CSteamID steamID)
    {
        int avatarInt = SteamFriends.GetLargeFriendAvatar(steamID);
        if (avatarInt == -1)
        {
            Debug.LogWarning("[Steam] No large avatar found.");
            yield break;
        }

        uint width,
            height;
        if (!SteamUtils.GetImageSize(avatarInt, out width, out height))
            yield break;

        byte[] image = new byte[4 * width * height];
        if (!SteamUtils.GetImageRGBA(avatarInt, image, image.Length))
            yield break;

        Texture2D tex = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
        tex.LoadRawTextureData(image);
        tex.Apply();

        avatarImage.texture = tex;
        Debug.Log("[Steam] Avatar loaded from Steam.");
    }

    // --- Helper: Load Avatar from Supabase URL ---
    private IEnumerator LoadAvatarFromUrl(string url)
    {
        using (var www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                avatarImage.texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                Debug.Log("[Supabase] Avatar loaded from URL.");
            }
            else
            {
                Debug.LogWarning(
                    "[Supabase] Avatar download failed, falling back to Steam avatar."
                );
            }
        }
    }

    // --- Update UI when AuthState changes ---
    private void RefreshUI()
    {
        string name = AuthState.PersonaName ?? "(unknown)";
        string id = AuthState.SteamId ?? "(no id)";
        finalUserInfo.text = $"User: {name}\nSteamID: {id}";
    }
}

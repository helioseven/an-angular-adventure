using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
#if !UNITY_IOS
// conditionally add steamworks to desktop only (must be unity native define symbols - STEAMWORKS_NET will not work)
using Steamworks;
#endif

public class AccountIndicator : MonoBehaviour
{
    private const string SignedOutLabel = "Not signed in";

    [Header("UI")]
    [SerializeField]
    private TMP_Text accountIndicatorText;

    [SerializeField]
    private RawImage avatarImage;

    [SerializeField]
    private float spinSpeed = 180f; // degrees per second

    [SerializeField]
    private Vector2 avatarSize = new Vector2(64, 64);

    private bool steamInitialized;
    private bool isEditor => Application.isEditor;
    private string ticketHex = "";
    private bool isLoading = true;
    private bool lastAuthSucceeded;
    private bool isSubscribed;

#if !UNITY_IOS
    private void Start()
    {
        if (StartupManager.DemoModeEnabled)
        {
            isLoading = false;
            ClearAvatarTexture();
            if (avatarImage != null)
                avatarImage.gameObject.SetActive(false);
            SetStatus("Tessel Run Demo");
            return;
        }

        AuthState.Instance.OnChanged += RefreshUI;
        isSubscribed = true;
        RefreshUI();

        if (!string.IsNullOrEmpty(AuthState.Instance.Jwt) && !AuthState.Instance.IsTokenExpired())
        {
            Debug.Log("[AccountIndicator] JWT present in memory - same session, skipping re-auth.");

            // Skip login, but still finalize the display
            StartCoroutine(FinalizeUserDisplay());
            isLoading = false;
        }
        else
        {
            if (AuthState.Instance.IsTokenExpired())
                AuthState.Instance.SetJwt("");
            // Fresh boot - run full Steam + Edge + Supabase sequence
            StartCoroutine(RunStartupSequence());
        }
    }

    private void OnDestroy()
    {
        if (isSubscribed && AuthState.Instance != null)
            AuthState.Instance.OnChanged -= RefreshUI;
    }

    private void Update()
    {
        // Rotate avatar while loading
        if (isLoading && avatarImage != null)
        {
            avatarImage.transform.Rotate(Vector3.forward, spinSpeed * Time.deltaTime);
        }
    }

    private IEnumerator RunStartupSequence()
    {
        isLoading = true;
        ResetAvatarRotation();

        SetStatus("Starting Steam...");
        yield return StartCoroutine(InitializeSteam());

        if (!steamInitialized && !isEditor)
        {
            HandleAuthFailure("Couldn't start Steam. Please restart.");
            yield break;
        }

        ticketHex = "";
        SetStatus("Requesting a session ticket...");
        yield return StartCoroutine(GetSessionTicket());

        if (string.IsNullOrEmpty(ticketHex))
        {
            HandleAuthFailure("Couldn't get a Steam ticket.");
            yield break;
        }

        SetStatus("Signing you in...");
        yield return StartCoroutine(AuthenticateAndFetchUser());

        if (!lastAuthSucceeded)
        {
            HandleAuthFailure("Sign-in failed. Please try again.");
            yield break;
        }

        SetStatus("Loading your profile...");
        yield return StartCoroutine(FinalizeUserDisplay());

        // Stop spinning and finalize
        isLoading = false;
        ResetAvatarRotation();

        string name = string.IsNullOrEmpty(AuthState.Instance.PersonaName)
            ? SignedOutLabel
            : AuthState.Instance.PersonaName;
        SetStatus(name);
    }

    // --- Step 1: Steam Init ---
    private IEnumerator InitializeSteam()
    {
        if (isEditor)
        {
            steamInitialized = true;
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

        yield return null;
    }

    // --- Step 2: Session Ticket ---
    private IEnumerator GetSessionTicket()
    {
        ticketHex = "";

        if (!steamInitialized && !isEditor)
        {
            SetStatus("Steam not initialized.");
            yield break;
        }

        if (isEditor)
        {
            ticketHex = "DEV_TICKET";
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
            SetStatus("Failed to get ticket.");
            ticketHex = "";
            yield break;
        }

        ticketHex = System
            .BitConverter.ToString(ticketData, 0, (int)ticketSize)
            .Replace("-", string.Empty);
        yield return null;
    }

    // --- Step 3: Edge Auth + Supabase JWT ---
    private IEnumerator AuthenticateAndFetchUser()
    {
        lastAuthSucceeded = false;
        bool edgeOk = false;
        string steamIdText = isEditor
            ? StartupManager.Instance.testSteamId
            : SteamUser.GetSteamID().ToString();

        if (isEditor)
        {
            edgeOk = true;
        }
        else
        {
            yield return StartCoroutine(
                SteamAuthHelper.AuthenticateWithEdge(steamIdText, ticketHex, ok => edgeOk = ok)
            );
        }

        if (!edgeOk)
        {
            SetStatus("Edge auth failed.");
            yield break;
        }

        yield return StartCoroutine(StartupManager.Instance.PostSteamId(steamIdText, ticketHex));

        lastAuthSucceeded =
            !string.IsNullOrEmpty(AuthState.Instance.SteamId)
            || !string.IsNullOrEmpty(AuthState.Instance.PersonaName)
            || !string.IsNullOrEmpty(AuthState.Instance.Jwt);
        yield return null;
    }

    // --- Step 4: Load Avatar + Final UI ---
    private IEnumerator FinalizeUserDisplay()
    {
        RefreshUI();

        if (!string.IsNullOrEmpty(AuthState.Instance.AvatarUrl))
        {
            yield return StartCoroutine(LoadAvatarFromUrl(AuthState.Instance.AvatarUrl));
        }
        else if (!isEditor && steamInitialized)
        {
            yield return StartCoroutine(LoadSteamAvatar(SteamUser.GetSteamID()));
        }
        else
        {
            ClearAvatarTexture();
        }
    }

    private IEnumerator LoadSteamAvatar(CSteamID steamID)
    {
        int avatarInt = SteamFriends.GetLargeFriendAvatar(steamID);
        if (avatarInt == -1)
            yield break;

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
        ApplyAvatarSizing();
        yield return null;
    }

    private IEnumerator LoadAvatarFromUrl(string url)
    {
        using (var www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                avatarImage.texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                ApplyAvatarSizing();
            }
            else
            {
                Debug.LogWarning($"[AccountIndicator] Avatar download failed: {www.error}");
                ClearAvatarTexture();
            }
        }
    }

    private void RefreshUI()
    {
        if (accountIndicatorText == null)
            return;

        string displayName = string.IsNullOrEmpty(AuthState.Instance.PersonaName)
            ? SignedOutLabel
            : AuthState.Instance.PersonaName;

        accountIndicatorText.text = displayName;
    }

    private void SetStatus(string msg)
    {
        if (accountIndicatorText != null)
            accountIndicatorText.text = msg;

        Debug.Log($"[AccountIndicator] {msg}");
    }

    private void HandleAuthFailure(string message)
    {
        isLoading = false;
        ResetAvatarRotation();
        ClearAvatarTexture();
        AuthState.Instance.Clear();
        SetStatus(message);
    }

    private void ResetAvatarRotation()
    {
        if (avatarImage != null)
        {
            avatarImage.transform.rotation = Quaternion.identity;
        }
    }

    private void ClearAvatarTexture()
    {
        if (avatarImage != null)
        {
            avatarImage.texture = null;
        }
    }

    private void ApplyAvatarSizing()
    {
        if (avatarImage != null)
        {
            avatarImage.rectTransform.sizeDelta = avatarSize;
        }
    }

#endif // corresponds to #if UNITY_IOS
}

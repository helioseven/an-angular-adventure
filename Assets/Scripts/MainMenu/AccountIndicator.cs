// conditionally add steamworks to desktop only (must be unity native define symbols - the user defined STEAMWORKS_NET will not work)
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if !UNITY_IOS && !UNITY_ANDROID
using Steamworks;
#endif

public class AccountIndicator : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private TMP_Text accountIndicatorText;

    [SerializeField]
    private RawImage avatarImage;

    [SerializeField]
    private float spinSpeed = 180f; // degrees per second

    private bool steamInitialized;
    private bool isEditor => Application.isEditor;
    private string ticketHex = "";
    private bool isLoading = true;

#if !STEAMWORKS_NET
    private void Start()
    {
        AuthState.Instance.OnChanged += RefreshUI;

        if (!string.IsNullOrEmpty(AuthState.Instance.Jwt))
        {
            Debug.Log("[AccountIndicator] JWT present in memory — same session, skipping re-auth.");

            // Skip login, but still finalize the display
            StartCoroutine(FinalizeUserDisplay());
            isLoading = false;
        }
        else
        {
            // Fresh boot → run full Steam → Edge → Supabase sequence
            StartCoroutine(RunStartupSequence());
        }
    }

    private void OnDestroy()
    {
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
        SetStatus("Initializing Steam…");
        yield return StartCoroutine(InitializeSteam());

        SetStatus("Getting session ticket…");
        yield return StartCoroutine(GetSessionTicket());

        SetStatus("Authenticating with Edge Function…");
        yield return StartCoroutine(AuthenticateAndFetchUser());

        SetStatus("Fetching profile and avatar…");
        yield return StartCoroutine(FinalizeUserDisplay());

        // Stop spinning and finalize
        isLoading = false;
        avatarImage.transform.rotation = Quaternion.identity;

        string name = AuthState.Instance.PersonaName ?? "(unknown)";
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
        bool edgeOk = false;
        string steamIdText = isEditor
            ? SupabaseTest.Instance.testSteamId
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

        yield return StartCoroutine(SupabaseTest.Instance.PostSteamId(steamIdText));
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
            }
        }
    }

    private void RefreshUI()
    {
        accountIndicatorText.text = AuthState.Instance.PersonaName ?? "(unknown)";
    }

    private void SetStatus(string msg)
    {
        accountIndicatorText.text = msg;
        Debug.Log($"[AccountIndicator] {msg}");
    }
    // #endif for #if STEAMWORKS_NET
#endif
}

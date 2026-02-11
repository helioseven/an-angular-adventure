using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#if !UNITY_IOS
using Steamworks;
#endif

public class MenuGM : MonoBehaviour
{
    // Edit button ref
    public Button editButton;

    // EditLoader ref
    public GameObject editLoader;

    // Play button ref
    public Button playButton;

    // PlayLoader ref
    public GameObject playLoader;

    // Browse Menu ref
    public Button browseButton;
    public Button quitButton;
    public Button settingsButton;
    public Button howToPlayButton;
    public GameObject Logo;

    // Panels
    public GameObject browsePanel;
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject accountPanel;

    [Header("Onboarding")]
    public WelcomeModal welcomeModal;
    public HowToPlayModal howToPlayModal;
    public Button tutorialButton;
    public Button discordButton;
    public Button wishlistButton;
    public string tutorialLevelName = "Menu Mayhem";
    public string discordUrl = "https://discord.gg/WR6Qy3dPvg";
    public uint wishlistAppId = 3661660;
    public string wishlistUrl = "https://store.steampowered.com/app/3661660/Tessel_Run/";
    public bool showWelcomeOnFirstLaunch = true;
    public string welcomeHeader = "Welcome to Tessel Run!";

    [TextArea(3, 8)]
    public string welcomeBody = "";
    public string howToPlayHeader = "How to Play";

    [TextArea(4, 10)]
    public string howToPlayBody =
        "Roll, jump, and weave your way through tessellated courses.\n\n"
        + "Keyboard/Mouse:\n"
        + "  - Move: WASD\n"
        + "  - Jump: Space\n"
        + "  - Reset: R\n\n"
        + "Controller:\n"
        + "  - Move: Left Stick\n"
        + "  - Jump: South Button\n"
        + "  - Reset: Select/Back";

    [Header("Physics (Main Menu Only)")]
    public MenuUIPhysicsProxy physicsProxy;
    public MenuUIPhysicsBall physicsBall;
    public RectTransform mainMenuPhysicsRoot;
    public RectTransform browsePhysicsRoot;
    public RectTransform settingsPhysicsRoot;
    public RectTransform browsePhysicsViewport;

    // private variables
    private GameObject[] menuPanels;
    private MenuInputModeAdapter mainMenuAdapter;
    private CanvasGroup mainMenuCanvasGroup;

    void Awake()
    {
        if (!physicsProxy)
        {
            physicsProxy = FindAnyObjectByType<MenuUIPhysicsProxy>();
        }
        if (!physicsBall)
        {
            physicsBall = FindAnyObjectByType<MenuUIPhysicsBall>();
        }
        if (!mainMenuPhysicsRoot && mainMenuPanel)
        {
            mainMenuPhysicsRoot = mainMenuPanel.GetComponent<RectTransform>();
        }
        if (!browsePhysicsRoot && browsePanel)
        {
            LevelBrowser browser = browsePanel.GetComponentInChildren<LevelBrowser>(true);
            if (browser && browser.levelListContent)
            {
                browsePhysicsRoot = browser.levelListContent as RectTransform;
            }

            if (browser && !browsePhysicsViewport)
            {
                ScrollRect scrollRect = browser.GetComponentInParent<ScrollRect>(true);
                if (scrollRect && scrollRect.viewport)
                {
                    browsePhysicsViewport = scrollRect.viewport;
                }
            }
        }
        if (!settingsPhysicsRoot && settingsPanel)
        {
            settingsPhysicsRoot = settingsPanel.GetComponent<RectTransform>();
        }

        if (physicsProxy)
        {
            physicsProxy.RegisterGroup("MainMenu", mainMenuPhysicsRoot);
            physicsProxy.RegisterGroup("Browse", browsePhysicsRoot);
            physicsProxy.RegisterGroup("Settings", settingsPhysicsRoot);

            if (browsePhysicsViewport)
            {
                physicsProxy.SetGroupVisibilityRect("Browse", browsePhysicsViewport);
            }

            if (physicsBall)
            {
                Transform mainMenuPhysicsContainer = physicsProxy.GetGroupContainer("MainMenu");
                if (mainMenuPhysicsContainer)
                {
                    physicsBall.SetPhysicsRoot(mainMenuPhysicsContainer, true);
                }
            }
        }

        if (!welcomeModal)
            welcomeModal = FindAnyObjectByType<WelcomeModal>();
        if (!howToPlayModal)
            howToPlayModal = FindAnyObjectByType<HowToPlayModal>();

        playButton.onClick.AddListener(StartPlay);
        editButton.onClick.AddListener(StartEdit);
        browseButton.onClick.AddListener(OpenLevelBrowser);
        quitButton.onClick.AddListener(Quit);
        settingsButton.onClick.AddListener(OpenSettingsMenu);
        // howToPlay.onClick.AddListener(OpenAccountMenu);
        if (tutorialButton != null)
        {
            bool isModalButton =
                welcomeModal != null && tutorialButton.transform.IsChildOf(welcomeModal.transform);
            if (!isModalButton)
                tutorialButton.onClick.AddListener(StartTutorial);
        }
        if (howToPlayButton != null)
            howToPlayButton.onClick.AddListener(ShowHowToPlay);
        if (discordButton != null)
        {
            bool isModalButton =
                welcomeModal != null && discordButton.transform.IsChildOf(welcomeModal.transform);
            if (!isModalButton)
            {
                discordButton.gameObject.SetActive(StartupManager.DemoModeEnabled);
                if (StartupManager.DemoModeEnabled)
                    discordButton.onClick.AddListener(OpenDiscord);
            }
        }

        if (wishlistButton != null)
        {
            bool isModalButton =
                welcomeModal != null && wishlistButton.transform.IsChildOf(welcomeModal.transform);
            if (!isModalButton)
            {
                wishlistButton.gameObject.SetActive(StartupManager.DemoModeEnabled);
                if (StartupManager.DemoModeEnabled)
                    wishlistButton.onClick.AddListener(OpenWishlist);
            }
        }
        menuPanels = new GameObject[] { mainMenuPanel, browsePanel, settingsPanel, accountPanel };

        // turn off create mode for demo
        if (StartupManager.DemoModeEnabled)
        {
            editButton.gameObject.SetActive(false);
        }

        // Only keep Play button on iOS for now
#if UNITY_IOS
        editButton.gameObject.SetActive(false);
        settingsButton.gameObject.SetActive(false);
        howToPlay.gameObject.SetActive(false);
        browseButton.gameObject.SetActive(false);
        quitButton.gameObject.SetActive(false);
#endif

        if (physicsProxy)
        {
            physicsProxy.RebuildGroup("MainMenu");
        }

        OpenMainMenu();
    }

    void Start()
    {
        InputManager.Instance.SetSceneInputs("MainMenu");

        if (showWelcomeOnFirstLaunch && StartupManager.DemoModeEnabled)
            StartCoroutine(ShowWelcomeNextFrame());
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
            return;

        bool closedModal = false;
        if (welcomeModal != null && welcomeModal.gameObject.activeSelf)
        {
            MarkWelcomeSeen();
            welcomeModal.Hide();
            closedModal = true;
        }

        if (howToPlayModal != null && howToPlayModal.gameObject.activeSelf)
        {
            howToPlayModal.Hide();
            closedModal = true;
        }

        if (closedModal)
            SetMainMenuAdapterEnabled(true);
    }

    public void SwitchToMenu(GameObject targetPanel)
    {
        foreach (GameObject panel in menuPanels)
        {
            panel.SetActive(panel == targetPanel);
        }

        bool isMainMenu = targetPanel == mainMenuPanel;
        bool isBrowse = targetPanel == browsePanel;
        bool isSettings = targetPanel == settingsPanel;

        if (physicsProxy)
        {
            physicsProxy.SetGroupActive("MainMenu", isMainMenu);
            physicsProxy.SetGroupActive("Browse", isBrowse);
            physicsProxy.SetGroupActive("Settings", isSettings);
        }
        SetPhysicsActive(isMainMenu || isBrowse || isSettings);

        InputModeTracker.EnsureInstance();
        var adapter = targetPanel.GetComponent<MenuInputModeAdapter>();
        if (adapter == null)
            adapter = targetPanel.AddComponent<MenuInputModeAdapter>();
        adapter.SetScope(targetPanel.transform);
        if (targetPanel == mainMenuPanel)
            adapter.SetPreferred(playButton);
        else
            adapter.SetPreferred(null);

        if (
            InputModeTracker.Instance != null
            && InputModeTracker.Instance.CurrentMode == InputMode.Navigation
        )
        {
            if (targetPanel == browsePanel)
                return;
            if (targetPanel == mainMenuPanel)
                MenuFocusUtility.SelectPreferred(targetPanel, playButton);
            else
                MenuFocusUtility.SelectPreferred(targetPanel);
        }
    }

    public void OpenLevelBrowser()
    {
        SwitchToMenu(browsePanel);
    }

    public void OpenMainMenu()
    {
        ForceMenuGravityDown();
        StopGameplayLoopingSounds();
        SwitchToMenu(mainMenuPanel);
    }

    public void OpenSettingsMenu()
    {
        SwitchToMenu(settingsPanel);
    }

    private void SetPhysicsActive(bool active)
    {
        if (physicsProxy)
        {
            physicsProxy.SetPhysicsActive(active);
        }

        if (physicsBall)
        {
            physicsBall.SetPhysicsActive(active && mainMenuPanel && mainMenuPanel.activeSelf);
        }
    }

    public void RebuildBrowsePhysicsProxies()
    {
        if (physicsProxy)
        {
            physicsProxy.RebuildGroup("Browse");
        }
    }

    /* Private Functions */
    private void StartPlay()
    {
        // start the play scene with the next bundled level without a best time
        var loaderGO = Instantiate(playLoader);
        var loader = loaderGO.GetComponent<PlayLoader>();
        LevelInfo next = LevelStorage.GetNextBundledLevelByBestTime();
        loader.levelInfo = next ?? new LevelInfo { name = "", isLocal = true };
        loader.playModeContext = PlayGM.PlayModeContext.FromMainMenuPlayButton;
    }

    public void StartTutorial()
    {
        if (playLoader == null)
        {
            Debug.LogWarning("[MenuGM] Cannot start tutorial: playLoader is not assigned.");
            return;
        }

        var loaderGO = Instantiate(playLoader);
        var loader = loaderGO.GetComponent<PlayLoader>();
        loader.levelInfo = ResolveTutorialLevelInfo();
        loader.playModeContext = PlayGM.PlayModeContext.FromMainMenuPlayButton;
        MarkWelcomeSeen();
    }

    public void ShowHowToPlay()
    {
        if (howToPlayModal == null)
        {
            Debug.LogWarning("[MenuGM] Cannot show How To Play: howToPlayModal is missing.");
            return;
        }

        SetMainMenuAdapterEnabled(false);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (!howToPlayModal.gameObject.activeSelf)
            howToPlayModal.gameObject.SetActive(true);

        howToPlayModal.Show(
            header: howToPlayHeader,
            body: howToPlayBody,
            confirmAction: () =>
            {
                StartTutorial();
                SetMainMenuAdapterEnabled(true);
            },
            cancelAction: () =>
            {
                SetMainMenuAdapterEnabled(true);
            }
        );
    }

    private void OpenDiscord()
    {
        if (string.IsNullOrWhiteSpace(discordUrl))
        {
            Debug.LogWarning("[MenuGM] Discord URL is not configured.");
            return;
        }

        Application.OpenURL(discordUrl);
    }

    private void OpenWishlist()
    {
#if !UNITY_IOS
        try
        {
            AppId_t appId = wishlistAppId != 0 ? new AppId_t(wishlistAppId) : SteamUtils.GetAppID();
            SteamFriends.ActivateGameOverlayToStore(
                appId,
                EOverlayToStoreFlag.k_EOverlayToStoreFlag_None
            );
            return;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[MenuGM] Steam overlay failed, falling back to URL. {ex.Message}");
        }
#endif

        if (string.IsNullOrWhiteSpace(wishlistUrl))
        {
            Debug.LogWarning("[MenuGM] Wishlist URL is not configured.");
            return;
        }

        Application.OpenURL(wishlistUrl);
    }

    private IEnumerator ShowWelcomeNextFrame()
    {
        yield return null;

        if (PlayerPrefs.GetInt(StartupManager.WelcomeSeenKey, 0) == 1)
            yield break;
        if (welcomeModal == null)
            yield break;

        if (!welcomeModal.gameObject.activeSelf)
            welcomeModal.gameObject.SetActive(true);

        SetMainMenuAdapterEnabled(false);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        welcomeModal.Show(
            header: welcomeHeader,
            body: welcomeBody,
            confirmAction: () =>
            {
                MarkWelcomeSeen();
                SetMainMenuAdapterEnabled(true);
            },
            cancelAction: () =>
            {
                MarkWelcomeSeen();
                SetMainMenuAdapterEnabled(true);
            },
            discordAction: () =>
            {
                OpenDiscord();
            },
            wishlistAction: () =>
            {
                OpenWishlist();
            }
        );
    }

    private void MarkWelcomeSeen()
    {
        PlayerPrefs.SetInt(StartupManager.WelcomeSeenKey, 1);
        PlayerPrefs.Save();
    }

    private void SetMainMenuAdapterEnabled(bool enabled)
    {
        if (mainMenuPanel == null)
            return;

        if (mainMenuAdapter == null)
            mainMenuAdapter = mainMenuPanel.GetComponent<MenuInputModeAdapter>();

        if (mainMenuAdapter != null)
            mainMenuAdapter.enabled = enabled;

        if (mainMenuCanvasGroup == null)
            mainMenuCanvasGroup = mainMenuPanel.GetComponent<CanvasGroup>();
        if (mainMenuCanvasGroup == null)
            mainMenuCanvasGroup = mainMenuPanel.AddComponent<CanvasGroup>();
        mainMenuCanvasGroup.interactable = enabled;
        mainMenuCanvasGroup.blocksRaycasts = enabled;
    }

    private LevelInfo ResolveTutorialLevelInfo()
    {
        if (!string.IsNullOrWhiteSpace(tutorialLevelName))
        {
            var bundled = LevelStorage.LoadBundledLevelMetadata();
            var match = bundled.Find(info =>
                string.Equals(info.name, tutorialLevelName, StringComparison.OrdinalIgnoreCase)
            );
            if (match != null)
                return match;
        }

        LevelInfo fallback = LevelStorage.GetNextBundledLevelByBestTime();
        if (fallback != null)
            return fallback;

        Debug.LogWarning(
            "[MenuGM] No bundled tutorial level found, falling back to default play level."
        );
        return new LevelInfo { name = "", isLocal = true };
    }

    private void StartEdit()
    {
        // fire off an empty editing scene (it will load the default creation level)
        Instantiate(editLoader);
    }

    private void Quit()
    {
        Debug.Log("You Quitter");
        Application.Quit();
    }

    private static void ForceMenuGravityDown()
    {
        Physics2D.gravity = new Vector2(0f, -9.81f);
    }

    private static void StopGameplayLoopingSounds()
    {
        if (PlayGM.instance != null && PlayGM.instance.player != null)
        {
            PlayGM.instance.player.StopAirWooshSound();
            PlayGM.instance.player.StopRollingSound();
            return;
        }

        if (SoundManager.instance != null)
        {
            SoundManager.instance.StopSound("air-woosh");
            SoundManager.instance.StopSound("rolling-soft");
            SoundManager.instance.StopSound("rolling-loud");
        }
    }
}

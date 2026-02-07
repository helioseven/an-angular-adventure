using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

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
    public Button accountButton;
    public GameObject Logo;

    // Panels
    public GameObject browsePanel;
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject accountPanel;

    [Header("Physics (Main Menu Only)")]
    public MenuUIPhysicsProxy physicsProxy;
    public MenuUIPhysicsBall physicsBall;
    public RectTransform mainMenuPhysicsRoot;
    public RectTransform browsePhysicsRoot;
    public RectTransform settingsPhysicsRoot;
    public RectTransform browsePhysicsViewport;

    // private variables
    private GameObject[] menuPanels;

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

        playButton.onClick.AddListener(StartPlay);
        editButton.onClick.AddListener(StartEdit);
        browseButton.onClick.AddListener(OpenLevelBrowser);
        quitButton.onClick.AddListener(Quit);
        settingsButton.onClick.AddListener(OpenSettingsMenu);
        accountButton.onClick.AddListener(OpenAccountMenu);
        menuPanels = new GameObject[] { mainMenuPanel, browsePanel, settingsPanel, accountPanel };

        if (StartupManager.DemoModeEnabled)
        {
            editButton.gameObject.SetActive(false);
            accountButton.gameObject.SetActive(false);
        }

        // Only keep Play button on iOS for now
#if UNITY_IOS
        editButton.gameObject.SetActive(false);
        settingsButton.gameObject.SetActive(false);
        accountButton.gameObject.SetActive(false);
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
        SwitchToMenu(mainMenuPanel);
    }

    public void OpenSettingsMenu()
    {
        SwitchToMenu(settingsPanel);
    }

    public void OpenAccountMenu()
    {
        SwitchToMenu(accountPanel);
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
}

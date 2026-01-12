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

    // private variables
    private GameObject[] menuPanels;

    void Awake()
    {
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

    /* Private Functions */
    private void StartPlay()
    {
        // start the play scene with the next bundled level without a best time
        var loaderGO = Instantiate(playLoader);
        var loader = loaderGO.GetComponent<PlayLoader>();
        LevelInfo next = null;
        List<LevelInfo> bundled = LevelStorage.LoadBundledLevelMetadata();

        foreach (LevelInfo info in bundled)
        {
            if (!BestTimeStore.TryGetBestTime(info.name, info.dataHash, out _))
            {
                next = info;
                break;
            }
        }

        if (next == null && bundled.Count > 0)
            next = bundled[UnityEngine.Random.Range(0, bundled.Count)];

        if (next == null)
            next = new LevelInfo { name = "", isLocal = true };

        loader.levelInfo = next;
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

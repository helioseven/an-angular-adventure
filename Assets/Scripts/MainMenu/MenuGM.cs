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
    public GameObject Logo;

    // Panels
    public GameObject browsePanel;
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;

    // private variables
    private GameObject[] menuPanels;

    void Awake()
    {
        playButton.onClick.AddListener(StartPlay);
        editButton.onClick.AddListener(StartEdit);
        browseButton.onClick.AddListener(OpenLevelBrowser);
        quitButton.onClick.AddListener(Quit);
        settingsButton.onClick.AddListener(OpenSettingsMenu);
        menuPanels = new GameObject[] { mainMenuPanel, browsePanel, settingsPanel };
        OpenMainMenu();
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

    /* Private Functions */
    private void StartPlay()
    {
        // start the play scene and set the levelName to default
        var loaderGO = Instantiate(playLoader);
        var loader = loaderGO.GetComponent<PlayLoader>();

        // use dummy default level info to load us into the default level
        loader.levelInfo = new LevelInfo
        {
            id = "",
            name = "default",
            isLocal = true,
            created_at = DateTime.MinValue,
        };
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

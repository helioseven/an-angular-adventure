using System;
using System.Collections;
using System.Collections.Generic;
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
    public GameObject Logo;

    // Panels
    public GameObject levelBrowserPanel;
    public GameObject mainMenuPanel;

    void Awake()
    {
        playButton.onClick.AddListener(StartPlay);
        editButton.onClick.AddListener(StartEdit);
        browseButton.onClick.AddListener(OpenLevelBrowser);
        quitButton.onClick.AddListener(Quit);
    }

    /* Private Functions */

    private void OpenLevelBrowser()
    {
        levelBrowserPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
        Logo.SetActive(false);
    }

    public void OpenMainMenu()
    {
        levelBrowserPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
        Logo.SetActive(true);
    }

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

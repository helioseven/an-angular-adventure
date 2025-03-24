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

    public GameObject levelBrowserPanel;
    public GameObject mainMenuPanel;


    void Awake()
    {
        playButton.onClick.AddListener(StartPlay);
        editButton.onClick.AddListener(StartEdit);
        browseButton.onClick.AddListener(OpenLevelBrowser);
    }
    void Start()
    {
        List<LevelInfo> drafts = LevelStorage.LoadLocalLevelMetadata();
        foreach (var level in drafts)
        {
            Debug.Log($"Draft: {level.name} (Last Modified: {level.lastModified})");
        }
    }
    /* Private Functions */

    private void OpenLevelBrowser()
    {
        Debug.Log("asdff");
        levelBrowserPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
    }

    private void StartPlay()
    {
        var loaderGO = Instantiate(playLoader);
        var loader = loaderGO.GetComponent<PlayLoader>();
        loader.levelName = "default";
    }

    private void StartEdit()
    {
        Instantiate(editLoader);
    }
}

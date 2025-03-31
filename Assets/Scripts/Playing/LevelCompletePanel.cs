using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static PlayGM;

public class LevelCompletePanel : MonoBehaviour
{
    public PlayLoader levelLoader = null;
    public PlayModeContext playModeContext = PlayModeContext.FromMainMenuPlayButton;
    public GameObject playtestButtons;
    public GameObject standardButtons;
    private bool uploadComplete = false;

    private void Start()
    {
        playModeContext = PlayGM.instance.playModeContext;

        // Hide panel at start
        gameObject.SetActive(false);
    }

    public void Show()
    {
        switch (playModeContext)
        {
            case PlayModeContext.FromEditor:
                playtestButtons.SetActive(true);
                standardButtons.SetActive(false);
                break;
            case PlayModeContext.FromBrowser:
            case PlayModeContext.FromMainMenuPlayButton:
                playtestButtons.SetActive(false);
                standardButtons.SetActive(true);
                break;
        }

        gameObject.SetActive(true);
        StartCoroutine(EnableUIAfterFade());
    }

    private IEnumerator EnableUIAfterFade()
    {
        // Enable the buttons after the fade animation - set WaitForSeconds to match fade animation length
        yield return new WaitForSeconds(0.5f);
        var group = gameObject.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.interactable = true;
            group.blocksRaycasts = true;
        }
    }

    public void OnNegativeButton()
    {
        // this is used for all "negative buttons" in victory menu (main menu or edit)
        Debug.Log("OnNegativeButton");

        switch (playModeContext)
        {
            case PlayModeContext.FromEditor:
                var loaderGO = Instantiate(PlayGM.instance.editLoader);
                var loader = loaderGO.GetComponent<EditLoader>();
                loader.levelInfo = PlayGM.instance.levelInfo;
                loader.levelInfo.isLocal = true;
                SceneManager.LoadScene("Editing");
                break;
            case PlayModeContext.FromBrowser:
            case PlayModeContext.FromMainMenuPlayButton:
                SceneManager.LoadScene("MainMenu");
                break;
        }
    }

    void Update()
    {
        if (uploadComplete)
        {
            // load editing scene
            var loaderGO = Instantiate(PlayGM.instance.editLoader);
            var loader = loaderGO.GetComponent<EditLoader>();
            loader.levelInfo = PlayGM.instance.levelInfo;
            loader.levelInfo.isLocal = true;
            SceneManager.LoadScene("Editing");
        }
    }

    public void OnPositiveButton()
    {
        // this is used for all "postive buttons" in victory menu (replay or upload)
        Debug.Log("OnPositiveButton");

        switch (playModeContext)
        {
            case PlayModeContext.FromEditor:
                // Upload from edit mode
                Debug.Log(
                    "OnPositiveButton - Publishing to Supabase: " + PlayGM.instance.levelName
                );
                // disable buttons
                Button[] buttons = playtestButtons.GetComponentsInChildren<Button>(
                    includeInactive: true
                );
                foreach (Button btn in buttons)
                {
                    btn.interactable = false;
                }

                // publish to supabase
                PublishToSupabase();
                break;
            case PlayModeContext.FromBrowser:
            case PlayModeContext.FromMainMenuPlayButton:
                // start the play scene and set the levelName to default
                PlayLoader playLoaderGO = Instantiate(levelLoader);
                var playLoader = playLoaderGO.GetComponent<PlayLoader>();
                playLoader.levelInfo = PlayGM.instance.levelInfo;
                break;
        }
    }

    public void PublishToSupabase()
    {
        string[] lines = PlayGM.instance.levelData.Serialize();

        SupabaseLevelDTO levelDTO = new SupabaseLevelDTO
        {
            name = PlayGM.instance.levelName,
            data = lines,
        };
        SupabaseController.Instance.StartCoroutine(
            SupabaseController.Instance.SaveLevel(levelDTO, SaveLevelCallback)
        );
    }

    public void SaveLevelCallback(string s)
    {
        Debug.Log("[LevelCompletePanel] SaveLevelCallback");
        Debug.Log(s);
        uploadComplete = true;
    }
}

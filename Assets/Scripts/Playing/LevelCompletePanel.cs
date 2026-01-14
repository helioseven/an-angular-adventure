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
    public TMP_Text levelCompleteNameText;
    public GameObject playtestButtons;
    public GameObject standardButtons;
    [SerializeField]
    private Button uploadButton;
    [SerializeField]
    private Button nextLevelButton;
    [SerializeField]
    private TMP_Text nextLevelLabel;
    private bool uploadComplete = false;

    private void Start()
    {
        playModeContext = PlayGM.instance.playModeContext;

        // Hide panel at start
        gameObject.SetActive(false);
    }

    public void Show()
    {
        ConfigureDemoButtons();

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

        // set level name
        levelCompleteNameText.text = PlayGM.instance.levelName;

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
                LoadEditing();
                break;
            case PlayModeContext.FromBrowser:
            case PlayModeContext.FromMainMenuPlayButton:
                SceneManager.LoadScene("MainMenu");
                break;
        }
    }

    private void LoadEditing()
    {
        var loaderGO = Instantiate(PlayGM.instance.editLoader);
        var loader = loaderGO.GetComponent<EditLoader>();
        loader.levelInfo = PlayGM.instance.levelInfo;
        loader.levelInfo.isLocal = true;
        SceneManager.LoadScene("Editing");
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
                // In this case the user clicked "retry level"
                // start the play scene and set the level info based on current level / context
                PlayLoader playLoaderGO = Instantiate(levelLoader);
                var playLoader = playLoaderGO.GetComponent<PlayLoader>();
                playLoader.levelInfo = PlayGM.instance.levelInfo;
                playLoader.playModeContext = PlayGM.PlayModeContext.FromBrowser;
                break;
            case PlayModeContext.FromMainMenuPlayButton:
                // start the play scene without any extra info - it will load default level
                Instantiate(levelLoader);
                break;
        }
    }

    public void OnNextLevelButton()
    {
        if (!StartupManager.DemoModeEnabled)
        {
            PlayLoader retryLoaderGO = Instantiate(levelLoader);
            var retryLoader = retryLoaderGO.GetComponent<PlayLoader>();
            retryLoader.levelInfo = PlayGM.instance.levelInfo;
            retryLoader.playModeContext = PlayGM.instance.playModeContext;
            return;
        }

        LevelInfo next = LevelStorage.GetNextBundledLevelByBestTime();
        if (next == null)
            return;

        PlayLoader playLoaderGO = Instantiate(levelLoader);
        var playLoader = playLoaderGO.GetComponent<PlayLoader>();
        playLoader.levelInfo = next;
        playLoader.playModeContext = PlayGM.PlayModeContext.FromMainMenuPlayButton;
    }

    public void PublishToSupabase()
    {
        // get the data we need
        string[] lines = PlayGM.instance.levelData.Serialize();
        string levelName = PlayGM.instance.levelName;
        const int previewWidth = LevelPreviewConstants.PREVIEW_WIDTH;
        const int previewHeight = LevelPreviewConstants.PREVIEW_HEIGHT;

        // create the data transfer object to send up
        LevelPreviewDTO preview = CapturePreviewPng(Camera.main, previewWidth, previewHeight);
        SupabaseLevelDTO levelDTO = new SupabaseLevelDTO
        {
            name = levelName,
            data = lines,
            preview = preview,
        };

        // Upload the level to supabase
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

    private void ConfigureDemoButtons()
    {
        bool demo = StartupManager.DemoModeEnabled;
        uploadButton.gameObject.SetActive(!demo);
        nextLevelButton.gameObject.SetActive(true);
        nextLevelLabel.text = demo ? "Next Level" : "Retry Level";
        nextLevelButton.onClick.RemoveAllListeners();
        nextLevelButton.onClick.AddListener(OnNextLevelButton);
    }

    private LevelPreviewDTO CapturePreviewPng(Camera camera, int width, int height)
    {
        if (camera == null || width <= 0 || height <= 0)
            return null;

        RenderTexture rt = RenderTexture.GetTemporary(
            width,
            height,
            24,
            RenderTextureFormat.ARGB32
        );
        RenderTexture previous = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;

        camera.targetTexture = rt;
        RenderTexture.active = rt;
        camera.Render();

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();

        camera.targetTexture = previousTarget;
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        byte[] pngBytes = texture.EncodeToPNG();
        Destroy(texture);

        if (pngBytes == null || pngBytes.Length == 0)
            return null;

        return new LevelPreviewDTO
        {
            format = "png",
            width = width,
            height = height,
            encoding = "base64",
            data = Convert.ToBase64String(pngBytes),
        };
    }
}

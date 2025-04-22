using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class LevelBrowser : MonoBehaviour
{
    public GameObject levelListItemPrefab;
    public Transform levelListContent;
    public TMP_InputField filterInput;
    public GameObject playLoader;
    public GameObject editLoader;
    public SupabaseController supabase;
    public MenuGM menuGM;
    private List<LevelInfo> allLevels = new();

    void OnEnable()
    {
        allLevels = LevelStorage.LoadLocalLevelMetadata();

        /* ## Supabase ## - Fetch all published levels from Supabase */
        StartCoroutine(
            supabase.FetchPublishedLevels(onlineLevels =>
            {
                allLevels.AddRange(onlineLevels);
                RefreshUI();
            })
        );

        RefreshUI();
        filterInput.onValueChanged.AddListener(_ => RefreshUI());
    }

    void OnDisable()
    {
        filterInput.onValueChanged.RemoveAllListeners();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            menuGM.OpenMainMenu();
        }
    }

    void RefreshUI()
    {
        // Clear old items
        foreach (Transform child in levelListContent)
        {
            Destroy(child.gameObject);
        }

        string query = filterInput.text.ToLower();
        var filtered = allLevels.Where(level => level.name.ToLower().Contains(query)).ToList();

        foreach (var level in filtered)
        {
            var itemGO = Instantiate(levelListItemPrefab, levelListContent);
            var itemUI = itemGO.GetComponent<LevelListItemUI>();

            itemUI.Setup(
                level,
                onPlay: () =>
                {
                    Debug.Log($"Clicked Play: {level.name} (ID: {level.id})");

                    // Load the level to play
                    var loaderGO = Instantiate(playLoader);
                    var loader = loaderGO.GetComponent<PlayLoader>();
                    loader.levelInfo = level;
                    // let the play loader know it's coming from the browser
                    loader.playModeContext = PlayGM.PlayModeContext.FromBrowser;
                },
                onEditOrRemix: () =>
                {
                    // Load the level for editing
                    var loaderGO = Instantiate(editLoader);
                    var loader = loaderGO.GetComponent<EditLoader>();
                    loader.levelInfo = level;
                }
            );
        }
    }
}

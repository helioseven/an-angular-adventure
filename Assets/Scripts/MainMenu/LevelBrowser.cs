using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class LevelBrowser : MonoBehaviour
{
    public GameObject levelListItemPrefab;
    public Transform levelListContent;
    public InputField filterInput;
    public GameObject playLoader;
    public GameObject editLoader;
    public SupabaseEditController supabase;
    private List<LevelInfo> allLevels = new();

    void OnEnable()
    {
        allLevels = LevelStorage.LoadLocalLevelMetadata();

        // Fetch published levels from Supabase
        StartCoroutine(supabase.FetchPublishedLevels(onlineLevels =>
        {
            allLevels.AddRange(onlineLevels);
            RefreshUI();
        }));

        RefreshUI();
        filterInput.onValueChanged.AddListener(_ => RefreshUI());
    }

    void OnDisable()
    {
        filterInput.onValueChanged.RemoveAllListeners();
    }

    void RefreshUI()
    {
        // Clear old items
        foreach (Transform child in levelListContent)
        {
            Destroy(child.gameObject);
        }

        string query = filterInput.text.ToLower();
        var filtered = allLevels
            .Where(level => level.name.ToLower().Contains(query))
            .ToList();

        foreach (var level in filtered)
        {
            var itemGO = Instantiate(levelListItemPrefab, levelListContent);
            var itemUI = itemGO.GetComponent<LevelListItemUI>();

            itemUI.Setup(level, onPlay: () =>
            {
                Debug.Log($"Clicked Play: {level.name} (ID: {level.id})");

                // Load the level to play
                var loaderGO = Instantiate(playLoader);
                var loader = loaderGO.GetComponent<PlayLoader>();
                loader.levelName = level.name;
                loader.id = level.id;
                loader.loadFromSupabase = !level.isLocal;
            },
            onEditOrRemix: () =>
            {
                // Load the level for editing
                var loaderGO = Instantiate(editLoader);
                var loader = loaderGO.GetComponent<EditLoader>();
                loader.levelName = level.name;
                loader.id = level.id;
                loader.loadFromSupabase = !level.isLocal;
            });
        }
    }
}

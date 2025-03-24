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

    private List<LevelInfo> allLevels = new();

    void OnEnable()
    {
        allLevels = LevelStorage.LoadLocalLevelMetadata();
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

            itemUI.Setup(level, () =>
            {
                Debug.Log($"Clicked: {level.name} (ID: {level.id})");

                // Load the level 
                var loaderGO = Instantiate(playLoader);
                var loader = loaderGO.GetComponent<PlayLoader>();
                loader.levelName = level.name;
            });
        }
    }


    void LoadLevel(string id)
    {
        Debug.Log($"[LevelBrowser] Load level: {id}");
        // Add your actual load logic here
    }
}

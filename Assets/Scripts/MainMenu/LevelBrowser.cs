using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelBrowser : MonoBehaviour
{
    [Header("UI References")]
    public GameObject levelListItemPrefab;
    public Transform levelListContent;
    public TMP_InputField filterInput;
    public GameObject playLoader;
    public GameObject editLoader;
    public ConfirmModal confirmModal;

    [Header("Tabs")]
    public Button communityTabButton;
    public Button myTessellationsButton;

    [Header("Controllers")]
    public SupabaseController supabase;
    public MenuGM menuGM;

    private List<LevelInfo> allLevels = new();
    private bool showingMyLevels = false;

    void OnEnable()
    {
        filterInput.onValueChanged.AddListener(_ => RefreshUI());

        communityTabButton.onClick.AddListener(() => SwitchTab(false));
        myTessellationsButton.onClick.AddListener(() => SwitchTab(true));

        // Default tab = Community
        SwitchTab(false);

        // Wait a frame, then select to ensure EventSystem is active
        StartCoroutine(SelectCommunityTabNextFrame());
    }

    IEnumerator SelectCommunityTabNextFrame()
    {
        yield return null; // wait one frame
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(communityTabButton.gameObject);
    }

    void OnDisable()
    {
        filterInput.onValueChanged.RemoveAllListeners();
        communityTabButton.onClick.RemoveAllListeners();
        myTessellationsButton.onClick.RemoveAllListeners();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            menuGM.OpenMainMenu();
    }

    void SwitchTab(bool showMine)
    {
        // Clear current levels
        allLevels.Clear();
        RefreshUI();

        showingMyLevels = showMine;

        // Fetch new data
        if (showMine)
        {
            string steamId = AuthState.SteamId;
            StartCoroutine(
                supabase.FetchPublishedLevelsBySteamId(
                    steamId,
                    levels =>
                    {
                        allLevels = levels;
                        RefreshUI();
                    }
                )
            );
        }
        else
        {
            StartCoroutine(
                supabase.FetchPublishedLevels(levels =>
                {
                    allLevels = levels;
                    RefreshUI();
                })
            );
        }
    }

    public void ShowConfirmDelete(string levelId, string levelName)
    {
        confirmModal.Show(
            header: "Delete Tessellation?",
            body: $"Are you sure you want to delete “{levelName}”?",
            confirmAction: async () =>
            {
                SupabaseController.Instance.StartCoroutine(
                    SupabaseController.Instance.SoftDeleteLevelById(levelId, callback)
                );
            }
        );
    }

    // Supabase - callback function after deleting
    public void callback(bool success)
    {
        Debug.Log("Delete via modal successful: " + success);
        RefreshList();
    }

    void RefreshUI()
    {
        foreach (Transform child in levelListContent)
            Destroy(child.gameObject);

        string query = filterInput.text.ToLower();
        var filtered = allLevels.Where(l => l.name.ToLower().Contains(query)).ToList();

        foreach (var level in filtered)
        {
            var itemGO = Instantiate(levelListItemPrefab, levelListContent);
            var itemUI = itemGO.GetComponent<LevelListItemUI>();

            itemUI.Setup(
                level,
                onPlay: () =>
                {
                    var loaderGO = Instantiate(playLoader);
                    var loader = loaderGO.GetComponent<PlayLoader>();
                    loader.levelInfo = level;
                    loader.playModeContext = PlayGM.PlayModeContext.FromBrowser;
                },
                onEditOrRemix: () =>
                {
                    var loaderGO = Instantiate(editLoader);
                    var loader = loaderGO.GetComponent<EditLoader>();
                    loader.levelInfo = level;
                },
                browser: this
            );
        }
    }

    public void RefreshList()
    {
        SwitchTab(showingMyLevels);
    }
}

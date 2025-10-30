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

    [Header("Tabs")]
    public Button communityTabButton;
    public Button myTessellationsButton;
    public Color activeTabColor = new(0.8f, 0.8f, 1f);
    public Color inactiveTabColor = new(0.6f, 0.6f, 0.6f);

    [Header("Controllers")]
    public SupabaseController supabase;
    public MenuGM menuGM;

    private List<LevelInfo> allLevels = new();

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
                }
            );
        }
    }
}

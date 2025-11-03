using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum LevelBrowserTab
{
    Local = 0,
    DeveloperLevels,
    MyRemote,
    Community,
}

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
    public Button localTabButton;
    public Button developerTabButton;
    public Button communityTabButton;
    public Button myTessellationsButton;

    [Header("Controllers")]
    public SupabaseController supabase;
    public MenuGM menuGM;

    public LevelBrowserTab currTab = LevelBrowserTab.Local;

    private List<LevelInfo> allLevels = new();

    void OnEnable()
    {
        filterInput.onValueChanged.AddListener(_ => RefreshUI());

        localTabButton.onClick.AddListener(() => SwitchTab(LevelBrowserTab.Local));

        developerTabButton.onClick.AddListener(() => SwitchTab(LevelBrowserTab.DeveloperLevels));
        communityTabButton.onClick.AddListener(() => SwitchTab(LevelBrowserTab.Community));
        myTessellationsButton.onClick.AddListener(() => SwitchTab(LevelBrowserTab.MyRemote));

        // Default tab = DeveloperLevels
        SwitchTab(LevelBrowserTab.DeveloperLevels);

        // Wait a frame, then select to ensure EventSystem is active
        StartCoroutine(SelectStartingTabNextFrame());
    }

    IEnumerator SelectStartingTabNextFrame()
    {
        yield return null; // wait one frame
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(developerTabButton.gameObject);
    }

    void OnDisable()
    {
        filterInput.onValueChanged.RemoveAllListeners();
        localTabButton.onClick.RemoveAllListeners();
        developerTabButton.onClick.RemoveAllListeners();
        communityTabButton.onClick.RemoveAllListeners();
        myTessellationsButton.onClick.RemoveAllListeners();
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            menuGM.OpenMainMenu();
    }

    void SwitchTab(LevelBrowserTab tab)
    {
        currTab = tab;

        // Clear current levels
        allLevels.Clear();
        RefreshUI();

        // Fetch new data
        switch (tab)
        {
            case LevelBrowserTab.MyRemote:
            {
                string steamId = AuthState.Instance.SteamId;
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
                break;
            }
            case LevelBrowserTab.Community:
            {
                StartCoroutine(
                    supabase.FetchPublishedLevels(levels =>
                    {
                        allLevels = levels;
                        RefreshUI();
                    })
                );
                break;
            }
            case LevelBrowserTab.Local:
            {
                allLevels = LevelStorage.LoadLocalLevelMetadata();
                RefreshUI();
                break;
            }
            case LevelBrowserTab.DeveloperLevels:
            {
                string steamId = AuthState.Instance.SteamId;
                StartCoroutine(
                    supabase.FetchPublishedLevelsFromDevelopers(levels =>
                    {
                        allLevels = levels;
                        RefreshUI();
                    })
                );
                break;
            }
            default:
            {
                break;
            }
        }
    }

    public void ShowConfirmDelete(string levelId, string levelName)
    {
        confirmModal.Show(
            header: "Delete Tessellation?",
            body: $"Are you sure you want to delete “{levelName}”?",
            confirmAction: () =>
            {
                SupabaseController.Instance.StartCoroutine(
                    SupabaseController.Instance.SoftDeleteLevelById(levelId, DeleteCallback)
                );
            }
        );
    }

    // Supabase - DeleteCallback function after deleting
    public void DeleteCallback(bool success)
    {
        if (success)
        {
            Debug.Log("Delete via modal successful :)");
            RefreshList();
        }
        else
        {
            // TODO: popup failure snackbar (and success one for that matter!)
            Debug.LogError("[LevelBrowser] [DeleteCallback] f-f-f-failure");
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
                },
                browser: this
            );
        }
    }

    public void RefreshList()
    {
        SwitchTab(currTab);
    }
}

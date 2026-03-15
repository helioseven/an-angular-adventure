using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public enum LevelBrowserTab
{
    Official = 0,
    Community,
    MyLevels,
}

public enum MyTessellationsFilter
{
    Local = 0,
    Published,
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

    public Button backButton;

    [Header("Tabs")]
    public Toggle officialTabToggle;
    public Toggle communityTabToggle;
    public Toggle myLevelsTabToggle;

    [Header("My Tessellations")]
    public GameObject myTessellationsFilterRoot;
    public Toggle myTessellationsLocalToggle;
    public Toggle myTessellationsPublishedToggle;

    [Header("Controllers")]
    public SupabaseController supabase;
    public MenuGM menuGM;

    [Header("Preview Popup")]
    public LevelPreviewPopup previewPopup;

    [Header("Name Popup")]
    public LevelNamePopup namePopup;

    public LevelBrowserTab currTab = LevelBrowserTab.Official;

    private List<LevelInfo> allLevels = new();
    private bool hasLocalLevels;
    private MenuInputModeAdapter inputAdapter;
    private ScrollRect scrollRect;
    private Coroutine rebuildRoutine;
    private UnityAction<Vector2> scrollListener;
    private RectTransform lastScrollTarget;
    private LevelBrowserFilterInputController filterInputController;

    [Header("Controller Scroll")]
    public float thumbstickScrollSpeed = 2.2f;
    public float thumbstickDeadzone = 0.1f;
    public float thumbstickInputSmoothing = 20f;
    public float thumbstickScrollPixelsPerSecond = 1200f;
    private float thumbstickInput;
    private bool suppressNavigate;
    private bool suppressNavigateForFilterEditing;
    private InputSystemUIInputModule uiInputModule;
    private UnityAction<bool> officialTabListener;
    private UnityAction<bool> communityTabListener;
    private UnityAction<bool> myLevelsTabListener;
    private UnityAction<bool> myTessellationsLocalListener;
    private UnityAction<bool> myTessellationsPublishedListener;

    public MyTessellationsFilter currMyTessellationsFilter = MyTessellationsFilter.Local;

    void OnEnable()
    {
        MenuFocusUtility.ApplyHighlightedAsSelected(gameObject);
        InitializePreviewPopup();
        EnsureNamePopup();

        if (!StartupManager.DemoModeEnabled)
        {
            if (supabase == null)
                supabase = SupabaseController.Instance;
            if (supabase == null)
            {
                Debug.LogError("[LevelBrowser] SupabaseController reference is missing.");
                return;
            }
        }

        if (filterInput != null)
        {
            filterInput.onValueChanged.AddListener(_ => RefreshUI());
            filterInputController = filterInput.GetComponent<LevelBrowserFilterInputController>();
            if (filterInputController == null)
            {
                filterInputController =
                    filterInput.gameObject.AddComponent<LevelBrowserFilterInputController>();
            }
            filterInputController.Configure(this);
        }

        if (officialTabToggle != null)
        {
            officialTabListener ??= isOn =>
            {
                if (isOn)
                    SwitchTab(LevelBrowserTab.Official);
            };
            officialTabToggle.onValueChanged.AddListener(officialTabListener);
        }
        if (communityTabToggle != null)
        {
            communityTabListener ??= isOn =>
            {
                if (isOn)
                    SwitchTab(LevelBrowserTab.Community);
            };
            communityTabToggle.onValueChanged.AddListener(communityTabListener);
        }
        if (myLevelsTabToggle != null)
        {
            myLevelsTabListener ??= isOn =>
            {
                if (isOn)
                    SwitchTab(LevelBrowserTab.MyLevels);
            };
            myLevelsTabToggle.onValueChanged.AddListener(myLevelsTabListener);
        }
        if (myTessellationsLocalToggle != null)
        {
            myTessellationsLocalListener ??= isOn =>
            {
                if (!isOn)
                    return;

                currMyTessellationsFilter = MyTessellationsFilter.Local;
                if (currTab == LevelBrowserTab.MyLevels)
                    LoadMyLevels();
            };
            myTessellationsLocalToggle.onValueChanged.AddListener(myTessellationsLocalListener);
        }
        if (myTessellationsPublishedToggle != null)
        {
            myTessellationsPublishedListener ??= isOn =>
            {
                if (!isOn)
                    return;

                currMyTessellationsFilter = MyTessellationsFilter.Published;
                if (currTab == LevelBrowserTab.MyLevels)
                    LoadMyLevels();
            };
            myTessellationsPublishedToggle.onValueChanged.AddListener(
                myTessellationsPublishedListener
            );
        }

        if (backButton != null)
            backButton.onClick.AddListener(() =>
            {
                menuGM.OpenMainMenu();
            });

        scrollRect =
            levelListContent != null
                ? levelListContent.GetComponentInParent<ScrollRect>(true)
                : GetComponentInParent<ScrollRect>(true);
        if (scrollRect != null)
        {
            if (scrollListener == null)
            {
                scrollListener = _ => ScheduleProxyRebuild();
            }

            scrollRect.onValueChanged.AddListener(scrollListener);
        }

        hasLocalLevels = LevelStorage.HasLocalLevels();
        InitializeMyTessellationsFilterState();

        if (StartupManager.DemoModeEnabled)
        {
            if (filterInput != null)
                filterInput.gameObject.SetActive(false);
            if (communityTabToggle != null)
                communityTabToggle.gameObject.SetActive(false);
            if (myLevelsTabToggle != null)
                myLevelsTabToggle.gameObject.SetActive(hasLocalLevels);
        }

        UpdateMyTessellationsFilterVisibility();
        RefreshMyTessellationsFilterOptions();
        ConfigureTopNavigation();

        // Default tab = Official for the main browse experience
        var defaultTab = LevelBrowserTab.Official;
        SwitchTab(defaultTab);

        InputModeTracker.EnsureInstance();
        var jiggle = GetComponent<SelectedJiggle>();
        if (jiggle == null)
            jiggle = gameObject.AddComponent<SelectedJiggle>();
        jiggle.SetScope(transform);
        jiggle.SetAmplitude(3f);

        inputAdapter = GetComponent<MenuInputModeAdapter>();
        if (inputAdapter == null)
            inputAdapter = gameObject.AddComponent<MenuInputModeAdapter>();
        inputAdapter.SetScope(transform);
        inputAdapter.SetPreferred(null);

        // Wait a frame, then select to ensure EventSystem is active
        StartCoroutine(SelectStartingTabNextFrame());
    }

    IEnumerator SelectStartingTabNextFrame()
    {
        yield return null; // wait one frame
        if (
            EventSystem.current != null
            && InputModeTracker.Instance != null
            && InputModeTracker.Instance.CurrentMode == InputMode.Navigation
        )
        {
            var target = GetDefaultTabButton();
            if (target != null && target != backButton)
                EventSystem.current.SetSelectedGameObject(target.gameObject);
        }
    }

    private Selectable GetDefaultTabButton()
    {
        if (officialTabToggle != null && officialTabToggle.gameObject.activeInHierarchy)
            return officialTabToggle;
        if (communityTabToggle != null && communityTabToggle.gameObject.activeInHierarchy)
            return communityTabToggle;
        if (myLevelsTabToggle != null && myLevelsTabToggle.gameObject.activeInHierarchy)
            return myLevelsTabToggle;

        return backButton;
    }

    void OnDisable()
    {
        if (filterInput != null)
            filterInput.onValueChanged.RemoveAllListeners();
        if (previewPopup != null)
            previewPopup.Hide();
        if (namePopup != null)
            namePopup.Hide();
        if (officialTabToggle != null && officialTabListener != null)
            officialTabToggle.onValueChanged.RemoveListener(officialTabListener);
        if (communityTabToggle != null && communityTabListener != null)
            communityTabToggle.onValueChanged.RemoveListener(communityTabListener);
        if (myLevelsTabToggle != null && myLevelsTabListener != null)
            myLevelsTabToggle.onValueChanged.RemoveListener(myLevelsTabListener);
        if (myTessellationsLocalToggle != null && myTessellationsLocalListener != null)
            myTessellationsLocalToggle.onValueChanged.RemoveListener(myTessellationsLocalListener);
        if (
            myTessellationsPublishedToggle != null
            && myTessellationsPublishedListener != null
        )
        {
            myTessellationsPublishedToggle.onValueChanged.RemoveListener(
                myTessellationsPublishedListener
            );
        }

        if (scrollRect != null && scrollListener != null)
        {
            scrollRect.onValueChanged.RemoveListener(scrollListener);
        }
    }

    void Update()
    {
        if (
            Keyboard.current.escapeKey.wasPressedThisFrame
            || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
        )
            menuGM.OpenMainMenu();

        AutoScrollToSelection();

        if (scrollRect != null && Gamepad.current != null)
        {
            float rawInput = Gamepad.current.rightStick.ReadUnprocessedValue().y;
            float target = Mathf.Abs(rawInput) < thumbstickDeadzone ? 0f : rawInput;
            float t = 1f - Mathf.Exp(-thumbstickInputSmoothing * Time.unscaledDeltaTime);
            thumbstickInput = Mathf.Lerp(thumbstickInput, target, t);

            if (Mathf.Abs(thumbstickInput) < 0.001f)
            {
                if (suppressNavigate)
                {
                    suppressNavigate = false;
                    InputManager.Instance?.Controls.UI.Navigate.Enable();
                    SetUINavigateEnabled(true);
                }
                return;
            }

            if (!suppressNavigate)
            {
                suppressNavigate = true;
                InputManager.Instance?.Controls.UI.Navigate.Disable();
                SetUINavigateEnabled(false);
            }

            ApplyThumbstickScroll(scrollRect, thumbstickInput, thumbstickScrollPixelsPerSecond);
        }
    }

    void SwitchTab(LevelBrowserTab tab)
    {
        bool usesSupabase = tab == LevelBrowserTab.Community;
        if (supabase == null && usesSupabase)
        {
            Debug.LogError("[LevelBrowser] Cannot switch tab: SupabaseController is missing.");
            return;
        }

        currTab = tab;
        SetActiveTabWithoutNotify(tab);
        RefreshTabVisuals();
        UpdateMyTessellationsFilterVisibility();
        RefreshMyTessellationsFilterOptions();
        ConfigureTopNavigation();

        // Clear current levels
        allLevels.Clear();
        RefreshUI();

        // Fetch new data
        switch (tab)
        {
            case LevelBrowserTab.Community:
            {
                LoadCommunityLevels();
                break;
            }
            case LevelBrowserTab.Official:
            {
                allLevels = LevelStorage.LoadBundledLevelMetadata();
                RefreshUI();
                break;
            }
            case LevelBrowserTab.MyLevels:
            {
                LoadMyLevels();
                break;
            }
            default:
            {
                break;
            }
        }
    }

    private void SetActiveTabWithoutNotify(LevelBrowserTab tab)
    {
        SetToggleValueWithoutNotify(officialTabToggle, tab == LevelBrowserTab.Official);
        SetToggleValueWithoutNotify(communityTabToggle, tab == LevelBrowserTab.Community);
        SetToggleValueWithoutNotify(myLevelsTabToggle, tab == LevelBrowserTab.MyLevels);
    }

    private static void SetToggleValueWithoutNotify(Toggle toggle, bool value)
    {
        if (toggle == null)
            return;

        toggle.SetIsOnWithoutNotify(value);
    }

    private void RefreshTabVisuals()
    {
        ApplyTabVisualState(officialTabToggle);
        ApplyTabVisualState(communityTabToggle);
        ApplyTabVisualState(myLevelsTabToggle);
    }

    private static void ApplyTabVisualState(Toggle toggle)
    {
        if (toggle == null || toggle.targetGraphic == null)
            return;

        ColorBlock colors = toggle.colors;
        toggle.targetGraphic.color = toggle.isOn ? colors.selectedColor : colors.normalColor;
    }

    private void InitializeMyTessellationsFilterState()
    {
        if (!hasLocalLevels)
            currMyTessellationsFilter = MyTessellationsFilter.Published;
    }

    private void UpdateMyTessellationsFilterVisibility()
    {
        if (myTessellationsFilterRoot == null)
            return;

        myTessellationsFilterRoot.SetActive(currTab == LevelBrowserTab.MyLevels);
    }

    private void RefreshMyTessellationsFilterOptions()
    {
        if (myTessellationsLocalToggle != null)
            myTessellationsLocalToggle.gameObject.SetActive(hasLocalLevels);

        if (myTessellationsPublishedToggle != null)
            myTessellationsPublishedToggle.gameObject.SetActive(!StartupManager.DemoModeEnabled);

        if (currMyTessellationsFilter == MyTessellationsFilter.Local && !hasLocalLevels)
            currMyTessellationsFilter = MyTessellationsFilter.Published;

        if (
            currMyTessellationsFilter == MyTessellationsFilter.Published
            && StartupManager.DemoModeEnabled
        )
        {
            currMyTessellationsFilter = MyTessellationsFilter.Local;
        }

        if (myTessellationsLocalToggle != null)
        {
            myTessellationsLocalToggle.SetIsOnWithoutNotify(
                currMyTessellationsFilter == MyTessellationsFilter.Local
            );
        }

        if (myTessellationsPublishedToggle != null)
        {
            myTessellationsPublishedToggle.SetIsOnWithoutNotify(
                currMyTessellationsFilter == MyTessellationsFilter.Published
            );
        }
    }

    private void LoadCommunityLevels()
    {
        if (StartupManager.DemoModeEnabled)
        {
            allLevels = new List<LevelInfo>();
            RefreshUI();
            return;
        }

        var merged = new List<LevelInfo>();
        int pending = 2;

        void Complete()
        {
            pending--;
            if (pending > 0)
                return;

            allLevels = DeduplicateLevels(merged);
            RefreshUI();
        }

        StartCoroutine(
            supabase.FetchPublishedLevelsFromDevelopers(levels =>
            {
                if (levels != null)
                    merged.AddRange(levels);
                Complete();
            })
        );

        StartCoroutine(
            supabase.FetchPublishedLevels(levels =>
            {
                if (levels != null)
                    merged.AddRange(levels);
                Complete();
            })
        );
    }

    private void LoadMyLevels()
    {
        switch (currMyTessellationsFilter)
        {
            case MyTessellationsFilter.Local:
                LoadMyLocalLevels();
                break;
            case MyTessellationsFilter.Published:
                LoadMyPublishedLevels();
                break;
            default:
                LoadMyLocalLevels();
                break;
        }
    }

    private void LoadMyLocalLevels()
    {
        if (!hasLocalLevels)
        {
            allLevels = new List<LevelInfo>();
            RefreshUI();
            return;
        }

        Debug.Log($"[LevelBrowser] Local Tessellations path: {LevelStorage.TessellationsFolder}");
        allLevels = LevelStorage.LoadLocalLevelMetadata();
        RefreshUI();
    }

    private void LoadMyPublishedLevels()
    {
        if (StartupManager.DemoModeEnabled)
        {
            allLevels = new List<LevelInfo>();
            RefreshUI();
            return;
        }

        if (supabase == null || AuthState.Instance == null)
        {
            allLevels = new List<LevelInfo>();
            RefreshUI();
            return;
        }

        string steamId = AuthState.Instance.SteamId;
        if (string.IsNullOrEmpty(steamId))
        {
            allLevels = new List<LevelInfo>();
            RefreshUI();
            return;
        }

        StartCoroutine(
            supabase.FetchPublishedLevelsBySteamId(
                steamId,
                levels =>
                {
                    allLevels = levels ?? new List<LevelInfo>();
                    RefreshUI();
                }
            )
        );
    }

    private static List<LevelInfo> DeduplicateLevels(List<LevelInfo> levels)
    {
        if (levels == null || levels.Count == 0)
            return new List<LevelInfo>();

        var deduped = new List<LevelInfo>(levels.Count);
        var seenKeys = new HashSet<string>();

        foreach (var level in levels)
        {
            if (level == null)
                continue;

            string key = !string.IsNullOrEmpty(level.id)
                ? level.id
                : $"{level.name}:{level.uploaderId}:{level.isLocal}:{level.isBundled}";

            if (!seenKeys.Add(key))
                continue;

            deduped.Add(level);
        }

        return deduped;
    }

    public void ShowConfirmDelete(string levelIdOrName, string levelName, bool isLocal = false)
    {
        string header = "Delete Tessellation?";
        string body = isLocal
            ? $"Are you sure you want to permanently delete your local draft \"{levelName}\"? This cannot be undone."
            : $"Are you sure you want to delete \"{levelName}\"?";

        confirmModal.Show(
            header: header,
            body: body,
            confirmAction: () =>
            {
                if (isLocal)
                {
                    bool deleted = LevelStorage.DeleteLocalLevel(levelIdOrName);
                    Debug.Log($"[LevelBrowser] Local level delete result: {deleted}");
                    RefreshList();
                }
                else
                {
                    SupabaseController.Instance.StartCoroutine(
                        SupabaseController.Instance.SoftDeleteLevelById(
                            levelIdOrName,
                            DeleteCallback
                        )
                    );
                }
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

        LevelListItemUI firstItem = null;
        foreach (var level in filtered)
        {
            var itemGO = Instantiate(levelListItemPrefab, levelListContent);
            var itemUI = itemGO.GetComponent<LevelListItemUI>();

            itemUI.Setup(
                level,
                onPlay: () =>
                {
                    Debug.Log($"[LevelBrowser] Play clicked: {level.name} ({level.id})");
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

            if (firstItem == null)
                firstItem = itemUI;
        }

        SelectFirstListItemIfNavigating(firstItem);
        ScheduleProxyRebuild();
    }

    private void ScheduleProxyRebuild()
    {
        if (rebuildRoutine != null)
        {
            StopCoroutine(rebuildRoutine);
        }

        rebuildRoutine = StartCoroutine(RebuildBrowseProxiesNextFrame());
    }

    private IEnumerator RebuildBrowseProxiesNextFrame()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();
        RectTransform listRect = levelListContent as RectTransform;
        if (listRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(listRect);
        }

        if (menuGM != null)
        {
            menuGM.RebuildBrowsePhysicsProxies();
        }
    }

    public void RefreshList()
    {
        SwitchTab(currTab);
    }

    private void SelectFirstListItemIfNavigating(LevelListItemUI firstItem)
    {
        if (firstItem == null || InputModeTracker.Instance == null)
            return;
        if (InputModeTracker.Instance.CurrentMode != InputMode.Navigation)
            return;
        if (ShouldPreserveFilterFocus())
            return;

        var button = GetPrimaryAction(firstItem);

        if (button != null)
        {
            if (inputAdapter != null)
                inputAdapter.SetPreferred(button);
            StartCoroutine(SelectListButtonNextFrame(button));
        }
    }

    private IEnumerator SelectListButtonNextFrame(Button button)
    {
        yield return null;
        if (button == null || InputModeTracker.Instance == null)
            yield break;
        if (InputModeTracker.Instance.CurrentMode != InputMode.Navigation)
            yield break;

        EventSystem.current?.SetSelectedGameObject(null);
        EventSystem.current?.SetSelectedGameObject(button.gameObject);
    }

    private bool ShouldPreserveFilterFocus()
    {
        if (filterInput == null)
            return false;
        if (filterInput.isFocused)
            return true;

        return EventSystem.current != null
            && EventSystem.current.currentSelectedGameObject == filterInput.gameObject;
    }

    private Selectable GetSelectedOrFirstVisibleTab()
    {
        Selectable selectedTab = currTab switch
        {
            LevelBrowserTab.Official => officialTabToggle,
            LevelBrowserTab.Community => communityTabToggle,
            LevelBrowserTab.MyLevels => myLevelsTabToggle,
            _ => null,
        };

        if (IsSelectableActive(selectedTab))
            return selectedTab;

        return GetVisibleTabs().FirstOrDefault();
    }

    public Selectable GetActiveTabSelectable()
    {
        return GetSelectedOrFirstVisibleTab();
    }

    public void SetFilterEditingNavigationSuppressed(bool suppressed)
    {
        suppressNavigateForFilterEditing = suppressed;

        bool shouldEnableNavigate = !suppressNavigate && !suppressNavigateForFilterEditing;
        if (shouldEnableNavigate)
            InputManager.Instance?.Controls.UI.Navigate.Enable();
        else
            InputManager.Instance?.Controls.UI.Navigate.Disable();

        SetUINavigateEnabled(shouldEnableNavigate);
    }

    private List<Selectable> GetVisibleTabs()
    {
        var visibleTabs = new List<Selectable>(3);
        if (IsSelectableActive(officialTabToggle))
            visibleTabs.Add(officialTabToggle);
        if (IsSelectableActive(communityTabToggle))
            visibleTabs.Add(communityTabToggle);
        if (IsSelectableActive(myLevelsTabToggle))
            visibleTabs.Add(myLevelsTabToggle);
        return visibleTabs;
    }

    private static bool IsSelectableActive(Selectable selectable)
    {
        return selectable != null
            && selectable.gameObject.activeInHierarchy
            && selectable.IsInteractable();
    }

    private void ConfigureTopNavigation()
    {
        var visibleTabs = GetVisibleTabs();
        Selectable selectedTab = GetSelectedOrFirstVisibleTab();
        Selectable filterSelectable =
            filterInput != null
            && filterInput.gameObject.activeInHierarchy
            && filterInput.IsInteractable()
                ? filterInput
                : null;
        Selectable backSelectable = IsSelectableActive(backButton) ? backButton : null;
        Selectable localSelectable = IsSelectableActive(myTessellationsLocalToggle)
            ? myTessellationsLocalToggle
            : null;
        Selectable publishedSelectable = IsSelectableActive(myTessellationsPublishedToggle)
            ? myTessellationsPublishedToggle
            : null;
        Selectable myLevelsSubToggle = localSelectable ?? publishedSelectable;

        for (int i = 0; i < visibleTabs.Count; i++)
        {
            Selectable current = visibleTabs[i];
            Navigation navigation = current.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnLeft = i > 0 ? visibleTabs[i - 1] : null;
            navigation.selectOnRight = i < visibleTabs.Count - 1 ? visibleTabs[i + 1] : null;
            navigation.selectOnUp =
                current == myLevelsTabToggle && myLevelsSubToggle != null
                    ? myLevelsSubToggle
                    : backSelectable;
            navigation.selectOnDown = null;
            current.navigation = navigation;
        }

        if (backButton != null)
        {
            Navigation navigation = backButton.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnLeft = null;
            navigation.selectOnRight = filterSelectable;
            navigation.selectOnUp = null;
            navigation.selectOnDown = selectedTab;
            backButton.navigation = navigation;
        }

        if (filterInput != null)
        {
            Navigation navigation = filterInput.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnLeft = backSelectable;
            navigation.selectOnRight = localSelectable;
            navigation.selectOnUp = null;
            navigation.selectOnDown = selectedTab;
            filterInput.navigation = navigation;
        }

        if (myTessellationsLocalToggle != null)
        {
            Navigation navigation = myTessellationsLocalToggle.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnLeft = filterSelectable;
            navigation.selectOnRight = publishedSelectable;
            navigation.selectOnUp = null;
            navigation.selectOnDown = myLevelsTabToggle;
            myTessellationsLocalToggle.navigation = navigation;
        }

        if (myTessellationsPublishedToggle != null)
        {
            Navigation navigation = myTessellationsPublishedToggle.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnLeft = localSelectable;
            navigation.selectOnRight = null;
            navigation.selectOnUp = null;
            navigation.selectOnDown = myLevelsTabToggle;
            myTessellationsPublishedToggle.navigation = navigation;
        }
    }

    private static Button GetPrimaryAction(LevelListItemUI item)
    {
        if (item == null)
            return null;
        if (item.playButton != null && item.playButton.gameObject.activeInHierarchy)
            return item.playButton;
        if (item.editOrRemixButton != null && item.editOrRemixButton.gameObject.activeInHierarchy)
            return item.editOrRemixButton;
        if (item.deleteButton != null && item.deleteButton.gameObject.activeInHierarchy)
            return item.deleteButton;
        return null;
    }

    private void AutoScrollToSelection()
    {
        if (scrollRect == null || scrollRect.content == null)
            return;
        if (InputModeTracker.Instance != null)
        {
            // Allow keyboard navigation to drive auto-scroll even if the pointer was active recently.
            if (
                InputModeTracker.Instance.CurrentMode != InputMode.Navigation
                && !IsKeyboardNavigationPressed()
            )
                return;
        }
        if (EventSystem.current == null)
            return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null)
        {
            return;
        }

        RectTransform target = ResolveScrollTarget(selected);
        if (target == null)
        {
            lastScrollTarget = null;
            return;
        }

        if (target == lastScrollTarget && IsSelectionFullyVisible(target))
        {
            return;
        }

        if (IsRightStickScrolling() && !IsSelectionFullyVisible(target))
        {
            NudgeSelectionForScroll(selected, thumbstickInput);
            return;
        }

        lastScrollTarget = target;
        ScrollSelectionIntoView(target);
    }

    private RectTransform ResolveScrollTarget(GameObject selected)
    {
        if (selected == null || levelListContent == null)
            return null;

        Transform selectedTransform = selected.transform;
        if (!selectedTransform.IsChildOf(levelListContent))
            return null;

        var listItem = selectedTransform.GetComponentInParent<LevelListItemUI>();
        if (listItem != null && listItem.transform.IsChildOf(levelListContent))
        {
            return listItem.GetComponent<RectTransform>();
        }

        return selectedTransform.GetComponent<RectTransform>();
    }

    private bool IsSelectionFullyVisible(RectTransform target)
    {
        RectTransform viewport =
            scrollRect.viewport != null
                ? scrollRect.viewport
                : scrollRect.GetComponent<RectTransform>();
        if (viewport == null || target == null || scrollRect.content == null)
            return true;

        Canvas.ForceUpdateCanvases();

        Bounds targetInViewport = GetBoundsInLocalSpace(viewport, target);
        Rect viewRect = viewport.rect;
        const float padding = 16f;
        float minY = viewRect.yMin + padding;
        float maxY = viewRect.yMax - padding;
        return targetInViewport.min.y >= minY && targetInViewport.max.y <= maxY;
    }

    private void ScrollSelectionIntoView(RectTransform target)
    {
        RectTransform viewport =
            scrollRect.viewport != null
                ? scrollRect.viewport
                : scrollRect.GetComponent<RectTransform>();
        if (viewport == null || target == null)
            return;

        Canvas.ForceUpdateCanvases();

        Bounds viewBounds = GetBoundsInLocalSpace(scrollRect.content, viewport);
        Bounds targetBounds = GetBoundsInLocalSpace(scrollRect.content, target);
        float padding = GetPaddingInContentSpace(viewport, scrollRect.content, 16f);
        float viewMin = viewBounds.min.y + padding;
        float viewMax = viewBounds.max.y - padding;

        float delta = 0f;
        if (targetBounds.max.y > viewMax)
            delta = targetBounds.max.y - viewMax;
        else if (targetBounds.min.y < viewMin)
            delta = targetBounds.min.y - viewMin;

        if (Mathf.Abs(delta) <= 0.001f)
            return;

        scrollRect.StopMovement();
        scrollRect.velocity = Vector2.zero;

        // Convert delta in content space to normalized scroll change for reliability
        float contentHeight = scrollRect.content.rect.height;
        float viewHeight = viewport.rect.height;
        float scrollable = Mathf.Max(0.0001f, contentHeight - viewHeight);

        float normalizedDelta = delta / scrollable;
        float nextNorm = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + normalizedDelta);
        scrollRect.verticalNormalizedPosition = nextNorm;
    }

    private static bool IsKeyboardNavigationPressed()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        return keyboard.upArrowKey.isPressed
            || keyboard.downArrowKey.isPressed
            || keyboard.leftArrowKey.isPressed
            || keyboard.rightArrowKey.isPressed
            || keyboard.wKey.isPressed
            || keyboard.aKey.isPressed
            || keyboard.sKey.isPressed
            || keyboard.dKey.isPressed;
    }

    private bool IsRightStickScrolling()
    {
        if (Gamepad.current == null)
            return false;

        if (!suppressNavigate)
            return false;

        float raw = Gamepad.current.rightStick.ReadUnprocessedValue().y;
        return Mathf.Abs(raw) > thumbstickDeadzone;
    }

    private void NudgeSelectionForScroll(GameObject selected, float input)
    {
        if (selected == null)
            return;

        var selectable = selected.GetComponent<Selectable>();
        if (selectable == null)
            return;

        Selectable next =
            input > 0f ? selectable.FindSelectableOnUp() : selectable.FindSelectableOnDown();
        if (next == null || !next.gameObject.activeInHierarchy || !next.IsInteractable())
            return;

        if (inputAdapter != null)
            inputAdapter.SetPreferred(next);

        EventSystem.current?.SetSelectedGameObject(next.gameObject);
    }

    private static Bounds GetBoundsInLocalSpace(RectTransform root, RectTransform target)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, 0f);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, 0f);

        for (int i = 0; i < 4; i++)
        {
            Vector3 local = root.InverseTransformPoint(corners[i]);
            min = Vector3.Min(min, local);
            max = Vector3.Max(max, local);
        }

        Bounds bounds = new Bounds();
        bounds.SetMinMax(min, max);
        return bounds;
    }

    private static float GetPaddingInContentSpace(
        RectTransform viewport,
        RectTransform content,
        float padding
    )
    {
        if (viewport == null || content == null)
            return padding;

        Vector3 delta = viewport.TransformVector(new Vector3(0f, padding, 0f));
        Vector3 local = content.InverseTransformVector(delta);
        return Mathf.Abs(local.y);
    }

    private static void ApplyThumbstickScroll(ScrollRect target, float input, float pixelsPerSecond)
    {
        if (target == null || target.content == null)
            return;

        target.StopMovement();
        target.velocity = Vector2.zero;

        RectTransform viewport =
            target.viewport != null ? target.viewport : target.GetComponent<RectTransform>();
        if (viewport == null)
            return;

        float contentHeight = target.content.rect.height;
        float viewHeight = viewport.rect.height;
        float maxScroll = Mathf.Max(0f, contentHeight - viewHeight);
        if (maxScroll <= 0.001f)
            return;

        Vector2 anchored = target.content.anchoredPosition;
        float delta = -input * pixelsPerSecond * Time.unscaledDeltaTime;
        anchored.y = Mathf.Clamp(anchored.y + delta, 0f, maxScroll);
        target.content.anchoredPosition = anchored;
    }

    private void SetUINavigateEnabled(bool enabled)
    {
        if (uiInputModule == null)
        {
            var current = EventSystem.current;
            if (current != null)
                uiInputModule = current.GetComponent<InputSystemUIInputModule>();
        }

        var move = uiInputModule != null ? uiInputModule.move : null;
        if (move == null || move.action == null)
            return;

        if (enabled)
            move.action.Enable();
        else
            move.action.Disable();
    }

    public void ShowPreview(Sprite sprite, Vector2 screenPos)
    {
        if (sprite == null)
            return;

        InitializePreviewPopup();
        previewPopup?.Show(sprite, screenPos);
    }

    public void MovePreview(Vector2 screenPos)
    {
        previewPopup?.MoveTo(screenPos);
    }

    public void HidePreview()
    {
        previewPopup?.Hide();
    }

    public void ShowNamePopup(string text, TMP_Text source, Vector2 screenPos)
    {
        if (string.IsNullOrEmpty(text))
            return;

        EnsureNamePopup();
        namePopup?.Show(text, source, screenPos);
    }

    public void MoveNamePopup(Vector2 screenPos)
    {
        namePopup?.MoveTo(screenPos);
    }

    public void HideNamePopup()
    {
        namePopup?.Hide();
    }

    private void InitializePreviewPopup()
    {
        if (previewPopup != null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[LevelBrowser] Cannot create preview popup: Canvas missing.");
            return;
        }

        GameObject popupGO = new GameObject(
            "LevelPreviewPopup",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(Image),
            typeof(LevelPreviewPopup)
        );
        popupGO.transform.SetParent(canvas.transform, false);

        CanvasGroup group = popupGO.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        group.ignoreParentGroups = true;

        Image background = popupGO.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.85f);

        GameObject imageGO = new GameObject("PreviewImage", typeof(RectTransform), typeof(Image));
        imageGO.transform.SetParent(popupGO.transform, false);
        Image image = imageGO.GetComponent<Image>();
        image.preserveAspect = true;

        LevelPreviewPopup popup = popupGO.GetComponent<LevelPreviewPopup>();
        popup.Initialize(canvas, image);
        previewPopup = popup;
    }

    private void EnsureNamePopup()
    {
        if (namePopup != null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[LevelBrowser] Cannot create name popup: Canvas missing.");
            return;
        }

        GameObject popupGO = new GameObject(
            "LevelNamePopup",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(Image),
            typeof(LevelNamePopup)
        );
        popupGO.transform.SetParent(canvas.transform, false);

        CanvasGroup group = popupGO.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        group.ignoreParentGroups = true;

        Image background = popupGO.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.85f);

        GameObject textGO = new GameObject(
            "NameText",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        textGO.transform.SetParent(popupGO.transform, false);
        TMP_Text text = textGO.GetComponent<TMP_Text>();
        text.enableAutoSizing = false;
        text.raycastTarget = false;

        LevelNamePopup popup = popupGO.GetComponent<LevelNamePopup>();
        popup.Initialize(canvas, text);
        namePopup = popup;
    }
}

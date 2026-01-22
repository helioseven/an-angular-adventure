using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PausePanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private GameObject mainButtonContainer;

    [SerializeField]
    private GameObject settingsContainer;

    [SerializeField]
    private Button settingsBackButton;

    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private TMP_Text headerText;

    [SerializeField]
    private string pausedTitle = "Paused";

    [SerializeField]
    private float fadeDelaySeconds = 0f;

    [SerializeField]
    private PauseMenu pauseMenu;

    [SerializeField]
    private PlayLoader playLoaderPrefab;
    private GameObject _activeContainer;
    private Animator _animator;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        _animator = GetComponent<Animator>();
        if (_animator != null)
        {
            _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            _animator.speed = 6f;
        }
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (pauseMenu != null && pauseMenu.IsSettingsOpen)
            {
                ShowMainMenu();
            }
            else
                pauseMenu.Resume();
            return;
        }

        if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
        {
            pauseMenu.Resume();
            return;
        }

        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            if (pauseMenu != null && pauseMenu.IsSettingsOpen)
            {
                ShowMainMenu();
            }
            else
                pauseMenu.Resume();
        }
    }

    public void Show()
    {
        if (headerText != null)
            headerText.text = pausedTitle;
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        gameObject.SetActive(true);
        ShowMainMenu();
        StartCoroutine(EnableUIAfterFade());
    }

    public void Hide()
    {
        StopAllCoroutines();
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
    }

    public void ShowMainMenu()
    {
        if (mainButtonContainer != null)
            mainButtonContainer.SetActive(true);
        if (settingsContainer != null)
            settingsContainer.SetActive(false);

        _activeContainer = mainButtonContainer != null ? mainButtonContainer : gameObject;
        ApplyButtonNavigation(_activeContainer);
        pauseMenu.SetSettingsOpen(false);
        if (settingsBackButton != null)
            settingsBackButton.onClick.RemoveAllListeners();
        RefreshNavigationFocus();
    }

    public void ShowSettings()
    {
        if (mainButtonContainer != null)
            mainButtonContainer.SetActive(false);
        if (settingsContainer != null)
            settingsContainer.SetActive(true);

        _activeContainer = settingsContainer != null ? settingsContainer : gameObject;
        ApplyButtonNavigation(_activeContainer);
        pauseMenu.SetSettingsOpen(true);
        var settingsMenu =
            _activeContainer != null
                ? _activeContainer.GetComponentInChildren<SettingsMenu>(true)
                : null;
        settingsMenu.RefreshNavigation();
        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.RemoveAllListeners();
            settingsBackButton.onClick.AddListener(ShowMainMenu);
        }
        RefreshNavigationFocus();
    }

    public void NotifySettingsOpen(bool open)
    {
        pauseMenu.SetSettingsOpen(open);
    }

    public void OnResumeButton()
    {
        pauseMenu.Resume();
    }

    public void OnMainMenuButton()
    {
        pauseMenu.PrepareForSceneChange();
        PlayGM.instance.QuitToMenu();
    }

    public void OnRestartButton()
    {
        pauseMenu.PrepareForSceneChange();

        LevelInfo info = PlayGM.instance != null ? PlayGM.instance.levelInfo : null;
        if (playLoaderPrefab == null)
        {
            Debug.LogError("[PausePanel] PlayLoader prefab not assigned.");
            return;
        }

        var loader = Instantiate(playLoaderPrefab);
        loader.levelInfo = info ?? new LevelInfo { name = "", isLocal = true };
        loader.playModeContext =
            PlayGM.instance != null
                ? PlayGM.instance.playModeContext
                : PlayGM.PlayModeContext.FromMainMenuPlayButton;
    }

    private IEnumerator EnableUIAfterFade()
    {
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (fadeDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(fadeDelaySeconds);

        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        InputModeTracker.EnsureInstance();

        var jiggle = GetComponent<SelectedJiggle>();
        if (jiggle == null)
            jiggle = gameObject.AddComponent<SelectedJiggle>();
        jiggle.SetScope(_activeContainer != null ? _activeContainer.transform : transform);

        var adapter = GetComponent<MenuInputModeAdapter>();
        if (adapter == null)
            adapter = gameObject.AddComponent<MenuInputModeAdapter>();
        adapter.SetScope(_activeContainer != null ? _activeContainer.transform : transform);

        RefreshNavigationFocus();
    }

    private void RefreshNavigationFocus()
    {
        if (InputModeTracker.Instance == null)
            return;
        if (InputModeTracker.Instance.CurrentMode != InputMode.Navigation)
            return;

        if (_activeContainer != null)
            MenuFocusUtility.SelectPreferred(_activeContainer);
        else
            MenuFocusUtility.SelectPreferred(gameObject);
    }

    private void ApplyButtonNavigation(GameObject container)
    {
        if (container == null)
            return;

        var selectables = container.GetComponentsInChildren<Selectable>(true);
        var activeSelectables = new System.Collections.Generic.List<Selectable>(selectables.Length);
        foreach (var selectable in selectables)
        {
            if (
                selectable != null
                && selectable.gameObject.activeInHierarchy
                && selectable.IsInteractable()
            )
            {
                if (
                    container == mainButtonContainer
                    && selectable.transform.parent != container.transform
                )
                    continue;
                activeSelectables.Add(selectable);
            }
        }

        if (container == mainButtonContainer)
            activeSelectables.Sort(
                (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex())
            );

        for (int i = 0; i < activeSelectables.Count; i++)
        {
            var nav = activeSelectables[i].navigation;
            nav.mode = Navigation.Mode.Explicit;
            if (container == mainButtonContainer && activeSelectables.Count > 1)
            {
                nav.selectOnUp =
                    i > 0
                        ? activeSelectables[i - 1]
                        : activeSelectables[activeSelectables.Count - 1];
                nav.selectOnDown =
                    i < activeSelectables.Count - 1
                        ? activeSelectables[i + 1]
                        : activeSelectables[0];
            }
            else
            {
                nav.selectOnUp = i > 0 ? activeSelectables[i - 1] : null;
                nav.selectOnDown =
                    i < activeSelectables.Count - 1 ? activeSelectables[i + 1] : null;
            }
            nav.selectOnLeft = null;
            nav.selectOnRight = null;
            activeSelectables[i].navigation = nav;
        }
    }
}

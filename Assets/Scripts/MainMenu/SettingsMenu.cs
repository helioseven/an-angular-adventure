using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("Audio")]
    public AudioMixer audioMixer;
    public MenuGM menuGM;
    public Slider masterVolumeSlider;
    public TMP_Text masterVolumeLabel;
    public Slider musicVolumeSlider;
    public TMP_Text musicVolumeLabel;

    public Slider sfxVolumeSlider;
    public TMP_Text sfxVolumeLabel;

    public Button backButton;

    [Header("Pause Menu Integration")]
    public PausePanel pausePanel;

    private const string masterVolumeKey = "MasterVolume";
    private const string musicVolumeKey = "MusicVolume";
    private const string sfxVolumeKey = "SFXVolume";

    private void OnEnable()
    {
        InputModeTracker.EnsureInstance();
        pausePanel.NotifySettingsOpen(true);

        var jiggle = GetComponent<SelectedJiggle>();
        if (jiggle == null)
            jiggle = gameObject.AddComponent<SelectedJiggle>();
        jiggle.SetScope(transform);

        var adapter = GetComponent<MenuInputModeAdapter>();
        if (adapter == null)
            adapter = gameObject.AddComponent<MenuInputModeAdapter>();
        adapter.SetScope(transform);
        adapter.SetPreferred(masterVolumeSlider);

        if (
            InputModeTracker.Instance != null
            && InputModeTracker.Instance.CurrentMode == InputMode.Navigation
            && masterVolumeSlider != null
        )
        {
            MenuFocusUtility.SelectPreferred(gameObject, masterVolumeSlider);
        }

        ApplyWrapNavigation();
    }

    private void OnDisable()
    {
        pausePanel.NotifySettingsOpen(false);
    }

    private void Start()
    {
        // Load saved preferences or use defaults (Master)
        float savedMasterVolume = PlayerPrefs.GetFloat(masterVolumeKey, 0.8f);
        masterVolumeSlider.value = savedMasterVolume;
        ApplyMasterVolume(savedMasterVolume);
        int percent = Mathf.RoundToInt(savedMasterVolume * 100f);
        masterVolumeLabel.text = percent + "%";

        // Load saved preferences or use defaults (Music)
        float savedMusicVolume = PlayerPrefs.GetFloat(musicVolumeKey, 0.8f);
        musicVolumeSlider.value = savedMusicVolume;
        ApplyMusicVolume(savedMusicVolume);
        percent = Mathf.RoundToInt(savedMusicVolume * 100f);
        musicVolumeLabel.text = percent + "%";

        // Load saved preferences or use defaults (SFX)
        float savedSFXVolume = PlayerPrefs.GetFloat(sfxVolumeKey, 0.8f);
        sfxVolumeSlider.value = savedSFXVolume;
        ApplySFXVolume(savedSFXVolume);
        percent = Mathf.RoundToInt(savedSFXVolume * 100f);
        sfxVolumeLabel.text = percent + "%";

        // Hook up slider events
        masterVolumeSlider.onValueChanged.AddListener(ApplyMasterVolume);
        musicVolumeSlider.onValueChanged.AddListener(ApplyMusicVolume);
        sfxVolumeSlider.onValueChanged.AddListener(ApplySFXVolume);

        // Hook up back button
        if (backButton != null)
            backButton.onClick.AddListener(() =>
            {
                if (pausePanel != null)
                    pausePanel.ShowMainMenu();
                else if (menuGM != null)
                    menuGM.OpenMainMenu();
            });
    }

    void Update()
    {
        if (
            Keyboard.current.escapeKey.wasPressedThisFrame
            || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
        )
        {
            if (pausePanel != null)
            {
                var pauseMenu = pausePanel.GetComponent<PauseMenu>();
                pausePanel.ShowMainMenu();
            }
            else if (menuGM != null)
            {
                menuGM.OpenMainMenu();
            }
        }
    }

    public void ApplyMasterVolume(float volume)
    {
        SoundManager.instance.SetMasterVolume(volume);

        // Update label
        if (masterVolumeLabel != null)
        {
            int percent = Mathf.RoundToInt(volume * 100f);
            masterVolumeLabel.text = percent + "%";
        }

        SaveSettings();
    }

    public void ApplyMusicVolume(float volume)
    {
        SoundManager.instance.SetMusicVolume(volume);

        // Update label
        if (musicVolumeLabel != null)
        {
            int percent = Mathf.RoundToInt(volume * 100f);
            musicVolumeLabel.text = percent + "%";
        }

        SaveSettings();
    }

    public void ApplySFXVolume(float volume)
    {
        SoundManager.instance.SetSFXVolume(volume);

        // Update label
        if (sfxVolumeLabel != null)
        {
            int percent = Mathf.RoundToInt(volume * 100f);
            sfxVolumeLabel.text = percent + "%";
        }

        SaveSettings();
    }

    // Call when exiting settings to make sure all prefs are saved
    public void SaveSettings()
    {
        PlayerPrefs.Save();
    }

    public void RefreshNavigation()
    {
        ApplyWrapNavigation();
    }

    private void ApplyWrapNavigation()
    {
        var order = new Selectable[]
        {
            masterVolumeSlider,
            musicVolumeSlider,
            sfxVolumeSlider,
            backButton,
        };

        var active = new System.Collections.Generic.List<Selectable>(order.Length);
        foreach (var selectable in order)
        {
            if (
                selectable == null
                || !selectable.gameObject.activeInHierarchy
                || !selectable.IsInteractable()
            )
                continue;
            active.Add(selectable);
        }

        if (active.Count < 2)
            return;

        for (int i = 0; i < active.Count; i++)
        {
            var nav = active[i].navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = i > 0 ? active[i - 1] : active[active.Count - 1];
            nav.selectOnDown = i < active.Count - 1 ? active[i + 1] : active[0];
            nav.selectOnLeft = null;
            nav.selectOnRight = null;
            active[i].navigation = nav;
        }
    }
}

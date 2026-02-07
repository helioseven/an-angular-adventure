using System.Text;
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

    [Header("Audio Credits")]
    public TMP_Text audioCreditsText;
    public Button audioCreditsButton;
    public GameObject audioCreditsPanel;
    public Button audioCreditsBackButton;

    private const string masterVolumeKey = "MasterVolume";
    private const string musicVolumeKey = "MusicVolume";
    private const string sfxVolumeKey = "SFXVolume";

    private bool IsPauseMenuSettings => pausePanel != null;

    private void OnEnable()
    {
        InputModeTracker.EnsureInstance();
        pausePanel?.NotifySettingsOpen(true);

        if (audioCreditsPanel != null)
            audioCreditsPanel.SetActive(false);

        if (IsPauseMenuSettings)
        {
            if (audioCreditsButton != null)
                audioCreditsButton.gameObject.SetActive(false);
            if (audioCreditsPanel != null)
                audioCreditsPanel.SetActive(false);
        }

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
        pausePanel?.NotifySettingsOpen(false);
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
            backButton.onClick.AddListener(HandleBackButton);

        if (!IsPauseMenuSettings)
        {
            if (audioCreditsButton != null)
                audioCreditsButton.onClick.AddListener(ToggleAudioCredits);
            UpdateAudioCreditsText();
        }
    }

    void Update()
    {
        if (
            Keyboard.current.escapeKey.wasPressedThisFrame
            || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
        )
        {
            if (audioCreditsPanel != null && audioCreditsPanel.activeSelf)
            {
                HideAudioCredits();
                return;
            }

            if (pausePanel != null)
            {
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
            audioCreditsButton,
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

    private void HandleBackButton()
    {
        if (audioCreditsPanel != null && audioCreditsPanel.activeSelf)
        {
            HideAudioCredits();
            return;
        }

        if (pausePanel != null)
            pausePanel.ShowMainMenu();
        else if (menuGM != null)
            menuGM.OpenMainMenu();
    }

    public void ShowAudioCredits()
    {
        if (audioCreditsPanel == null)
            return;

        if (audioCreditsPanel.activeSelf)
            return;

        audioCreditsPanel.SetActive(true);
        if (audioCreditsBackButton != null)
        {
            MenuFocusUtility.SelectPreferred(audioCreditsPanel, audioCreditsBackButton);
        }
        else if (backButton != null)
        {
            MenuFocusUtility.SelectPreferred(gameObject, backButton);
        }
    }

    public void HideAudioCredits()
    {
        if (audioCreditsPanel == null)
            return;

        audioCreditsPanel.SetActive(false);
        if (audioCreditsButton != null)
            MenuFocusUtility.SelectPreferred(gameObject, audioCreditsButton);
    }

    public void ToggleAudioCredits()
    {
        if (audioCreditsPanel == null)
            return;

        if (audioCreditsPanel.activeSelf)
            HideAudioCredits();
        else
            ShowAudioCredits();
    }

    private void UpdateAudioCreditsText()
    {
        if (audioCreditsText == null)
            return;

        var builder = new StringBuilder();

        TextAsset[] creditFiles = Resources.LoadAll<TextAsset>("AudioCredits");
        if (creditFiles == null || creditFiles.Length == 0)
        {
            builder.Append("No audio credits configured.");
            audioCreditsText.text = builder.ToString();
            return;
        }

        bool wroteAny = false;
        for (int fileIndex = 0; fileIndex < creditFiles.Length; fileIndex++)
        {
            TextAsset creditFile = creditFiles[fileIndex];
            if (creditFile == null)
                continue;

            AudioCredit entry = JsonUtility.FromJson<AudioCredit>(creditFile.text);
            if (entry == null || IsEmptyCredit(entry))
                continue;

            bool hasSoundName = !string.IsNullOrWhiteSpace(entry.sound_name);
            bool hasSoundUrl = !string.IsNullOrWhiteSpace(entry.sound_url);
            bool hasAuthorName = !string.IsNullOrWhiteSpace(entry.author_name);
            bool hasAuthorUrl = !string.IsNullOrWhiteSpace(entry.author_url);
            bool hasLicenseName = !string.IsNullOrWhiteSpace(entry.license_name);
            bool hasLicenseUrl = !string.IsNullOrWhiteSpace(entry.license_url);

            if (wroteAny)
                builder.AppendLine();

            if (hasSoundName)
                builder.Append(entry.sound_name.Trim());
            else
                builder.Append("Untitled");

            if (hasAuthorName)
                builder.Append(" by ").Append(entry.author_name.Trim());
            else if (hasAuthorUrl)
                builder.Append(" by ").Append(entry.author_url.Trim());

            if (hasSoundUrl)
                builder.Append(" -- ").Append(NormalizeSoundUrl(entry.sound_url));

            if (hasLicenseName)
                builder.Append(" -- License: ").Append(entry.license_name.Trim());
            else if (hasLicenseUrl)
                builder.Append(" -- License: ").Append(entry.license_url.Trim());

            wroteAny = true;
        }

        if (!wroteAny)
            builder.Append("No audio credits configured.");

        audioCreditsText.text = builder.ToString();
    }

    [System.Serializable]
    private class AudioCredit
    {
        public string sound_url;
        public string sound_name;
        public string author_url;
        public string author_name;
        public string license_url;
        public string license_name;
    }

    private static bool IsEmptyCredit(AudioCredit credit)
    {
        if (credit == null)
            return true;

        return string.IsNullOrWhiteSpace(credit.sound_url)
            && string.IsNullOrWhiteSpace(credit.sound_name)
            && string.IsNullOrWhiteSpace(credit.author_url)
            && string.IsNullOrWhiteSpace(credit.author_name)
            && string.IsNullOrWhiteSpace(credit.license_url)
            && string.IsNullOrWhiteSpace(credit.license_name);
    }

    private static string NormalizeSoundUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        string trimmed = url.Trim();
        if (!trimmed.Contains("freesound.org"))
            return trimmed;

        if (!System.Uri.TryCreate(trimmed, System.UriKind.Absolute, out System.Uri uri))
            return trimmed;

        string[] segments = uri.AbsolutePath.Trim('/').Split('/');
        if (segments.Length >= 4 && segments[0] == "people" && segments[2] == "sounds")
        {
            string id = segments[3];
            if (!string.IsNullOrWhiteSpace(id))
                return $"https://freesound.org/s/{id}/";
        }

        return trimmed;
    }
}

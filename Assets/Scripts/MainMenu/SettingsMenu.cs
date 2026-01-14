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

    private const string masterVolumeKey = "MasterVolume";
    private const string musicVolumeKey = "MusicVolume";
    private const string sfxVolumeKey = "SFXVolume";

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

        // Hook up slider event
        masterVolumeSlider.onValueChanged.AddListener(ApplyMasterVolume);

        // Hook up back button
        if (backButton != null)
            backButton.onClick.AddListener(() =>
            {
                menuGM.OpenMainMenu();
            });
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            menuGM.OpenMainMenu();
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
}

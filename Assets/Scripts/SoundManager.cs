using System;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    // public static variables
    public static SoundManager instance;
    public AudioMixer audioMixer;
    public AudioMixerGroup masterGroup;
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup sfxGroup;

    // public references
    public Sound[] sounds;

    // privates
    private const string masterVolumeKey = "MasterVolume";
    private const string musicVolumeKey = "MusicVolume";
    private const string sfxVolumeKey = "SFXVolume";

    void Awake()
    {
        // Solo Dolo
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        // Load our sounds :)
        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;

            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;

            // all of our sounds except our main theme song are sfx, so add them to that group by default
            s.source.outputAudioMixerGroup = sfxGroup;

            // if it's the main theme song, update mixer group to music group
            // TODO: using the name of the file is a bit janky, consider using the first file in the sounds array?
            if (s.name == "main-theme")
            {
                s.source.outputAudioMixerGroup = musicGroup;
            }

            // Allow music to keep playing while the game is paused via AudioListener.pause.
            s.source.ignoreListenerPause = s.source.outputAudioMixerGroup == musicGroup;
        }
    }

    void Start()
    {
        // apply user preferences (apply modifications from settings menu)
        float savedMasterVolume = PlayerPrefs.GetFloat(masterVolumeKey, 0.8f);
        Debug.Log("[SoundManagerStart] savedMasterVolume: " + savedMasterVolume);
        float dB = Mathf.Log10(Mathf.Clamp(savedMasterVolume, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat(masterVolumeKey, dB);

        // apply user preferences (apply modifications from settings menu)
        float savedMusic = PlayerPrefs.GetFloat(musicVolumeKey, 0.8f);
        Debug.Log("[SoundManagerStart] savedMusic: " + savedMusic);
        dB = Mathf.Log10(Mathf.Clamp(savedMusic, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat(musicVolumeKey, dB);

        // apply user preferences (apply modifications from settings menu)
        float savedSFXVolume = PlayerPrefs.GetFloat(sfxVolumeKey, 0.8f);
        Debug.Log("[SoundManagerStart] savedSFXVolume: " + savedSFXVolume);
        dB = Mathf.Log10(Mathf.Clamp(savedSFXVolume, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat(sfxVolumeKey, dB);

        // Kick off the theme song
        Play("main-theme");
    }

    /* Public Functions */

    // play our sounds; option to set volume too
    public void Play(string name, float volume = 0.5f)
    {
        if (!TryGetSound(name, out Sound s))
            return;

        // set the volume
        s.volume = volume;
        s.source.volume = volume;
        s.source.pitch = s.pitch;

        // play the sound
        s.source.Play();
    }

    public void PlayWithPitchVariance(string name, float pitchVariance, float volume = 0.5f)
    {
        if (!TryGetSound(name, out Sound s))
            return;

        // set the volume
        s.volume = volume;
        s.source.volume = volume;

        float variance = Mathf.Abs(pitchVariance);
        float pitchScale = UnityEngine.Random.Range(1f - variance, 1f + variance);
        s.source.pitch = s.pitch * pitchScale;

        // play the sound
        s.source.Play();
    }

    public void SetLoopingSound(string name, float volume, float pitch)
    {
        if (!TryGetSound(name, out Sound s))
            return;

        s.volume = volume;
        s.source.volume = volume;
        s.source.pitch = pitch;

        if (!s.source.isPlaying)
            s.source.Play();
    }

    public void StopSound(string name)
    {
        if (!TryGetSound(name, out Sound s))
            return;

        s.volume = 0f;
        s.source.volume = 0f;
        if (s.source.isPlaying)
            s.source.Stop();
    }

    public void SetMasterVolume(float volume)
    {
        float dB = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat(masterVolumeKey, dB);
        PlayerPrefs.SetFloat(masterVolumeKey, volume);
    }

    public void SetMusicVolume(float volume)
    {
        float dB = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat(musicVolumeKey, dB);
        PlayerPrefs.SetFloat(musicVolumeKey, volume);
    }

    public void SetSFXVolume(float volume)
    {
        float dB = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat(sfxVolumeKey, dB);
        PlayerPrefs.SetFloat(sfxVolumeKey, volume);
    }

    private bool TryGetSound(string name, out Sound sound)
    {
        sound = Array.Find(sounds, s => s.name == name);
        if (sound == null)
        {
            Debug.LogWarning("Sound name not recognized: " + name);
            return false;
        }
        if (sound.source == null)
        {
            Debug.LogWarning("Load me better: " + name);
            return false;
        }
        return true;
    }
}

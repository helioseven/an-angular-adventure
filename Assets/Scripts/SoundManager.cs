using UnityEngine.Audio;
using UnityEngine;
using System;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    public Sound[] sounds;

    void Awake ()
    {
        // Solo Dolo
        if (instance == null) {
            instance = this;
        } else 
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        // Load our sounds :)
        foreach(Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;

            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
        }
    }

    void Start()
    {
        Play("main-theme");
    }

    // Play - Play our sound. Option to set volume too
    public void Play(string name, float volume = .5f)
    {
        // get the sound
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null) {
            Debug.LogWarning ("Sound name not recognized: " + name);
            return;
        }
        else if (s.source == null) {
            Debug.LogWarning ("Load me better: " + name);
            return;
        }

        // set the volume
        s.volume = volume;
        s.source.volume = volume;

        // play the sound
        s.source.Play();
    }
}

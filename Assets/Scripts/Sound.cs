using UnityEngine;
using UnityEngine.Audio;

[System.Serializable]
public class Sound
{
    // public variables
    public AudioClip clip;
    public bool loop;

    [Range(.1f, 3f)]
    public float pitch;
    public string name;

    [Range(0f, 1f)]
    public float volume;

    // public-but-hidden variables
    [HideInInspector]
    public AudioSource source;
}

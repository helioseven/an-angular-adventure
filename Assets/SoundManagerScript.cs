using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManagerScript : MonoBehaviour
{

    public static AudioClip bounceSound, jumpSound;
    static AudioSource audioSource;
    // Start is called before the first frame update
    void Start()
    {
        bounceSound = Resources.Load<AudioClip> ("bounce");
        jumpSound = Resources.Load<AudioClip> ("jump");

        audioSource = GetComponent<AudioSource> ();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static void PlayOneShotSound (string clip)
    {
        switch (clip) {
            case "bounce":
                audioSource.PlayOneShot (bounceSound);
                break;
            case "jump":
                audioSource.PlayOneShot (jumpSound);
                break;
            default:
                Debug.LogError ("Sound name not recognized: " + clip);
                break;
        }
    }
}

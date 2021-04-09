using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManagerScript : MonoBehaviour
{

    public static AudioClip bounceSound, jumpSound, deathSound, warpSound, checkpointSound, gravitySound, iceSound;
    static AudioSource audioSource;
    // Start is called before the first frame update
    void Start()
    {
        bounceSound = Resources.Load<AudioClip> ("bounce");
        jumpSound = Resources.Load<AudioClip> ("jump");
        deathSound = Resources.Load<AudioClip> ("death");
        warpSound = Resources.Load<AudioClip> ("warp");
        checkpointSound = Resources.Load<AudioClip> ("checkpoint");
        gravitySound = Resources.Load<AudioClip> ("gravity");
        iceSound = Resources.Load<AudioClip> ("ice");

        audioSource = GetComponent<AudioSource> ();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static void PlayOneShotSound (string clip, float intensity = 0.5f)
    {
        switch (clip) {
            case "bounce":
                audioSource.PlayOneShot (bounceSound, intensity);
                break;
            case "jump":
                audioSource.PlayOneShot (jumpSound);
                break;
            case "death":
                audioSource.PlayOneShot (deathSound);
                break;
            case "warp":
                audioSource.PlayOneShot (warpSound);
                break;
             case "checkpoint":
                audioSource.PlayOneShot (checkpointSound);
                break;
            case "gravity":
                audioSource.PlayOneShot (gravitySound, intensity);
                break;
            case "ice":
                audioSource.PlayOneShot (iceSound, intensity);
                break;
            default:
                Debug.LogError ("Sound name not recognized: " + clip);
                break;
        }
    }
}

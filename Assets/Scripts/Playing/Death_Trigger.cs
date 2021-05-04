using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Death_Trigger : MonoBehaviour
{

    public PlayGM play_gm;

    void Awake()
    {
        play_gm = PlayGM.instance;
    }

    // triggers player's death when it detects player collision
    void OnCollisionEnter2D(Collision2D other)
    {
        // identifies the player by tag
        if (other.gameObject.CompareTag("Player"))
        {
            play_gm.soundManager.Play("death");
            PlayGM.instance.KillPlayer();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Death_Trigger : MonoBehaviour
{
    // private references
    private PlayGM _playGM;

    void Awake()
    {
        _playGM = PlayGM.instance;
    }

    /* Override Functions */

    // triggers player's death when it detects player collision
    void OnCollisionEnter2D(Collision2D other)
    {
        // identifies the player by tag
        if (other.gameObject.CompareTag("Player"))
        {
            _playGM.KillPlayer();
        }
    }
}

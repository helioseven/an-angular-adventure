using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using circleXsquares;

public class Checkpoint : MonoBehaviour
{

    private PlayGM play_gm;

    public ChkpntData data;

    void Awake()
    {
        play_gm = PlayGM.instance;
    }

    // becomes the current checkpoint when it detects a collision with the player
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            play_gm.soundManager.Play("checkpoint");
            play_gm.SetCheckpoint(data);
        }
    }
}

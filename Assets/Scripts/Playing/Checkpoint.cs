using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using circleXsquares;

public class Checkpoint : MonoBehaviour
{
    // public variables
    public ChkpntData data;

    // private references
    private PlayGM _playGM;

    void Awake()
    {
        _playGM = PlayGM.instance;
    }

    /* Override Functions */

    // becomes the current checkpoint when it detects a collision with the player
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player")) {
            _playGM.soundManager.Play("checkpoint");
            _playGM.SetCheckpoint(data);
        }
    }
}

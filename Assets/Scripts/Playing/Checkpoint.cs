using System.Collections;
using System.Collections.Generic;
using circleXsquares;
using UnityEngine;

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
        if (other.gameObject.CompareTag("Player"))
        {
            _playGM.soundManager.Play("checkpoint");
            _playGM.SetCheckpoint(gameObject);
            _playGM.SetCheckpointData(data);

            ParticleSystem checkpointBurst;
            Transform child = transform.Find("CheckpointBurst");
            if (child != null)
            {
                checkpointBurst = child.GetComponent<ParticleSystem>();
                // ✨ Trigger the particle burst
                checkpointBurst.Play();
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using circleXsquares;
using UnityEngine;

public class Tile_Purple : Tile
{
    /* Override Functions */

    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            float volume = _gmRef.ImpactIntensityToVolume(
                other.relativeVelocity,
                Physics2D.gravity
            );
            _gmRef.soundManager.Play("bounce", volume);
        }
    }

    /* Private Functions */

    // purple tiles have no player action
    private void playerAction()
    {
        return;
    }
}

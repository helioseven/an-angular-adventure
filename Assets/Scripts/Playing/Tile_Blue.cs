using System.Collections;
using System.Collections.Generic;
using circleXsquares;
using UnityEngine;

public class Tile_Blue : Tile
{
    // private variables
    private float _maxVolume = 0.5f;
    private float _volumeMultiplier = 0.25f;

    /* Override Functions */

    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Vector2 vel = other.relativeVelocity;
            Vector2 grav = Physics2D.gravity;

            // dot product calculates projection of velocity vector onto vector perpendicular gravity vector
            float slideForce = grav.x * vel.y + grav.y * vel.x;
            slideForce = Mathf.Abs(slideForce) / 10.0f;

            float intensity = Mathf.Clamp(slideForce * _volumeMultiplier, 0f, _maxVolume);
            _gmRef.soundManager.Play("ice", intensity);
        }
    }

    /* Private Functions */

    // blue tiles have no player action
    private void playerAction()
    {
        return;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using circleXsquares;
using UnityEngine;

public class Tile_Blue : Tile
{
    // private variables
    private float _maxVolume = 0.5f;
    private float _volumeMultiplier = 0.25f;
    private bool isOnIce;
    private Vector2 slopeNormal;

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

            foreach (ContactPoint2D contact in other.contacts)
            {
                if (contact.collider.CompareTag("Player"))
                {
                    slopeNormal = contact.normal;
                    isOnIce = true;
                    other.gameObject.GetComponent<Player_Controller>().isOnIce = true;
                    other.gameObject.GetComponent<Player_Controller>().isIceScalingBlockingJump =
                        !CanJump();
                }
            }

            // other.gameObject.GetComponent<Player_Controller>().isOnIce = true;
            // isOnIce = true;
            // other.gameObject.GetComponent<Player_Controller>().isIceScalingBlockingJump = true;
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.collider.CompareTag("Player"))
            {
                slopeNormal = contact.normal;
                isOnIce = true;
                collision.gameObject.GetComponent<Player_Controller>().isOnIce = true;
                collision.gameObject.GetComponent<Player_Controller>().isIceScalingBlockingJump =
                    !CanJump();

                // Debug.Log("[BlueTiile] [OnCollisionStay2D] slopeNormal: " + slopeNormal);
                // Debug.Log("[BlueTiile] [OnCollisionStay2D] CanJump(_): " + CanJump());
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<Player_Controller>().isOnIce = false;
            isOnIce = false;
            collision.gameObject.GetComponent<Player_Controller>().isIceScalingBlockingJump = false;
        }
    }

    /* Private Functions */

    bool CanJump()
    {
        Debug.Log("[BlueTiile] [CanJump] slopeNormal: " + slopeNormal);
        Debug.Log("[BlueTiile] [CanJump] (returns if false)isOnIce: " + isOnIce);

        if (!isOnIce)
            return true;

        // Disallow jump if facing into the slope (i.e., trying to scale it)
        float slopeDot = Vector2.Dot(slopeNormal, Vector2.up);
        Debug.Log("slopeDot: " + slopeDot);
        bool shouldBeAbleToJump = Math.Abs(slopeDot) > .6f;
        Debug.Log("[BlueTiile] [CanJump] shouldBeAbleToJump: " + shouldBeAbleToJump);

        return shouldBeAbleToJump;
    }
}

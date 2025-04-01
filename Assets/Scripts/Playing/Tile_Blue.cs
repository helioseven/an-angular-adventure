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
                    Player_Controller playerController =
                        other.gameObject.GetComponent<Player_Controller>();
                    playerController.isOnIce = true;
                    playerController.isIceScalingBlockingJump = !CanJump(
                        playerController.GetGravityDirection()
                    );
                }
            }
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
                Player_Controller playerController =
                    collision.gameObject.GetComponent<Player_Controller>();
                playerController.isOnIce = true;
                playerController.isIceScalingBlockingJump = !CanJump(
                    playerController.GetGravityDirection()
                );

                // Debug.Log("[BlueTiile] [OnCollisionStay2D] slopeNormal: " + slopeNormal);
                // Debug.Log("[BlueTiile] [OnCollisionStay2D] CanJump(_): " + CanJump());
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            isOnIce = false;
            Player_Controller playerController =
                collision.gameObject.GetComponent<Player_Controller>();
            playerController.isOnIce = false;
            playerController.isIceScalingBlockingJump = false;
        }
    }

    /* Private Functions */
    private bool CanJump(PlayGM.GravityDirection gravityDirection)
    {
        // Debug.Log("[BlueTiile] [CanJump] slopeNormal: " + slopeNormal);
        // Debug.Log("[BlueTiile] [CanJump] (returns if false)isOnIce: " + isOnIce);

        float breakPoint = 0.6f;

        if (!isOnIce)
            return true;

        // Disallow jump if facing into the slope (i.e., trying to scale it)
        float slopeDot = Vector2.Dot(slopeNormal, Vector2.up);
        float slopeDotGoofy = Vector2.Dot(slopeNormal, Vector2.right);

        // Debug.Log("slopeDot: " + slopeDot);
        // Debug.Log("slopeDotGoofy: " + slopeDotGoofy);

        bool shouldBeAbleToJump = Math.Abs(slopeDot) > breakPoint;
        // need to use goofy when gravity is left or right
        if (
            gravityDirection == PlayGM.GravityDirection.Left
            || gravityDirection == PlayGM.GravityDirection.Right
        )
        {
            shouldBeAbleToJump = Math.Abs(slopeDotGoofy) > breakPoint;
        }

        // Debug.Log("[BlueTiile] [CanJump] shouldBeAbleToJump: " + shouldBeAbleToJump);

        return shouldBeAbleToJump;
    }
}

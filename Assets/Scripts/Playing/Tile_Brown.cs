using System.Collections;
using System.Collections.Generic;
using circleXsquares;
using UnityEngine;

public class Tile_Brown : Tile
{
    /* Override Functions */

    void OnCollisionEnter2D(Collision2D other)
    {
        // identifies the player by tag
        if (other.gameObject.CompareTag("Player"))
        {
            Rigidbody2D rb = other.rigidbody;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            playerAction();
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody2D rb = collision.rigidbody;
            if (rb != null)
            {
                rb.linearVelocity *= 0.9f;
                rb.angularVelocity = 0f;
            }
        }
    }

    /* Private Functions */

    // brown tiles have no player action
    private void playerAction()
    {
        if (_gmRef != null && _gmRef.soundManager != null)
            _gmRef.soundManager.Play("brownStick");
        return;
    }
}

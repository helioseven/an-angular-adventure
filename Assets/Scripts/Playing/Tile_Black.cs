using System.Collections;
using System.Collections.Generic;
using circleXsquares;
using UnityEngine;

public class Tile_Black : Tile
{
    /* Override Functions */

    void OnCollisionEnter2D(Collision2D other)
    {
        // identifies the player by tag
        if (other.gameObject.CompareTag("Player"))
            playerAction();
    }

    /* Private Functions */

    // black tiles have no player action
    private void playerAction()
    {
        return;
    }
}

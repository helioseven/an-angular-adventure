using System.Collections;
using System.Collections.Generic;
using circleXsquares;
using UnityEngine;

public class Tile_White : Tile
{
    /* Override Functions */

    void OnCollisionEnter2D(Collision2D other)
    {
        // identifies the player by tag
        if (other.gameObject.CompareTag("Player"))
            playerAction();
    }

    /* Private Functions */

    // white tiles have no player action
    private void playerAction()
    {
        return;
    }
}

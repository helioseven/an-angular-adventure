﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using circleXsquares;

public class Tile_Brown : Tile
{
    /* Override Functions */

    void OnCollisionEnter2D(Collision2D other)
    {
        // identifies the player by tag
        if (other.gameObject.CompareTag("Player"))
            playerAction();
    }

    /* Private Functions */

    // brown tiles have no player action
    private void playerAction()
    {
        return;
    }
}

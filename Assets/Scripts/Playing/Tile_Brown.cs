using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using circleXsquares;

public class Tile_Brown : Tile
{

    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player")) playerAction(); // <1>

        /*
        <1> identifies the player by tag
        */
    }

    //
    private void playerAction()
    {
        Debug.Log("Brown");
    }
}

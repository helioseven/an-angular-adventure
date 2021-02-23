using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using circleXsquares;

public class Tile_Orange : Tile
{

  // triggers gravity redirection when it detects player collision
	void OnCollisionEnter2D (Collision2D other)
	{
		if (other.gameObject.CompareTag("Player")) redirectGravity(); // <1>

    /*
    <1> identifies the player by tag
    */
	}

  //
  private void redirectGravity ()
  {
    //
  }
}

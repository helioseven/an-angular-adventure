using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using circleXsquares;

public class Tile_Green : Tile {

	// triggers switch toggle when it detects player collision
	void OnCollisionEnter2D (Collision2D other)
	{
		if (other.gameObject.CompareTag("Player")) toggleSwitch(); // <1>

    /*
    <1> identifies the player by tag
    */
	}

  //
  private void toggleSwitch ()
  {
    Debug.Log("Green");
  }
}

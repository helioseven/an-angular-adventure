using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using circleXsquares;
public class Tile_Blue : Tile
{	
    private float maxVolume = 0.5f;
	private float volumeMultiplier = 0.25f;
    void OnCollisionEnter2D (Collision2D other)
	{
		if (other.gameObject.CompareTag("Player")) {
			Vector2 vel = other.relativeVelocity;
			Vector2 grav = Physics2D.gravity;

			// dot product calculates projection of velocity vector onto vector perpendicular gravity vector
			float slideForce = grav.x * vel.y + grav.y * vel.x;
			slideForce = Mathf.Abs(slideForce) / 10.0f;

			float intensity = Mathf.Clamp(slideForce * volumeMultiplier, 0f, maxVolume);
			// Debug.Log ("Blue Tile Ice Slide intensity: " + intensity + "\t slideForce: " + slideForce);
			FindObjectOfType<SoundManager>().Play ("ice", intensity);
        }
	}
}

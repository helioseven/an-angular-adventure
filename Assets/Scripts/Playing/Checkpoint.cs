using System.Collections;
using UnityEngine;

public class Checkpoint : MonoBehaviour {

	// becomes the current checkpoint when it detects a collision with the player
	void OnTriggerEnter2D (Collider2D other)
	{
		// identifies the player by tag
		if (other.CompareTag("Player")) {
			PlayGM.instance.SetCheckpoint(gameObject);
		}
	}
}
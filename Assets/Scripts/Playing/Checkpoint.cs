using System.Collections;
using UnityEngine;

public class Checkpoint : MonoBehaviour {

	// becomes the current checkpoint when it detects a collision with the player
	void OnTriggerEnter2D (Collider2D other)
	{
		PlayGM.instance.SetCheckpoint(gameObject);
	}
}
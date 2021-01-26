using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour {

	PlayGM play_gm;

	void Awake ()
	{
		play_gm = PlayGM.instance;
	}

	// becomes the current checkpoint when it detects a collision with the player
	void OnTriggerEnter2D (Collider2D other)
	{
		play_gm.SetCheckpoint(gameObject);
	}
}
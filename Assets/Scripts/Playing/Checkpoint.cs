using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour {

	//
	public int layer { get; private set; }

	void Awake ()
	{
		// assign layer
		layer = 0;
	}

	// becomes the current checkpoint when it detects a collision with the player
	void OnTriggerEnter2D (Collider2D other)
	{
		PlayGM.instance.SetCheckpoint(gameObject);
	}
}
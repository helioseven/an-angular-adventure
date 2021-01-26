using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using circleXsquares;

public class Checkpoint : MonoBehaviour {

	public PlayGM play_gm;

	public ChkpntData data { get; private set; }

	void Awake ()
	{
		play_gm = PlayGM.instance;
	}

	void Start ()
	{
		int i = transform.GetSiblingIndex();
		data = play_gm.levelData.chkpntSet[i];
	}

	// becomes the current checkpoint when it detects a collision with the player
	void OnTriggerEnter2D (Collider2D other)
	{
		play_gm.SetCheckpoint(data);
	}
}
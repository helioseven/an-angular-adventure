using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using circleXsquares;

public class Warp : MonoBehaviour {

	public PlayGM play_gm;

	public int baseLayer;
	public int targetLayer;
	public WarpData data;

	void Awake ()
	{
		play_gm = PlayGM.instance;
	}

	void OnTriggerEnter2D (Collider2D other)
	{
		play_gm.WarpPlayer(baseLayer, targetLayer);
	}
}
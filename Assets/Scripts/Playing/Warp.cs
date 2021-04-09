using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using circleXsquares;

public class Warp : MonoBehaviour {

	public WarpData data;
	public int baseLayer {
		get { return data.orient.layer; }
		set {}
	}
	public int targetLayer {
		get { return data.targetLayer; }
		set {}
	}

	private  PlayGM play_gm;

	void Awake ()
	{
		play_gm = PlayGM.instance;
	}

	void OnTriggerEnter2D (Collider2D other)
	{
		if (other.gameObject.CompareTag("Player")) {
			SoundManagerScript.PlayOneShotSound("warp");
			play_gm.WarpPlayer(baseLayer, targetLayer);
		}
	}
}

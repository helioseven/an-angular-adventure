using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayCam_Controller : MonoBehaviour {

	private Vector3 velocity = Vector3.zero;
	private GameObject player;

	void Start ()
	{
		velocity = PlayGM.instance.player.transform.position;
		player = GameObject.FindWithTag("Player");
	}

	// uses SmoothDamp to move camera towards the player at all times
	void Update ()
	{
		Vector3 target = player.transform.position;
		Vector3 v3 = transform.position;

		v3 = Vector3.SmoothDamp(v3, target, ref velocity, 0.3f);
		v3.z = target.z - 8.0f;
		transform.position = v3;
	}
}
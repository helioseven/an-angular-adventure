using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayCam_Controller : MonoBehaviour {

	private Vector3 velocity = Vector3.zero;

	private GameObject player;

	void Start ()
	{
//		gameObject.GetComponent<Camera>().cullingMask = ~(1 << 10);

		velocity = PlayGM.instance.player.transform.position;
		player = GameObject.FindWithTag("Player");
	}

	// uses SmoothDamp to move camera towards the player at all times
	void Update ()
	{
		Vector3 target = player.transform.position;
		Vector3 tempVec3 = transform.position;

		tempVec3 = Vector3.SmoothDamp(tempVec3, target, ref velocity, 0.3f);
		tempVec3.z = target.z - 8.0f;
		transform.position = tempVec3;
	}
}
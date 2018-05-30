using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayCamControl : MonoBehaviour {

	private Vector3 velocity = Vector3.zero;

	void Start ()
	{
		velocity = PlayGM.instance.player.transform.position;
	}

	// uses SmoothDamp to move camera towards the player at all times
	void Update ()
	{
		Vector3 target = PlayGM.instance.player.transform.position;
		Vector3 tempVec3 = transform.position;

		tempVec3 = Vector3.SmoothDamp(tempVec3, target, ref velocity, 0.3f);
		// Vector3.back ensure camera has a lower Z-depth value than objects in the scene
		tempVec3 += Vector3.back;
		transform.position = tempVec3;
	}
}
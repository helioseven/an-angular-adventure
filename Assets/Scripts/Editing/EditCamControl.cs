using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using InputKeys = EditGM.InputKeys;

public class EditCamControl : MonoBehaviour {

	// private variables
	private EditGM gm_ref;
	private InputKeys key_mask;
	private InputKeys cam_inputs;

	void Start ()
	{
		gm_ref = EditGM.instance;
		key_mask = (InputKeys.W | InputKeys.A | InputKeys.S | InputKeys.D);
	}

	void Update ()
	{
		cam_inputs = gm_ref.getKeys;
		cam_inputs &= key_mask; // <1>

		InputKeys tempKeys = (InputKeys.W | InputKeys.S); // <2>
		if (gm_ref.CheckKeys(tempKeys)) cam_inputs ^= tempKeys;
		tempKeys = (InputKeys.A | InputKeys.D);
		if (gm_ref.CheckKeys(tempKeys)) cam_inputs ^= tempKeys;

		Vector3 v3 = transform.position; // <3>
		if ((cam_inputs & InputKeys.W) == InputKeys.W) v3.y += (5.0f * Time.deltaTime);
		if ((cam_inputs & InputKeys.A) == InputKeys.A) v3.x -= (5.0f * Time.deltaTime);
		if ((cam_inputs & InputKeys.S) == InputKeys.S) v3.y -= (5.0f * Time.deltaTime);
		if ((cam_inputs & InputKeys.D) == InputKeys.D) v3.x += (5.0f * Time.deltaTime);

		v3.z = gm_ref.GetLayerDepth() - 8f; // <4>
		transform.position = v3;
	}

	/*
	<1> mask identifying the relevant keys (WASD) to the camera control
	<2> tempKeys is used to identify opposite-direction pairs and remove them
	<3> uses the isolated cam_inputs to modify a temporary position variable
	<4> get layer depth from the GM, set it back 8 units, and use to set position
	*/
}
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
		key_mask = (InputKeys.Up | InputKeys.Left | InputKeys.Down | InputKeys.Right);
	}

	void Update ()
	{
		cam_inputs = gm_ref.getInputs;
		cam_inputs &= key_mask; // <1>

		Vector3 v3 = transform.position; // <3>
		if ((cam_inputs & InputKeys.Up) == InputKeys.Up) v3.y += (5.0f * Time.deltaTime);
		if ((cam_inputs & InputKeys.Left) == InputKeys.Left) v3.x -= (5.0f * Time.deltaTime);
		if ((cam_inputs & InputKeys.Down) == InputKeys.Down) v3.y -= (5.0f * Time.deltaTime);
		if ((cam_inputs & InputKeys.Right) == InputKeys.Right) v3.x += (5.0f * Time.deltaTime);

		v3.z = gm_ref.GetLayerDepth() - 8f; // <4>
		transform.position = v3;
	}

	/*
	<1> mask identifying the relevant keys (WASD) to the camera control
	<2> uses the isolated cam_inputs to modify a temporary position variable
	<3> get layer depth from the GM, set it back 8 units, and use to set position
	*/
}
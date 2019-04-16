using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using inputKeys = EditGM.inputKeys;

public class EditCamControl : MonoBehaviour {

	private EditGM gmRef;

	void Start ()
	{
		gmRef = EditGM.instance;
	}

	void Update ()
	{
		Vector3 v3 = transform.position;

		inputKeys camInputs = inputKeys.None;
		inputKeys[] wasdInputs = new inputKeys[] {inputKeys.W, inputKeys.S, inputKeys.A, inputKeys.D};
		foreach (inputKeys ik in wasdInputs) {
			if (gmRef.checkKeys(ik)) camInputs |= ik;
		}

		// tempKeys is used to identify opposite-direction pairs and remove them
		inputKeys tempKeys = (inputKeys.W | inputKeys.S);
		if ((camInputs & tempKeys) == tempKeys) camInputs ^= tempKeys;
		tempKeys = (inputKeys.A | inputKeys.D);
		if ((camInputs & tempKeys) == tempKeys) camInputs ^= tempKeys;

		// uses the isolated camInputs to modify a temporary position variable
		switch (camInputs) {
			case inputKeys.W: {
				v3.y += (5.0f * Time.deltaTime);
				break; }
			case inputKeys.A: {
				v3.x -= (5.0f * Time.deltaTime);
				break; }
			case inputKeys.S: {
				v3.y -= (5.0f * Time.deltaTime);
				break; }
			case inputKeys.D: {
				v3.x += (5.0f * Time.deltaTime);
				break; }
			case (inputKeys.W | inputKeys.A): {
				v3.y += (5.0f * Time.deltaTime);
				v3.x -= (5.0f * Time.deltaTime);
				break; }
			case (inputKeys.W | inputKeys.D): {
				v3.y += (5.0f * Time.deltaTime);
				v3.x += (5.0f * Time.deltaTime);
				break; }
			case (inputKeys.S | inputKeys.A): {
				v3.y -= (5.0f * Time.deltaTime);
				v3.x -= (5.0f * Time.deltaTime);
				break; }
			case (inputKeys.S | inputKeys.D): {
				v3.y -= (5.0f * Time.deltaTime);
				v3.x += (5.0f * Time.deltaTime);
				break; }
		}

		v3.z = gmRef.getLayerDepth() - 8f;
		transform.position = v3;
	}
}
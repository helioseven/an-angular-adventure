using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using inputKeys = EditGM.inputKeys;

public class Camera_Controller : MonoBehaviour {

	// camera dolly is updated based on WASD input
	void Update () {
		Vector3 tempVec3 = transform.position;

		// camInputs isolates meaningful camera-controlling key inputs
		inputKeys camInputs = (inputKeys.W | inputKeys.A | inputKeys.S | inputKeys.D);
		// gets a copy of current getKeys from GM instance
		inputKeys getKeys = EditGM.instance.getInputs(false);
		camInputs = camInputs & getKeys;

		// tempKeys is used to identify opposite-direction pairs and remove them
		inputKeys tempKeys = (inputKeys.W | inputKeys.S);
		if ((camInputs & tempKeys) == tempKeys) camInputs = camInputs ^ tempKeys;
		tempKeys = (inputKeys.A | inputKeys.D);
		if ((camInputs & tempKeys) == tempKeys) camInputs = camInputs ^ tempKeys;

		// uses the isolated camInputs to modify a temporary position variable
		switch (camInputs) {
			case inputKeys.W: {
				tempVec3.y += (5.0f * Time.deltaTime);
				break; }
			case inputKeys.A: {
				tempVec3.x -= (5.0f * Time.deltaTime);
				break; }
			case inputKeys.S: {
				tempVec3.y -= (5.0f * Time.deltaTime);
				break; }
			case inputKeys.D: {
				tempVec3.x += (5.0f * Time.deltaTime);
				break; }
			case (inputKeys.W | inputKeys.A): {
				tempVec3.y += (5.0f * Time.deltaTime);
				tempVec3.x -= (5.0f * Time.deltaTime);
				break; }
			case (inputKeys.W | inputKeys.D): {
				tempVec3.y += (5.0f * Time.deltaTime);
				tempVec3.x += (5.0f * Time.deltaTime);
				break; }
			case (inputKeys.S | inputKeys.A): {
				tempVec3.y -= (5.0f * Time.deltaTime);
				tempVec3.x -= (5.0f * Time.deltaTime);
				break; }
			case (inputKeys.S | inputKeys.D): {
				tempVec3.y -= (5.0f * Time.deltaTime);
				tempVec3.x += (5.0f * Time.deltaTime);
				break; }
		}

		transform.position = tempVec3;
	}
}

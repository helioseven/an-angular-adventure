using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using circleXsquares;

public class HUDControl : MonoBehaviour {

	// activates the panel
	public void Activate () {
		gameObject.SetActive(true);
	}

	// deactivates the panel
	public void Deactivate () {
		gameObject.SetActive(false);
	}
}
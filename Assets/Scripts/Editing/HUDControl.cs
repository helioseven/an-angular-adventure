using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using circleXsquares;

public class HUDControl : MonoBehaviour {

	// public read-accessibility state variables
	public bool activeSelf { get { return gameObject.activeSelf; } private set {} }

	public void Awake ()
	{
		Deactivate();
	}

	// activates the panel
	public void Activate ()
	{
		gameObject.SetActive(true);
	}

	// deactivates the panel
	public void Deactivate ()
	{
		gameObject.SetActive(false);
	}
}
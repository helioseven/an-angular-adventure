using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarpCam_Controller : MonoBehaviour {

	void Start ()
	{
		gameObject.GetComponent<Camera>().cullingMask = 1 << 10;
	}
}

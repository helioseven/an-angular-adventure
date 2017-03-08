using UnityEngine;
using System.Collections;

public class Destroy_Self_Delayed : MonoBehaviour {

	// Use this for initialization
	void Awake () {
		Invoke ("DestroySelf", 1.0f);
	}

	void DestroySelf()
	{
		Destroy (gameObject);
	}
}

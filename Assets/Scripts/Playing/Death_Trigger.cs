using UnityEngine;
using System.Collections;

public class Death_Trigger : MonoBehaviour {

	void OnTriggerEnter2D (Collider2D other)
	{
		if (other.gameObject.CompareTag ("Player")) {
			CallResetToCheckpoint();
		}
	}
	
	void CallResetToCheckpoint()
	{
		PlayGM.instance.KillPlayer();
	}

}

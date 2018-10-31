using System.Collections;
using UnityEngine;

public class DeathTrigger : MonoBehaviour {

	// triggers player's death when it detects a collision with the player
	void OnCollisionEnter2D (Collision2D other)
	{
		// identifies the player by tag
		if (other.gameObject.CompareTag("Player")) {
			PlayGM.instance.KillPlayer();
		}
	}

}

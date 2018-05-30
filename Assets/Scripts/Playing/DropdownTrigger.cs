using System.Collections;
using UnityEngine;

// this whole script isn't getting used in this version

public class DropdownTrigger : MonoBehaviour {

	void OnTriggerEnter2D(Collider2D other){

		if (other.gameObject.CompareTag ("Player")) {

			float x = PlayGM.instance.player.transform.position.x;
			float y = PlayGM.instance.player.transform.position.y;
			// position drop
			PlayGM.instance.player.transform.position = new Vector3(x,y,3);

			// physics layer change
//			Physics2D.IgnoreLayerCollision(

			PlayGM.instance.player.layer = 9;
		}

	}
}
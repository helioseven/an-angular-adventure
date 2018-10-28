using UnityEngine;
using System.Collections;

public class Dropdown : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnTriggerEnter2D(Collider2D other){

		if (other.gameObject.CompareTag ("Player")) {

			float x = GM.instance.player.transform.position.x;
			float y = GM.instance.player.transform.position.y;
			// position drop
			GM.instance.player.transform.position = new Vector3(x,y,3);

			//physics layer change
//			Physics2D.IgnoreLayerCollision(

			GM.instance.player.layer = 9;
		}

	}
}

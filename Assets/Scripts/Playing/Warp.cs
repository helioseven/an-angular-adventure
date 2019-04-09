using System.Collections;
using UnityEngine;

public class Warp : MonoBehaviour {

	public int base_level;
	public int target_level;

	void OnTriggerEnter2D(Collider2D other){
		if (other.gameObject.CompareTag("Player")) PlayGM.instance.WarpPlayer(this);
	}
}
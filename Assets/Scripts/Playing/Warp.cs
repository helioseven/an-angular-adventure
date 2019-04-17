using System.Collections;
using UnityEngine;

public class Warp : MonoBehaviour {

	public int baseLevel;
	public int targetLevel;

	void OnTriggerEnter2D(Collider2D other){
		if (other.gameObject.CompareTag("Player")) PlayGM.instance.WarpPlayer(this);
	}
}
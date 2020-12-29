using System.Collections;
using UnityEngine;

public class Warp : MonoBehaviour {

	public int baseLayer;
	public int targetLayer;

	void OnTriggerEnter2D(Collider2D other) {
		if (other.gameObject.CompareTag("Player")) PlayGM.instance.WarpPlayer(this);
	}
}
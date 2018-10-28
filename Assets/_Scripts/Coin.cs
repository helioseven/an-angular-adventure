using UnityEngine;
using System.Collections;

public class Coin : MonoBehaviour {

	void OnTriggerEnter2D(Collider2D other)
	{
		if (other.gameObject.CompareTag ("Player")) {
			GM.instance.addCoin();
			Destroy(gameObject);
		}
	}

}

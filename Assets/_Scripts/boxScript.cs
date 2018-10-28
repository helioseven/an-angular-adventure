using UnityEngine;
using System.Collections;

public class boxScript : MonoBehaviour {


	void Start() {

		PolygonCollider2D my_col = gameObject.AddComponent<PolygonCollider2D> ();
		Vector2[] setPoints = new Vector2[] {new Vector2 (-1, -1), new Vector2 (-1, 1), new Vector2 (1, 1), new Vector2 (1, -1)};

		my_col.SetPath (0, setPoints);
	}
}

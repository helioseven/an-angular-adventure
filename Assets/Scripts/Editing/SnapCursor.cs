using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using circleXsquares;

public class SnapCursor : MonoBehaviour {

	// focus and anchor keep track of where the mouse input is snapping to
	public hexLocus focus { get; private set; }
	private hexLocus anchor;
	// fShift updates every frame with the distance between the mouse and the anchor
	private Vector3 fShift;

	void Awake ()
	{
		focus = new hexLocus();
		anchor = new hexLocus();
		fShift = new Vector3();
	}

	void Update ()
	{
		// focus is updated first based on the anchor and the mouse position
		fShift = Camera.main.ScreenToWorldPoint(Input.mousePosition) - anchor.toUnitySpace();
		focus = new hexLocus(fShift);
		focus += anchor;
	}

	// finds the closest snap point to the current mouse position and sets the anchor there
	public void findNewAnchor (Dictionary<GameObject, tileData> placedTiles)
	{
		// generates a list of all collisions within a radius 0.5 circle from current mouse position
		hexLocus newAnchor = new hexLocus();
		Vector2 inputPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Collider2D[] hitCols = Physics2D.OverlapCircleAll(inputPos, 0.5f);
		List<hexLocus> locusSnaps = new List<hexLocus>();

		// checks every vertex of each PolygonCollider reporting a hit
		foreach (Collider2D c2d in hitCols) {
			PolygonCollider2D pc2d = c2d as PolygonCollider2D;
			if (pc2d) {
				// if the collision is not from a placed tile, it is skipped
				if (!placedTiles.ContainsKey(c2d.gameObject)) continue;
				hexLocus tHL = placedTiles[c2d.gameObject].locus;
				foreach (Vector2 subPoint in pc2d.points) {
					// adds each vertex to the list of possible snap points
					hexLocus newPoint = new hexLocus(c2d.transform.TransformPoint(subPoint) - tHL.toUnitySpace());
					newPoint += tHL;
					locusSnaps.Add(newPoint);
					tHL = newPoint;
				}
			}
		}

		// finds the hexLocus with the smallest offset from original input position
		foreach (hexLocus hL in locusSnaps) {
			Vector2 newOffset = (Vector2)hL.toUnitySpace() - inputPos;
			Vector2 oldOffset = (Vector2)newAnchor.toUnitySpace() - inputPos;
			if (oldOffset.magnitude > newOffset.magnitude) newAnchor = hL;
		}

		// updates global variables
		anchor = newAnchor;
		transform.position = anchor.toUnitySpace();
	}
}

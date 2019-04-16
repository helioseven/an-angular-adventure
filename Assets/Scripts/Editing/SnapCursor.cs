using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using circleXsquares;

public class SnapCursor : MonoBehaviour {

	// focus is the closest snap point to the current mouse position
	public hexLocus focus { get; private set; }

	// private variables
	private EditGM gmRef;
	private GenesisTile gtRef;
	private hexLocus anchor;
	private Vector3 fShift;


	void Awake ()
	{
		gmRef = EditGM.instance;
		gtRef = gmRef.genesis_tile;

		focus = new hexLocus();
		anchor = new hexLocus();
		fShift = new Vector3();
	}

	void Update ()
	{
		// tile type needs to be known in order to calculate offset
		int tt = gtRef.tileType;
		// tile is the transform of the black (color: 0) tile of type tt
		Transform tile = gtRef.transform.GetChild(tt).GetChild(0);
		// tileOffset is the difference between the sprite's and the prefab's positions
		Vector3 tileOffset = tile.GetChild(0).position - tile.position;
		// lastly we grab the mouse position
		Vector3 mouseIn = Camera.main.ScreenToWorldPoint(Input.mousePosition);

		/* Need to adapt raycast code from findNewAnchor() to replace the line above. */

		// fShift is calculated based on the anchor and the mouse position
		fShift = mouseIn - tileOffset - anchor.toUnitySpace();
		focus = new hexLocus(fShift);
		focus += anchor;

		// the position of the genesis tile is updated
		gtRef.transform.position = focus.toUnitySpace();
	}

	// finds the closest snap point to the current mouse position and sets the anchor there
	public void findNewAnchor ()
	{
		// generates a list of all collisions within a radius 0.5 circle from current mouse position
		hexLocus newAnchor = new hexLocus();
		float depth = gmRef.tile_map.transform.GetChild(gmRef.active_layer).position.z;
		Plane layerPlane = new Plane(Vector3.back, Vector3.forward * depth);
		float distance;
		Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		if (!layerPlane.Raycast(inputRay, out distance)) {
			Debug.LogError("Screen click ray did not intersect with layer plane.");
			return;
		}
		Vector3 planeIntersection = inputRay.GetPoint(distance);

		Collider2D[] hitCols = Physics2D.OverlapCircleAll(planeIntersection, 0.5f, 1);
		List<hexLocus> locusSnaps = new List<hexLocus>();

		// checks every vertex of each PolygonCollider reporting a hit
		foreach (Collider2D c2d in hitCols) {
			PolygonCollider2D pc2d = c2d as PolygonCollider2D;
			if (pc2d) {
				int tLayer;
				tileData tData;
				bool b = gmRef.getDataFromTile(c2d.gameObject, out tData, out tLayer);
				// if the collision is not from a tile in the map, it is skipped
				if (!b) continue;
				hexLocus tHL = tData.locus;
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
			Vector2 newOffset = (Vector2)(hL.toUnitySpace() - planeIntersection);
			Vector2 oldOffset = (Vector2)(newAnchor.toUnitySpace() - planeIntersection);
			if (oldOffset.magnitude > newOffset.magnitude) newAnchor = hL;
		}

		// updates global variables
		anchor = newAnchor;
		Vector3 returnV3 = anchor.toUnitySpace();
		returnV3.z = depth;
		transform.position = returnV3;
	}
}

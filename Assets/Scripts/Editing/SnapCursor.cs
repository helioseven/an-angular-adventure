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
	private Plane layerPlane;
	private float depth;


	void Awake ()
	{
		focus = new hexLocus();
		anchor = new hexLocus();
		fShift = new Vector3();
		layerPlane = new Plane(Vector3.back, 0f);
		depth = 0f;
	}

	void Start ()
	{
		gmRef = EditGM.instance;
		gtRef = gmRef.genesis_tile;
	}

	void Update ()
	{
		int tt = gtRef.tileType;
		Transform tile = gtRef.transform.GetChild(tt).GetChild(0);
		Vector3 tileOffset = tile.GetChild(0).position - tile.position; // <1>

		Vector3 mouseIn = findPointOnPlane();
		fShift = mouseIn - tileOffset - anchor.toUnitySpace(); // <2>
		focus = new hexLocus(fShift);
		focus += anchor; // <3>

		/*
		<1> tileOffset is the difference between the sprite's and the prefab's positions
		<2> fShift is this offset between the anchor point and current mouse position
		<3> focus is the nearest grid point to which the genesis_tile will snap
		*/
	}

	// finds the closest snap point to the current mouse position and sets the anchor there
	public void findNewAnchor ()
	{
		Vector3 mouseIn = findPointOnPlane();
		// generates a list of all collisions within a radius 0.5 circle from current mouse position
		Collider2D[] hitCols = Physics2D.OverlapCircleAll(mouseIn, 0.5f, 1);
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

		hexLocus newAnchor = new hexLocus();
		// finds the hexLocus with the smallest offset from original input position
		foreach (hexLocus hL in locusSnaps) {
			Vector2 newOffset = (Vector2)(hL.toUnitySpace() - mouseIn);
			Vector2 oldOffset = (Vector2)(newAnchor.toUnitySpace() - mouseIn);
			if (oldOffset.magnitude > newOffset.magnitude) newAnchor = hL;
		}

		// updates global variables
		anchor = newAnchor;
		Vector3 returnV3 = anchor.toUnitySpace();
		returnV3.z = depth;
		transform.position = returnV3;
	}

	private Vector2 findPointOnPlane ()
	{
		depth = gmRef.getLayerDepth();
		layerPlane.distance = depth;

		float distance;
		Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		if (!layerPlane.Raycast(inputRay, out distance)) {
			Debug.LogError("Screen click ray did not intersect with layer plane.");
			return new Vector2();
		} else return inputRay.GetPoint(distance);
	}
}
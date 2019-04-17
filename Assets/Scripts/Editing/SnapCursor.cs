using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using circleXsquares;

public class SnapCursor : MonoBehaviour {

	// focus is the snap point (relative to anchor) closest to the current mouse position
	public hexLocus focus { get; private set; }

	// private variables
	private EditGM gmRef;
	private GenesisTile gtRef;
	private hexLocus anchor;
	private Vector2 fShift;
	private Plane layerPlane;
	private float depth;


	void Awake ()
	{
		focus = new hexLocus();
		anchor = new hexLocus();
		fShift = new Vector2();
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
		Vector2 tileOffset = tile.GetChild(0).position - tile.position; // <1>

		Vector2 mouseIn = findPointOnPlane();
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
		Vector2 mouseIn = findPointOnPlane();
		Collider2D[] hitCols = Physics2D.OverlapCircleAll(mouseIn, 0.5f, 1); // <1>
		List<hexLocus> locusSnaps = new List<hexLocus>();

		foreach (Collider2D c2d in hitCols) {
			PolygonCollider2D pc2d = c2d as PolygonCollider2D;
			if (pc2d) { // <2>
				int tLayer;
				tileData tData;
				bool b = gmRef.getDataFromTile(c2d.gameObject, out tData, out tLayer);
				if (!b) continue; // <3>
				hexLocus tHL = tData.locus;
				foreach (Vector2 subPoint in pc2d.points) { // <4>
					Vector2 v2 = c2d.transform.TransformPoint(subPoint); // <5>
					hexLocus newPoint = new hexLocus(v2 - tHL.toUnitySpace());
					newPoint += tHL;
					locusSnaps.Add(newPoint); // <6>
					tHL = newPoint;
				}
			}
		}

		hexLocus newAnchor = new hexLocus();
		foreach (hexLocus hL in locusSnaps) {
			Vector2 newOffset = (Vector2)(hL.toUnitySpace() - mouseIn);
			Vector2 oldOffset = (Vector2)(newAnchor.toUnitySpace() - mouseIn);
			if (oldOffset.magnitude > newOffset.magnitude) newAnchor = hL; // <7>
		}

		anchor = newAnchor; // <8>
		Vector3 returnV3 = anchor.toUnitySpace();
		returnV3.z = depth;
		transform.position = returnV3;

		/*
		<1> generates a list of all collisions within a radius 0.5 circle from current mouse position
		<2> make sure that each collider hit is a tile by confirming it is a PolygonCollider2D
		<3> if the collision is not from a tile in the map, it is skipped
		<4> we now check every vertex of of the collider's polygon
		<5> each vertex is translated from local space into world space
		<6> adds each vertex to the list of possible snap points
		<7> finds the hexLocus with the smallest offset from original input position
		<8> updates global variables
		*/
	}

	// uses mouse position ray's intersection with current level plane to generate a 2D coordinate
	private Vector2 findPointOnPlane ()
	{
		depth = gmRef.getLayerDepth();
		layerPlane.distance = depth; // <1>

		float distance;
		Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		if (!layerPlane.Raycast(inputRay, out distance)) { // <2>
			Debug.LogError("Screen click ray did not intersect with layer plane.");
			return new Vector2();
		} else return inputRay.GetPoint(distance); // <3>

		/*
		<1> get layer depth from GM and set plane's distance from origin accordingly
		<2> if the raycast doesn't hit our plane, something is wrong
		<3> simply return the point along ray at distance from origin
		*/
	}
}
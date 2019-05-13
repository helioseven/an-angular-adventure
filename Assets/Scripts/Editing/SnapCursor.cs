using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using circleXsquares;

public class SnapCursor : MonoBehaviour {

	// public read-accessibility state variables
	public HexLocus focus { get; private set; }
	public HexLocus anchor { get; private set; }

	// private variables
	private EditGM gm_ref;
	private TileCreator tc_ref;
	private Vector2 focus_shift;
	private Plane layer_plane;
	private float depth;


	void Awake ()
	{
		focus = new HexLocus();
		anchor = new HexLocus();
		focus_shift = new Vector2();
		layer_plane = new Plane(Vector3.back, 0f);
		depth = 0f;
	}

	void Start ()
	{
		gm_ref = EditGM.instance;
		tc_ref = gm_ref.tileCreator;
	}

	void Update ()
	{
		Vector2 tileOffset = Vector2.zero;
		if (tc_ref.gameObject.activeSelf) { // <1>
			int tt = tc_ref.tileType;
			Transform tile = tc_ref.transform.GetChild(tt).GetChild(0);
			tileOffset = tile.GetChild(0).position - tile.position; // <2>
		}

		Vector2 mouseIn = findPointOnPlane();
		focus_shift = mouseIn - tileOffset - anchor.ToUnitySpace(); // <3>
		focus = new HexLocus(focus_shift);
		focus += anchor; // <4>

		/*
		<1> if the tileCreator is active, an offset needs to be calculated
		<2> tileOffset is the difference between the sprite's and the prefab's positions
		<3> focus_shift is this offset between the anchor point and current mouse position
		<4> focus is the nearest grid point to which the genesis_tile will snap
		*/
	}

	// finds the closest snap point to the current mouse position and sets the anchor there
	public void FindNewAnchor ()
	{
		Vector2 mouseIn = findPointOnPlane();
		Collider2D[] hitCols = Physics2D.OverlapCircleAll(mouseIn, 0.5f, 1); // <1>
		List<HexLocus> locusSnaps = new List<HexLocus>();

		foreach (Collider2D c2d in hitCols) {
			PolygonCollider2D pc2d = c2d as PolygonCollider2D;
			if (pc2d) { // <2>
				TileData tData;
				bool b = gm_ref.IsMappedTile(c2d.gameObject, out tData);
				if (!b) continue; // <3>
				HexLocus tHL = tData.orient.locus;
				foreach (Vector2 subPoint in pc2d.points) { // <4>
					Vector2 v2 = c2d.transform.TransformPoint(subPoint); // <5>
					HexLocus newPoint = new HexLocus(v2 - tHL.ToUnitySpace());
					newPoint += tHL;
					locusSnaps.Add(newPoint); // <6>
					tHL = newPoint;
				}
			}
		}

		HexLocus newAnchor = new HexLocus();
		foreach (HexLocus hL in locusSnaps) {
			Vector2 newOffset = (Vector2)(hL.ToUnitySpace() - mouseIn);
			Vector2 oldOffset = (Vector2)(newAnchor.ToUnitySpace() - mouseIn);
			if (oldOffset.magnitude > newOffset.magnitude) newAnchor = hL; // <7>
		}

		anchor = newAnchor; // <8>
		Vector3 returnV3 = anchor.ToUnitySpace();
		returnV3.z = depth;
		transform.position = returnV3;

		/*
		<1> generates a list of all collisions within a radius 0.5 circle from current mouse position
		<2> make sure that each collider hit is a tile by confirming it is a PolygonCollider2D
		<3> if the collision is not from a tile in the map, it is skipped
		<4> we now check every vertex of of the collider's polygon
		<5> each vertex is translated from local space into world space
		<6> adds each translated vertex to the list of possible snap points
		<7> finds the HexLocus with the smallest offset from original input position
		<8> updates global variables
		*/
	}

	/* Private Functions */

	// uses mouse position ray's intersection with current level plane to generate a 2D coordinate
	private Vector2 findPointOnPlane ()
	{
		depth = gm_ref.GetLayerDepth();
		layer_plane.distance = depth; // <1>

		float distance;
		Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		if (!layer_plane.Raycast(inputRay, out distance)) { // <2>
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
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using circleXsquares;

public class BehaviourScript : MonoBehaviour {

	public GameObject cursor;
	public GameObject diamond;
	public GameObject hexagon;
	public GameObject trapezoid;
	public GameObject triangle;
	public GameObject square;
	public GameObject wedge;

	private GameObject anchorIcon;
	private GameObject[] tRefs;
	// current keeps track of which tile is being used
	private GameObject current;
	// focus and anchor keep track of where the mouse input is snapping to
	private hexLocus focus;
	private hexLocus anchor;
	private Vector3 fShift;

	// camInput helps manage the camera dolly
	private enum camInput : byte {Up, Down, Left, Right};
	// tileTypes helps manage the active tile
	private enum tileTypes : byte {Tri, Dia, Trap, Hex, Sqr, Wed};

	private Dictionary<GameObject, hexLocus> placedTiles;

	void Start () {
		anchorIcon = Instantiate(cursor, Vector3.zero, Quaternion.identity) as GameObject;
		tRefs = new GameObject[] {triangle, diamond, trapezoid, hexagon, square, wedge};
		current = Instantiate(tRefs[0]) as GameObject;

		focus = new hexLocus();
		anchor = new hexLocus();
		fShift = new Vector3();

		placedTiles = new Dictionary<GameObject, hexLocus>();
	}

	void Update () {
		// focus is updated first from the anchor and the mouse input
		fShift = Camera.main.ScreenToWorldPoint(Input.mousePosition) - anchor.toUnitySpace();
		focus = new hexLocus(fShift);
		focus += anchor;
		// the current tile is moved accordingly
		current.transform.position = focus.toUnitySpace();

		// inputs are handled as follows:
		// BackQuote is a special debugger key
		if (Input.GetKeyDown(KeyCode.BackQuote)) {
			Debug.Log(anchor.iA);
			Debug.Log(anchor.iB);
			Debug.Log(anchor.iC);
			Debug.Log(anchor.iD);
			Debug.Log(anchor.iE);
			Debug.Log(anchor.iF);
		}
		// 123456 manages tile type
		if (Input.GetKeyDown(KeyCode.Alpha1)) switchTile(tileTypes.Tri);
		if (Input.GetKeyDown(KeyCode.Alpha2)) switchTile(tileTypes.Dia);
		if (Input.GetKeyDown(KeyCode.Alpha3)) switchTile(tileTypes.Trap);
		if (Input.GetKeyDown(KeyCode.Alpha4)) switchTile(tileTypes.Hex);
		if (Input.GetKeyDown(KeyCode.Alpha5)) switchTile(tileTypes.Sqr);
		if (Input.GetKeyDown(KeyCode.Alpha6)) switchTile(tileTypes.Wed);
		// WSAD manages camera pan
		if (Input.GetKey(KeyCode.W)) shiftCam(camInput.Up);
		if (Input.GetKey(KeyCode.A)) shiftCam(camInput.Left);
		if (Input.GetKey(KeyCode.S)) shiftCam(camInput.Down);
		if (Input.GetKey(KeyCode.D)) shiftCam(camInput.Right);
		// Q&E manage current tile rotation
		if (Input.GetKeyDown(KeyCode.Q)) current.transform.Rotate(new Vector3(0, 0, 30));
		if (Input.GetKeyDown(KeyCode.E)) current.transform.Rotate(new Vector3(0, 0, -30));
		// Spacebar handles the setting of the anchor point
		if (Input.GetKeyDown(KeyCode.Space)) findAnchor();
		// Mouse click handles tile stamping
		if (Input.GetKeyDown(KeyCode.Mouse0)) stampTile();
	}

	// 
	private void findAnchor () {
		// 
		hexLocus newAnchor = new hexLocus();
		Vector2 inputPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Collider2D[] hitCols = Physics2D.OverlapCircleAll(inputPos, 0.5f);
		List<hexLocus> locusSnaps = new List<hexLocus>();

		// 
		foreach (Collider2D c2d in hitCols) {
			if (c2d is PolygonCollider2D) {
				// 
				if (!placedTiles.ContainsKey(c2d.gameObject)) continue;
				hexLocus tHL = placedTiles[c2d.gameObject];
				foreach (Vector2 subPoint in ((PolygonCollider2D)c2d).points) {
					// 
					hexLocus newPoint = new hexLocus(c2d.transform.TransformPoint(subPoint) - tHL.toUnitySpace());
					newPoint += tHL;
					locusSnaps.Add(newPoint);
					tHL = newPoint;
				}
			}
		}

		// 
		foreach (hexLocus hL in locusSnaps) {
			Vector2 newOffset = (Vector2)hL.toUnitySpace() - inputPos;
			Vector2 oldOffset = (Vector2)newAnchor.toUnitySpace() - inputPos;
			if (oldOffset.magnitude > newOffset.magnitude) newAnchor = hL;
		}

		// 
		anchor = newAnchor;
		anchorIcon.transform.position = anchor.toUnitySpace();
	}

	private void shiftCam (camInput inCam) {
		Vector3 tempVec3 = transform.position;

		// updates the camera dolly's position based on the current inputs
		switch (inCam) {
			case camInput.Up: {
				tempVec3.y += (5.0f * Time.deltaTime);
				break;
			}
			case camInput.Left: {
				tempVec3.x -= (5.0f * Time.deltaTime);
				break;
			}
			case camInput.Down: {
				tempVec3.y -= (5.0f * Time.deltaTime);
				break;
			}
			case camInput.Right: {
				tempVec3.x += (5.0f * Time.deltaTime);
				break;
			}
		}

		transform.position = tempVec3;
 	}

	private void stampTile () {
		// method is simple for now, no collision detection implementation yet
		placedTiles.Add(Instantiate(current, current.transform.position, current.transform.rotation) as GameObject, focus);
	}

	private void switchTile (tileTypes tType) {
		// method switches active tile by destroying and instantiating
		GameObject tempGO = tRefs[(int)tType];
		Vector3 tempVec3 = current.transform.position;
		Quaternion tempQuat = current.transform.rotation;

		Destroy(current);
		current = Instantiate(tempGO, tempVec3, tempQuat) as GameObject;
	}
}

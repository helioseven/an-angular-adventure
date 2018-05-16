using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using circleXsquares;

public class EditGM : MonoBehaviour {

	// singleton instance
	[HideInInspector]
	public static EditGM instance = null;
	public bool isUpdating = true;
	private EditLoader lvlLoad = null;
	// placedTiles manages all tiles placed into the world space
	private Dictionary<GameObject, hexLocus> placedTiles;

	// cursor is a prefab anchor icon
	public GameObject cursor;
	public GameObject uiPanel;
	public Button editButton;

	// reference to the script attached to the UI overlay
	private Editor_UI_Script uiOverlay;
	// anchorIcon keeps track of a single cursor instance
	private GameObject anchorIcon;
	// tRefs collects the tile prefab references into an array
	private GameObject[] tRefs;
	// tileTypes helps manage the active tile
	private enum tileTypes : byte {Tri, Dia, Trap, Hex, Sqr, Wed};

	// menuMode enables OnGUI
	private bool menuMode;
	// editMode enables editing of previously placed tiles
	// editMode being false is referred to in commenting as "creation mode"
	private bool editMode;
	// bEdit tracks a reference to the edit mode button in the menu panel
	private Button bEdit;
	// gkInputs and gkdInputs track GetKey() and GetKeyDown() states
	private inputKeys gkInputs = inputKeys.None;
	private inputKeys gkdInputs = inputKeys.None;

	// createTiles manages a current tile for each tile type
	private GameObject[] createTiles;
	// current keeps track of the tile that is in use at a given time
	private GameObject current;
	// eCurrent keeps track of currently selected tile in edit mode
	private GameObject eCurrent;
	// focus and anchor keep track of where the mouse input is snapping to
	private hexLocus focus;
	private hexLocus anchor;
	// fShift updates every frame with the distance between the mouse and the anchor
	private Vector3 fShift;

	// inputKeys helps manage keyboard input
	[Flags]
	public enum inputKeys {
		None = 0x0,
		Space = 0x1,
		Tab = 0x2,
		Delete = 0x4,
		Click0 = 0x8,
		Click1 = 0x10,
		Q = 0x20,
		W = 0x40,
		E = 0x80,
		A = 0x100,
		S = 0x200,
		D = 0x400,
		One = 0x800,
		Two = 0x1000,
		Three = 0x2000,
		Four = 0x4000,
		Five = 0x8000,
		Six = 0x10000
	}
	// keyCodeList is mapped onto inputKeys in order
	private KeyCode[] keyCodeList = new KeyCode[] {
		KeyCode.None,
		KeyCode.Space,
		KeyCode.Tab,
		KeyCode.Delete,
		KeyCode.Mouse0,
		KeyCode.Mouse1,
		KeyCode.Q,
		KeyCode.W,
		KeyCode.E,
		KeyCode.A,
		KeyCode.S,
		KeyCode.D,
		KeyCode.Alpha1,
		KeyCode.Alpha2,
		KeyCode.Alpha3,
		KeyCode.Alpha4,
		KeyCode.Alpha5,
		KeyCode.Alpha6
	};

	void Awake ()
	{
		if (!instance) {
			// set singleton instance
			instance = this;

			// level is loaded from file
			lvlLoad = GameObject.FindWithTag("Loader").GetComponent<EditLoader>();
			lvlLoad.supplyLevel(out placedTiles);

			// initializations for tile references
			tRefs = new GameObject[] {
				lvlLoad.triTile,
				lvlLoad.diaTile,
				lvlLoad.trapTile,
				lvlLoad.hexTile,
				lvlLoad.sqrTile,
				lvlLoad.wedTile
			};
			createTiles = new GameObject[tRefs.Length];
			for (int i = 0; i < tRefs.Length; i++) {
				createTiles[i] = Instantiate(tRefs[i], Vector3.zero, Quaternion.identity) as GameObject;
				createTiles[i].SetActive(false);
			}
			current = createTiles[0];
			current.SetActive(true);
			eCurrent = null;

			// set up UI elements
			uiOverlay = uiPanel.GetComponent<Editor_UI_Script>();
			anchorIcon = Instantiate(cursor, Vector3.zero, Quaternion.identity) as GameObject;

			// initializations for button listeners
			bEdit = editButton;

			// initializations for state variables
			menuMode = false;
			editMode = false;
			gkInputs = inputKeys.None;
			gkdInputs = inputKeys.None;
			focus = new hexLocus();
			anchor = new hexLocus();
			fShift = new Vector3();
		} else
			// only one singleton can exist
			Destroy(gameObject);
	}

	void Update ()
	{
		// (??)
		if (!isUpdating) return;

		// focus is updated first based on the anchor and the mouse position
		fShift = Camera.main.ScreenToWorldPoint(Input.mousePosition) - anchor.toUnitySpace();
		focus = new hexLocus(fShift);
		focus += anchor;

		// gkInputs and gkdInputs are reset and updated
		updateInputs();

		// BackQuote is a special debugger key, prints anchor's (A,B,C,D,E,F) coordinates
		if (Input.GetKeyDown(KeyCode.BackQuote)) {
			Debug.Log(anchor.iA);
			Debug.Log(anchor.iB);
			Debug.Log(anchor.iC);
			Debug.Log(anchor.iD);
			Debug.Log(anchor.iE);
			Debug.Log(anchor.iF);
		}

		// anchor is updated based on right-click input
		if ((gkdInputs & inputKeys.Click1) == inputKeys.Click1) findAnchor();

		// menuMode is enabled whenever the space bar is held down
		menuMode = (gkInputs & inputKeys.Space) == inputKeys.Space;

		if (menuMode) {
			// uiOverlay is enabled and current tile disabled during menu mode
			uiOverlay.activate(Input.mousePosition);
			current.SetActive(false);
		} else {
			// uiOverlay is disabled and current tile enabled when not in menu mode
			uiOverlay.deactivate();
			current.SetActive(editMode ? false : true);

			// editMode is set via the toggleEdit() listener on the bEdit button
			if (editMode) {
				if (eCurrent) {
					Genesis_Tile ts = eCurrent.GetComponent<Genesis_Tile>();

					// in edit mode, a selected tile will follow the focus
					eCurrent.transform.position = focus.toUnitySpace();
					// if there is a selected tile, Tab changes its color and left-click places it
					if ((gkdInputs & inputKeys.Tab) == inputKeys.Tab) ts.cycleColor();
					if ((gkdInputs & inputKeys.Click0) == inputKeys.Click0) {
						placedTiles.Add(eCurrent, focus);
						eCurrent = null;
					}
					// Q & E will change the selected tile's rotation
					if ((gkdInputs & inputKeys.Q) == inputKeys.Q) ts.rotate(false);
					if ((gkdInputs & inputKeys.E) == inputKeys.E) ts.rotate(true);
					// Delete will destroy the selected tile
					if ((gkdInputs & inputKeys.Delete) == inputKeys.Delete) Destroy(eCurrent);
				} else {
					// if there is not a selected tile in edit mode, left-click selects a placed tile
					if ((gkdInputs & inputKeys.Click0) == inputKeys.Click0) {
						Collider2D c2d = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(Input.mousePosition)).collider;
						if (!c2d) return;
						eCurrent = c2d.gameObject;
						placedTiles.Remove(eCurrent);
					}
				}
			} else {
				Genesis_Tile ts = current.GetComponent<Genesis_Tile>();

				// in creation mode, a member of createTiles is displayed via current
				current.transform.position = focus.toUnitySpace();
				// current tile is placed when left-click is made
				if ((gkdInputs & inputKeys.Click0) == inputKeys.Click0) placeTile();
				// current tile's color is updated when Tab is pressed
				if ((gkdInputs & inputKeys.Tab) == inputKeys.Tab) ts.cycleColor();
				// current is rotated counter-clockwise when Q is pressed, clockwise when E is pressed
				if ((gkdInputs & inputKeys.Q) == inputKeys.Q) ts.rotate(false);
				if ((gkdInputs & inputKeys.E) == inputKeys.E) ts.rotate(true);
				// current tile type is assigned by the numeric keys
				if ((gkdInputs & inputKeys.One) == inputKeys.One) switchTile(tileTypes.Tri);
				if ((gkdInputs & inputKeys.Two) == inputKeys.Two) switchTile(tileTypes.Dia);
				if ((gkdInputs & inputKeys.Three) == inputKeys.Three) switchTile(tileTypes.Trap);
				if ((gkdInputs & inputKeys.Four) == inputKeys.Four) switchTile(tileTypes.Hex);
				if ((gkdInputs & inputKeys.Five) == inputKeys.Five) switchTile(tileTypes.Sqr);
				if ((gkdInputs & inputKeys.Six) == inputKeys.Six) switchTile(tileTypes.Wed);
			}
		}
	}

	/* Public Functions */

	// deletes the current scene and loads the MainMenu scene
	public void returnToMainMenu ()
	{
		// (!!) prompt if unsaved
		SceneManager.LoadScene(0);
	}

	// returns getKeys or getKeyDowns depending on boolean parameter
	public inputKeys getInputs (bool onlyNewInputs)
	{
		// onlyNew determines the response of the function
		if (onlyNewInputs) return gkdInputs;
		else return gkInputs;
	}

	// toggles editMode and makes associated changes to current
	public void toggleEdit ()
	{
		// bEdit button is 75% grey in creation mode, 100% white in edit mode
		bEdit.image.color = editMode ? new Color(0.75f, 0.75f, 0.75f, 1.0f) : new Color(1.0f, 1.0f, 1.0f, 1.0f);
		// current is enabled in creation mode, disabled in edit mode
		current.SetActive(editMode ? true : false);
		// either way, editMode is toggled
		editMode = !editMode;
	}

	// (testing) save level to a file in plain text format
	public void saveFile (string filename)
	{
		string fpath = "Assets\\Levels\\" + filename + ".txt";
		GameObject[] pts = new GameObject[placedTiles.Count];
		string[] lines = new string[pts.Length + 2];

		placedTiles.Keys.CopyTo(pts, 0);
		lines[0] = "test level";
		lines[1] = "0 0 0 0 0 -10";

		for (int i = 0; i < pts.Length; i++)
			lines[i+2] = pts[i].GetComponent<Genesis_Tile>().serialize(placedTiles[pts[i]]);

		File.WriteAllLines(fpath, lines);
	}

	/* Private Functions */

	// updates gkInputs and gkdInputs each frame
	private void updateInputs ()
	{
		gkInputs = inputKeys.None;
		gkdInputs = inputKeys.None;

		// assigns enum flags by powers of 2
		for (int k = 0, i = 0; i < 0x8001; i = (i == 0) ? 1 : i * 2) {
			KeyCode kc = keyCodeList[k++];
			if (Input.GetKey(kc)) gkInputs = gkInputs | (inputKeys) i;
			if (Input.GetKeyDown(kc)) gkdInputs = gkdInputs | (inputKeys) i;
		}
	}

	// finds the closest snap point to the current mouse position and sets the anchor there
	private void findAnchor ()
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
				hexLocus tHL = placedTiles[c2d.gameObject];
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
		anchorIcon.transform.position = anchor.toUnitySpace();
	}

 	// places the current tile at the focus location
	private void placeTile ()
	{
		// method is simple for now, no collision detection implementation yet
		GameObject go = Instantiate(current, current.transform.position, Quaternion.identity) as GameObject;
		Genesis_Tile pet = go.GetComponent<Genesis_Tile>();
		Genesis_Tile cet = current.GetComponent<Genesis_Tile>();

		// cycles the color of the placed tile to match current
		for (int i = cet.tileColor; i > 0; i--) pet.cycleColor();
		// cycles the rotation of the placed tile to match current
		for (int i = cet.tileRotation; i > 0; i--) pet.rotate(false);

		// placedTiles is updated accordingly
		placedTiles.Add(go, focus);
	}

	// replaces the current tile with the indicated tile type
	private void switchTile (tileTypes tType)
	{
		// method switches active tile by deactivating current and activating createTiles[tType]
		GameObject go = createTiles[(int)tType];
		Vector3 tempVec3 = current.transform.position;
		Genesis_Tile et = current.GetComponent<Genesis_Tile>();
		int tRotation = et.tileRotation;
		int tColor = et.tileColor;

		// sets all values for the previously current tile to a state of inactivity
		current.transform.position = Vector3.zero;
		current.transform.rotation = Quaternion.identity;
		while (et.tileColor != 0) et.cycleColor();
		current.SetActive(false);

		et = go.GetComponent<Genesis_Tile>();
		// sets all values for the newly current tile to match state of previously current tile
		go.transform.position = tempVec3;
		// SOMETHING NOT RIGHT (??)
		while (tRotation > 0) {
			et.rotate(false);
			tRotation--;
		}
		while (tColor > 0) {
			go.GetComponent<Genesis_Tile>().cycleColor();
			tColor--;
		}

		// updates global variables accordingly
		current = go;
		current.SetActive(true);
	}
}
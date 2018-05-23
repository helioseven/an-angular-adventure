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
	public bool isUpdating { get; private set; }
	private EditLoader lvlLoad = null;
	// placedTiles manages all tiles placed into the world space
	private Dictionary<GameObject, tileData> placedTiles;

	// cursor is a prefab anchor icon
	public GameObject cursor;
	// reference to the main creation tool
	public GenesisTile genesisTile;
	// reference to the script attached to the UI overlay
	public Editor_UI_Script menuPanel;

	// anchorIcon keeps track of a single cursor instance
	private GameObject anchorIcon;
	// tileTypes helps manage the active tile
	private enum tileTypes : byte {Tri, Dia, Trap, Hex, Sqr, Wed};

	// menuMode enables OnGUI
	public bool menuMode { get; private set; }
	// editMode enables editing of previously placed tiles
	// editMode being false is referred to in commenting as "creation mode"
	public bool editMode { get; private set; }
	// gkInputs and gkdInputs track GetKey() and GetKeyDown() states
	private inputKeys gkInputs = inputKeys.None;
	private inputKeys gkdInputs = inputKeys.None;

	// isTileSelected and selectedTile track any selected tile in edit mode
	private bool isTileSelected;
	private tileData selectedTile;
	private tileData gtBackup;

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
			placedTiles = new Dictionary<GameObject, tileData>();
			lvlLoad = GameObject.FindWithTag("Loader").GetComponent<EditLoader>();
			lvlLoad.supplyLevel(out placedTiles);

			// sets up the anchor point
			anchorIcon = Instantiate(cursor, Vector3.zero, Quaternion.identity) as GameObject;

			// initializations for selection variables
			isTileSelected = false;
			selectedTile = new tileData();
			gtBackup = new tileData();

			// initializations for state variables
			isUpdating = true;
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

		// anchor is updated based on right-click input
		if ((gkdInputs & inputKeys.Click1) == inputKeys.Click1) findAnchor();

		// menuMode is enabled whenever the space bar is held down
		menuMode = (gkInputs & inputKeys.Space) == inputKeys.Space;

		if (menuMode) {
			// menuPanel is enabled and the genesisTile is disabled during menu mode
			menuPanel.activate(Input.mousePosition);
			genesisTile.gameObject.SetActive(false);
		} else {
			// menuPanel is disabled and the genesisTile is enabled when not in menu mode
			menuPanel.deactivate();
			genesisTile.gameObject.SetActive(editMode ? false : true);

			// editMode is set via the toggleEdit() function
			if (editMode) {
				if (isTileSelected) {
					// in edit mode, a selected tile will follow the focus
					genesisTile.transform.position = focus.toUnitySpace();
					// if there is a selected tile, left-click replaces it
					if ((gkdInputs & inputKeys.Click0) == inputKeys.Click0) {
						placeTile();
						genesisTile.setProperties(gtBackup);
						isTileSelected = false;
						return;
					}
					// Tab will cycle the selected tile through different colors
					if ((gkdInputs & inputKeys.Tab) == inputKeys.Tab)
						genesisTile.cycleColor();
					// Q & E will change the selected tile's rotation
					if ((gkdInputs & inputKeys.Q) == inputKeys.Q)
						genesisTile.rotate(false);
					if ((gkdInputs & inputKeys.E) == inputKeys.E)
						genesisTile.rotate(true);
					// Delete will destroy the selected tile
					if ((gkdInputs & inputKeys.Delete) == inputKeys.Delete) {
						genesisTile.setProperties(gtBackup);
						isTileSelected = false;
					}
				} else {
					// if there is not a selected tile in edit mode, left-click selects a placed tile
					if ((gkdInputs & inputKeys.Click0) == inputKeys.Click0) {
						Collider2D c2d = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(Input.mousePosition)).collider;
						if (!c2d) return;
						GameObject go = c2d.gameObject;

						gtBackup = getGTData();
						selectedTile = placedTiles[go];
						genesisTile.setProperties(selectedTile);
						isTileSelected = true;

						placedTiles.Remove(go);
						Destroy(go);
					}
				}
			} else {
				// in creation mode, the genesisTile is moved to the current focus
				genesisTile.transform.position = focus.toUnitySpace();

				// the genesisTile tile is placed when left-click is made
				if ((gkdInputs & inputKeys.Click0) == inputKeys.Click0)
					placeTile();

				// current tile's color is updated when Tab is pressed
				if ((gkdInputs & inputKeys.Tab) == inputKeys.Tab)
					genesisTile.cycleColor();

				// current is rotated counter-clockwise when Q is pressed, clockwise when E is pressed
				if ((gkdInputs & inputKeys.Q) == inputKeys.Q)
					genesisTile.rotate(false);
				if ((gkdInputs & inputKeys.E) == inputKeys.E)
					genesisTile.rotate(true);

				// current tile type is assigned by the numeric keys
				if ((gkdInputs & inputKeys.One) == inputKeys.One)
					genesisTile.selectType(0);
				if ((gkdInputs & inputKeys.Two) == inputKeys.Two)
					genesisTile.selectType(1);
				if ((gkdInputs & inputKeys.Three) == inputKeys.Three)
					genesisTile.selectType(2);
				if ((gkdInputs & inputKeys.Four) == inputKeys.Four)
					genesisTile.selectType(3);
				if ((gkdInputs & inputKeys.Five) == inputKeys.Five)
					genesisTile.selectType(4);
				if ((gkdInputs & inputKeys.Six) == inputKeys.Six)
					genesisTile.selectType(5);
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
		if (editMode) {
			// (??)
			genesisTile.setProperties(gtBackup);
			gtBackup = (isTileSelected) ? selectedTile : new tileData();
			genesisTile.gameObject.SetActive(true);
		} else {
			// (??)
			if (isTileSelected) selectedTile = gtBackup;
			gtBackup = getGTData();
			genesisTile.setProperties(selectedTile);
			genesisTile.gameObject.SetActive(false);
		}
		
		// either way, editMode is toggled
		editMode = !editMode;
	}

	// (??)
	public void pauseToggle ()
	{
		isUpdating = !isUpdating;
	}

	// (testing) save level to a file in plain text format
	public void saveFile (string filename)
	{
		string fpath = "Assets\\Levels\\" + filename + ".txt";
		GameObject[] pts = new GameObject[placedTiles.Count];
		string[] lines = new string[pts.Length + 2];

		placedTiles.Keys.CopyTo(pts, 0);
		// (!!) this bit will have to change
		lines[0] = "test level";
		lines[1] = "0 0 0 0 0 -10";

		for (int i = 0; i < pts.Length; i++)
			lines[i+2] = placedTiles[pts[i]].serialize();

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
		anchorIcon.transform.position = anchor.toUnitySpace();
	}

	// returns a tileData representation of the current state of genesisTile
	private tileData getGTData ()
	{
		GenesisTile gt = genesisTile;
		return new tileData(focus, gt.tileRotation, gt.tileType, gt.tileColor);
	}

 	// places the current tile at the focus location
	private void placeTile ()
	{
		placedTiles.Add(genesisTile.getActiveTile(), getGTData());
	}

/* (pretty sure this can now be deleted)

	// replaces the current tile with the indicated tile type
	private void switchTile (tileTypes tType)
	{
		// method switches active tile by deactivating current and activating createTiles[tType]
		GameObject go = createTiles[(int)tType];
		Vector3 tempVec3 = current.transform.position;
		GenesisTile et = current.GetComponent<GenesisTile>();
		int tRotation = et.tileRotation;
		int tColor = et.tileColor;

		// sets all values for the previously current tile to a state of inactivity
		current.transform.position = Vector3.zero;
		current.transform.rotation = Quaternion.identity;
		while (et.tileColor != 0) et.cycleColor();
		current.SetActive(false);

		et = go.GetComponent<GenesisTile>();
		// sets all values for the newly current tile to match state of previously current tile
		go.transform.position = tempVec3;
		// SOMETHING NOT RIGHT (??)
		while (tRotation > 0) {
			et.rotate(false);
			tRotation--;
		}
		while (tColor > 0) {
			go.GetComponent<GenesisTile>().cycleColor();
			tColor--;
		}

		// updates global variables accordingly
		current = go;
		current.SetActive(true);
	}
*/

}
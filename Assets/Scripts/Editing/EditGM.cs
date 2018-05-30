﻿using System;
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
	// anchorIcon keeps track of a single cursor instance
	public SnapCursor anchorIcon;
	// reference to the script attached to the UI overlay
	public MenuControl menuPanel;

	// menuMode enables OnGUI
	public bool menuMode { get; private set; }
	// editMode enables editing of previously placed tiles
	// editMode being false is referred to in commenting as "creation mode"
	public bool editMode { get; private set; }
	// gkInputs and gkdInputs track GetKey() and GetKeyDown() states
	private inputKeys gkInputs;
	private inputKeys gkdInputs;

	// focus is the closest snap point to the current mouse position
	// the variable is simply updated from anchorIcon, which keeps track of it
	private hexLocus focus;
	// isTileSelected and selectedTile track any selected tile in edit mode
	private bool isTileSelected;
	private tileData selectedTile;
	private tileData gtBackup;

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
		} else
			// only one singleton can exist
			Destroy(gameObject);
	}

	void Update ()
	{
		// escape hatch used when normal behavior is paused by pauseToggle()
		if (!isUpdating) return;

		// gkInputs and gkdInputs are reset and updated
		updateInputs();

		// update the focus from anchorIcon
		focus = anchorIcon.focus;
		// anchor is updated based on right-click input
		if (checkKey(inputKeys.Click1, true)) anchorIcon.findNewAnchor(placedTiles);

		// menuMode is enabled whenever the space bar is held down
		menuMode = checkKey(inputKeys.Space, false);

		if (menuMode) {
			// menuPanel is enabled and the genesisTile is disabled during menu mode
			menuPanel.activate(Input.mousePosition);
			genesisTile.gameObject.SetActive(false);
		} else {
			// menuPanel is disabled and the genesisTile is enabled when not in menu mode
			menuPanel.deactivate();
			genesisTile.gameObject.SetActive((editMode && !isTileSelected) ? false : true);

			// if genesisTile is active, check various inputs for state changes
			if (genesisTile.gameObject.activeSelf) updateGT();

			// editMode is set via the toggleEdit() function
			if (editMode) updateEditMode();
			else {
				// in creation mode, the genesisTile is moved to the current focus
				genesisTile.transform.position = focus.toUnitySpace();

				// the genesisTile tile is placed when left-click is made
				if (checkKey(inputKeys.Click0, true))
					placedTiles.Add(genesisTile.getActiveTile(), getGTData());
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
			// if we're in editMode, restore the genesisTile to previous properties
			genesisTile.setProperties(gtBackup);
			// if there's a tile selected, it's properties are stored in gtBackup
			gtBackup = (isTileSelected) ? selectedTile : new tileData();
			genesisTile.gameObject.SetActive(true);
		} else {
			tileData td = gtBackup;
			// if we're not in editMode, current state of genesisTile is stored
			gtBackup = getGTData();

			// if there's a tile selected, it's properties are restored from gtBackup
			if (isTileSelected) {
				selectedTile = td;
				genesisTile.setProperties(selectedTile);
				genesisTile.gameObject.SetActive(true);
			}
			else genesisTile.gameObject.SetActive(false);
		}
		
		// either way, editMode is toggled
		editMode = !editMode;
	}

	// pausing is used by menus that want to suspend normal behavior
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

	// makes changes associated with the state of the genesisTile
	private void updateGT ()
	{
		// genesisTile's color is cycled through when Tab is pressed
		if (checkKey(inputKeys.Tab, true)) genesisTile.cycleColor();
		// genesisTile is rotated around its pivot,
		// counter-clockwise when Q is pressed, clockwise when E is pressed
		if (checkKey(inputKeys.Q, true)) genesisTile.rotate(false);
		if (checkKey(inputKeys.E, true)) genesisTile.rotate(true);
		// genesisTile's type is assigned by the numeric keys
		if (checkKey(inputKeys.One, true)) genesisTile.selectType(0);
		if (checkKey(inputKeys.Two, true)) genesisTile.selectType(1);
		if (checkKey(inputKeys.Three, true)) genesisTile.selectType(2);
		if (checkKey(inputKeys.Four, true)) genesisTile.selectType(3);
		if (checkKey(inputKeys.Five, true)) genesisTile.selectType(4);
		if (checkKey(inputKeys.Six, true)) genesisTile.selectType(5);
	}

	// makes changes associated with being in editMode
	private void updateEditMode ()
	{
		if (isTileSelected) {
			// in edit mode, a selected tile will follow the focus
			genesisTile.transform.position = focus.toUnitySpace();

			// if there is a selected tile, left-click re-places it
			if (checkKey(inputKeys.Click0, true)) {
				placedTiles.Add(genesisTile.getActiveTile(), getGTData());
				// restore genesisTile to its backup
				genesisTile.setProperties(gtBackup);
				genesisTile.gameObject.SetActive(false);
				// deactivate isTileSelected flag
				isTileSelected = false;
				return;
			}

			// Delete will destroy the selected tile by simply forgetting about it
			if (checkKey(inputKeys.Delete, true)) {
				genesisTile.setProperties(gtBackup);
				isTileSelected = false;
			}
		} else {
			// if in editMode and no tile is selected, left-click selects a tile
			if (checkKey(inputKeys.Click0, true)) {
				// first we find out what (if anything) has been clicked on
				Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
				Collider2D c2d = Physics2D.GetRayIntersection(r).collider;
				if (!c2d) return;
				GameObject go = c2d.gameObject;

				// back up the genesisTile data for creation mode
				gtBackup = getGTData();
				// grab the tile data in question from the dictionary
				selectedTile = placedTiles[go];
				// set the genesisTile up to act like the selected tile
				genesisTile.gameObject.SetActive(true);
				genesisTile.setProperties(selectedTile);

				// activate isTileSelected flag
				isTileSelected = true;
				// remove the dictionary entry and then destroys the object
				placedTiles.Remove(go);
				Destroy(go);
			}
		}
	}

	// updates gkInputs and gkdInputs each frame
	private void updateInputs ()
	{
		gkInputs = inputKeys.None;
		gkdInputs = inputKeys.None;

		// assigns enum flags by powers of 2
		for (int k = 0, i = 0; i < 0x10001; i = (i == 0) ? 1 : i * 2) {
			KeyCode kc = keyCodeList[k++];
			if (Input.GetKey(kc)) gkInputs = gkInputs | (inputKeys) i;
			if (Input.GetKeyDown(kc)) gkdInputs = gkdInputs | (inputKeys) i;
		}
	}

	// returns a bool based on whether the given key is currently pressed
	// if checkKeyDown is passed true, function will only return true for new presses
	private bool checkKey (inputKeys inKey, bool checkKeyDown)
	{
		inputKeys ik = checkKeyDown ? gkdInputs : gkInputs;
		return (ik & inKey) == inKey;
	}

	// returns a tileData representation of the current state of genesisTile
	private tileData getGTData ()
	{
		GenesisTile gt = genesisTile;
		return new tileData(focus, gt.tileRotation, gt.tileType, gt.tileColor);
	}
}
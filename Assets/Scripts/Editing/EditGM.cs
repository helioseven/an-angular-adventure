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
	[HideInInspector] public static EditGM instance = null;

	// references to the loader, UI overlay, tile hierarchy, creation tool, and snap cursor
	private EditLoader lvl_load = null;
	public HUDControl hudPanel;
	public LevelInfoControl infoPanel;
	public PaletteControl palettePanel;
	public GenesisTile genesisTile;
	public SnapCursor anchorIcon;
	public GameObject tileMap;

	// public read-accessibility state variables
	public LevelData levelData { get; private set; }
	public bool menuMode { get; private set; }
	public bool editMode { get; private set; } // <*>
	public InputKeys getKeys { get; private set; }
	public InputKeys getKeyDowns { get; private set; }
	// <*> editMode being false may be referred to in commenting as "creation mode"

	// private variables
	private bool is_tile_selected;
	private TileData selected_tile;
	private bool edit_flag;
	private TileData gt_backup;
	private Dictionary<GameObject, TileData> data_lookup;


	// InputKeys wraps keyboard input into a bit-flag enum
	[Flags]
	public enum InputKeys {
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
		Z = 0x800,
		X = 0x1000,
		C = 0x2000,
		V = 0x4000,
		One = 0x8000,
		Two = 0x10000,
		Three = 0x20000,
		Four = 0x40000,
		Five = 0x80000,
		Six = 0x100000
	}
	// key_code_list is an index mapping between Unity KeyCode and InputKeys
	private KeyCode[] key_code_list = new KeyCode[] {
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
		KeyCode.Z,
		KeyCode.X,
		KeyCode.C,
		KeyCode.V,
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
			instance = this; // <1>

			lvl_load = GameObject.FindWithTag("Loader").GetComponent<EditLoader>();
			LevelData inLevel;
			data_lookup = lvl_load.supplyLevel(ref tileMap, out inLevel); // <2>
			levelData = inLevel;
			activateLayer(0); // <3>

			is_tile_selected = false; // <4>
			selected_tile = new TileData();
			edit_flag = false;
			gt_backup = new TileData();

			menuMode = false; // <5>
			editMode = false;
			getKeys = InputKeys.None;
			getKeyDowns = InputKeys.None;
		} else
			Destroy(gameObject); // <6>

		/*
		<1> set singleton instance
		<2> file is loaded and parsed
		<3> layer opacity is set
		<4> initializations for private variables
		<5> initializations for state variables
		<6> only one singleton can exist
		*/
	}

	void Update ()
	{
		updateInputs(); // <1>
		updateUI(); // <2>

		if (!menuMode) {
			updateWorld(); // <3>

			if (editMode) {
				if (edit_flag) genesisTile.Deactivate(); // <4>

				updateEditMode();
			} else {
				if (edit_flag) genesisTile.Activate(); // <5>

				updateGT();
			}
		}

		edit_flag = false; // <6>

		/*
		<1> getKeys and getKeyDowns are updated
		<2> hudPanel and palettePanel are updated
		<3> anchorIcon and infoPanel are updated
		<4> deactivate tool when entering editMode
		<5> activate tool when exiting editMode
		<6> reset flag every frame, only set by ToggleEdit()
		*/
	}

	/* Public Functions */

	// simply returns whether the given keys were being held during this frame
	public bool CheckKeys (InputKeys inKeys)
	{ return (getKeys & inKeys) == inKeys; }

	// simply returns whether the given keys were pressed on this frame
	public bool CheckKeyDowns (InputKeys inKeys)
	{ return (getKeyDowns & inKeys) == inKeys; }

	// returns the TileData corresponding to the passed tile, and supplies it's layer
	public bool GetDataFromTile (GameObject inTile, out TileData outData, out int outLayer)
	{
		if (!inTile.transform.IsChildOf(tileMap.transform)) { // <1>
			outLayer = 0;
			outData = new TileData();
			return false;
		} else {
			outLayer = inTile.transform.parent.GetSiblingIndex(); // <2>
			outData = data_lookup[inTile]; // <3>
			return true;
		}

		/*
		<1> If the passed tile isn't part of the map, log error and return default values
		<2> If it is, output the tile's layer by parent's sibling index
		<3> then return the TileData itself via data_lookup
		*/
	}

	// simply returns the z value of the current layer's transform
	public float GetLayerDepth ()
	{
		return tileMap.transform.GetChild(infoPanel.activeLayer).position.z;
	}

	// deletes the current scene and loads the MainMenu scene
	public void ReturnToMainMenu ()
	{
		// (!!) should prompt if unsaved
		SceneManager.LoadScene(0);
	}

	// toggles editMode and makes associated changes to current
	public void ToggleEdit ()
	{
		if (editMode) {
			genesisTile.SetProperties(gt_backup); // <1>
			gt_backup = is_tile_selected ? selected_tile : new TileData(); // <2>
			genesisTile.Activate();
		} else {
			TileData td = gt_backup;
			gt_backup = getGTData(); // <3>

			if (is_tile_selected) {
				selected_tile = td;
				genesisTile.SetProperties(selected_tile); // <4>
				genesisTile.Activate();
			}
			else genesisTile.Deactivate(); // <5>
		}

		editMode = !editMode; // <6>
		edit_flag = true;

		/*
		<1> if we're in editMode, restore the genesisTile to previous properties
		<2> if there's a tile selected, it's properties are stored in gt_backup
		<3> if we're in creation mode, current state of genesisTile is stored
		<4> if there's a tile selected, it's properties are restored from gt_backup
		<5> if no tile is selected, we deactivate the tool
		<6> either way, editMode is toggled and flag is set
		*/
	}

	// (testing) save level to a file in plain text format
	public void SaveFile (string filename)
	{
		string fpath = "Levels\\" + filename + ".txt";
		// (!!) this all is a kludge, just testing that it works
		List<TileData> tiles = new List<TileData>(data_lookup.Values);
		LayerData layer = new LayerData(0, tiles, new List<ChkpntData>());
		List<LayerData> layerList = new List<LayerData>(new LayerData[]{layer});
		LevelData _LevelDataName = new LevelData(layerList, new List<WarpData>());
		// (!!) _LevelDataName will be replaced
		string[] lines = _LevelDataName.Serialize();

		File.WriteAllLines(fpath, lines);
	}

	/* Private Functions */

	// updates getKeys and getKeyDowns each frame
	private void updateInputs ()
	{
		getKeys = InputKeys.None;
		getKeyDowns = InputKeys.None;
		int k = 0;

		for (int i = 0; i < 0x100001; i = (i == 0) ? 1 : i * 2) { // <1>
			KeyCode kc = key_code_list[k++];
			if (Input.GetKey(kc)) getKeys = getKeys | (InputKeys) i;
			if (Input.GetKeyDown(kc)) getKeyDowns = getKeyDowns | (InputKeys) i;
		}

		/*
		<1> assigns enum flags by powers of 2
		*/
	}

	// updates UI Overlay and Palette panels
	private void updateUI ()
	{
		bool sbckd = CheckKeyDowns(InputKeys.Space);
		bool tabck = CheckKeys(InputKeys.Tab);

		if (sbckd) { // <1>
			if (hudPanel.activeSelf) { // <2>
				hudPanel.Deactivate();
				menuMode = false;
			} else { // <3>
				palettePanel.Deactivate();
				hudPanel.Activate();
				menuMode = true;
			}
		}

		if (hudPanel.activeSelf) return; // <4>

		if (tabck) { // <5>
			if (!palettePanel.activeSelf) palettePanel.Activate(Input.mousePosition);
			menuMode = true;
		} else if (palettePanel.activeSelf) palettePanel.Deactivate();

		if (menuMode) genesisTile.Deactivate(); // <6>
		else genesisTile.Activate();

		/*
		<1> UI is toggled whenever spacebar is pressed
		<2> when UI is toggled off, deactivate and set menuMode to false
		<3> when UI is toggled on, activate and set menuMode to true
		<4> any time at which the UI is on, palette is ignored
		<5> palette is on whenever tab key is held down
		<6> if either menu is on, genesisTile is turned off
		*/
	}

	// makes changes associated with anchorIcon and infoPanel
	private void updateWorld ()
	{
		if (CheckKeyDowns(InputKeys.Click1)) anchorIcon.FindNewAnchor(); // <1>

		if (CheckKeyDowns(InputKeys.Q)) activateLayer(infoPanel.activeLayer - 1); // <2>
		if (CheckKeyDowns(InputKeys.E)) activateLayer(infoPanel.activeLayer + 1);

		/*
		<1> right-click will update snap cursor location
		<2> Q and E will change active layer
		*/
	}

	// makes changes associated with the state of the genesisTile
	private void updateGT ()
	{
		if (CheckKeyDowns(InputKeys.C)) genesisTile.CycleColor(false); // <1>
		if (CheckKeyDowns(InputKeys.V)) genesisTile.CycleColor(true);

		if (CheckKeyDowns(InputKeys.Z)) genesisTile.Rotate(false); // <2>
		if (CheckKeyDowns(InputKeys.X)) genesisTile.Rotate(true);

		if (CheckKeyDowns(InputKeys.One)) genesisTile.SelectType(0); // <3>
		if (CheckKeyDowns(InputKeys.Two)) genesisTile.SelectType(1);
		if (CheckKeyDowns(InputKeys.Three)) genesisTile.SelectType(2);
		if (CheckKeyDowns(InputKeys.Four)) genesisTile.SelectType(3);
		if (CheckKeyDowns(InputKeys.Five)) genesisTile.SelectType(4);
		if (CheckKeyDowns(InputKeys.Six)) genesisTile.SelectType(5);

		if (CheckKeyDowns(InputKeys.Click0)) addTile(); // <4>

		/*
		<1> when C is pressed, cycle through colors
		<2> Z and X rotate C-CW and CW, respectively
		<3> numeric keys assign tile type
		<4> if left click is made, tile is added to the level
		*/
	}

	// makes changes associated with being in editMode
	private void updateEditMode ()
	{
		if (is_tile_selected) {
			Vector3 v3 = anchorIcon.focus.ToUnitySpace();
			v3.z = GetLayerDepth();
			genesisTile.transform.position = v3; // <1>

			if (CheckKeyDowns(InputKeys.Click0)) {
				addTile(); // <2>

				genesisTile.SetProperties(gt_backup); // <3>
				genesisTile.Deactivate();
				is_tile_selected = false;
				return;
			}

			if (CheckKeyDowns(InputKeys.Delete)) { // <4>
				genesisTile.SetProperties(gt_backup);
				is_tile_selected = false;
			}
		} else {
			if (CheckKeyDowns(InputKeys.Click0)) { // <5>
				Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
				Collider2D c2d = Physics2D.GetRayIntersection(r).collider; // <6>
				if (!c2d) return; // <7>
				GameObject go = c2d.gameObject;
				removeTile(go); // <8>
			}
		}

		/*
		<1> in edit mode, a selected tile will follow the focus
		<2> if there is a selected tile, left click will place it again
		<3> we then restore genesisTile to its backup, reset flag, and return
		<4> if there is a selected tile, Delete will simply forget it
		<5> if there is no selected tile, left-click selects a tile
		<6> first we find out what (if anything) has been clicked on
		<7> if nothing is clicked on, return
		<8> if a tile was clicked on, it is removed
		*/
	}

	// adds a tile to the level based on current state of genesisTile
	private void addTile ()
	{
		levelData.layerSet[infoPanel.activeLayer].tileSet.Add(getGTData()); // <1>

		Transform tl = tileMap.transform.GetChild(infoPanel.activeLayer);
		GameObject go = genesisTile.getActiveTile();
		go.transform.SetParent(tl); // <2>

		infoPanel.AddTile(); // <3>

		/*
		<1> first, TileData is added to levelData
		<2> second, a corresponding tile is added to tileMap
		<3> lastly, the infoPanel is updated
		*/
	}

	// removes the specified tile from the level
	private void removeTile (GameObject inTile)
	{
		gt_backup = getGTData(); // <1>
		int tLayer;
		TileData tData;
		bool b = GetDataFromTile(inTile, out tData, out tLayer); // <2>
		if (b) selected_tile = tData;
		else return; // <3>
		genesisTile.Activate();
		genesisTile.SetProperties(selected_tile); // <4>

		levelData.layerSet[tLayer].tileSet.Remove(selected_tile); // <5>
		is_tile_selected = true; // <6>
		data_lookup.Remove(inTile);
		Destroy(inTile);

		infoPanel.RemoveTile();

		/*
		<1> first, back up genesisTile state
		<2> next, lookup the tile's TileData
		<3> if the specified tile is not part of tileMap, we ignore
		<4> then set the genesisTile up to act like the selected tile
		<5> after all that, levelData is updated
		<6> reset flag, remove from the lookup, and delete the tile
		<7> finally, the info panel is updated
		*/
	}

	// cycles through all layers, calculates distance, and sets opacity accordingly
	private void activateLayer (int layerIndex)
	{
		infoPanel.SetActiveLayer(layerIndex); // <1>

		foreach (Transform tileLayer in tileMap.transform) {
			int d = tileLayer.GetSiblingIndex();
			d = Math.Abs(d - layerIndex);
			setLayerOpacity(tileLayer, d);
		}

		Vector3 v3 = anchorIcon.transform.position;
		v3.z = GetLayerDepth();
		anchorIcon.transform.position = v3; // <2>

		/*
		<1> if invalid layerIndex is given, fail quietly
		<2> add active layer depth and move the snap cursor to the new location
		*/
	}

	// returns a TileData representation of the current state of genesisTile
	private TileData getGTData ()
	{
		GenesisTile gt = genesisTile;
		return new TileData(gt.tileType, gt.tileColor, anchorIcon.focus, gt.tileRotation);
	}

	// sets the opacity of all tiles within a layer based on distance from infoPanel.activeLayer
	private void setLayerOpacity (Transform tileLayer, int distance)
	{
		float a = 1f; // <1>
		int l = 0; // <2>
		if (distance != 0) { // <3>
			a = 1f / (distance + 1f);
			l = 9;
		}
		Color c = new Color(1f, 1f, 1f, a);

		foreach (Transform tile in tileLayer) { // <4>
			tile.gameObject.layer = l;
			tile.GetChild(0).GetComponent<SpriteRenderer>().color = c;
		}

		/*
		<1> a represents an alpha value
		<2> l represents the physics layer we will be setting
		<3> if this isn't the active layer, opacity and layer are set accordingly
		<4> the calculated opacity and layer are applied to all tiles within the layer
		*/
	}
}
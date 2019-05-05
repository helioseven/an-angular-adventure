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

	// references to UI elements, snap cursor, creation tool, checkpoint tool, warp tool, and tile hierarchy
	public GameObject hudPanel;
	public PaletteControl palettePanel;
	public TileCreator tileCreator;
	public GameObject chkpntTool;
	public GameObject warpTool;
	public SnapCursor anchorIcon;
	public GameObject tileMap;

	// public read-accessibility state variables
	public InputKeys getKeys { get; private set; }
	public InputKeys getKeyDowns { get; private set; }
	public string levelName { get; private set; }
	public LevelData levelData { get; private set; }
	public int activeLayer { get; private set; }
	public bool paletteMode { get; private set; }
	public bool createMode { get; private set; }
	public bool editMode { get; private set; }
	public bool selectMode { get; private set; }

	// private variables
	private EditLoader lvl_load;
	private GameObject current_tool;
	private int tool_rotation;
	private TileData? selected_tile;
	private TileData? tile_buffer;
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
		R = 0x100,
		A = 0x200,
		S = 0x400,
		D = 0x800,
		F = 0x1000,
		Z = 0x2000,
		X = 0x4000,
		C = 0x8000,
		V = 0x10000,
		One = 0x20000,
		Two = 0x40000,
		Three = 0x80000,
		Four = 0x100000,
		Five = 0x200000,
		Six = 0x400000
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
		KeyCode.R,
		KeyCode.A,
		KeyCode.S,
		KeyCode.D,
		KeyCode.F,
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

			current_tool = tileCreator.gameObject; // <2>
			tool_rotation = 0;
			selected_tile = null;
			tile_buffer = null;

			hudPanel.SetActive(false); // <3>
			chkpntTool.SetActive(false);
			warpTool.SetActive(false);
			getKeys = InputKeys.None;
			getKeyDowns = InputKeys.None;
			activeLayer = 0;
			paletteMode = false;
			createMode = true;
			editMode = false;
			selectMode = false;

			lvl_load = GameObject.FindWithTag("Loader").GetComponent<EditLoader>();
			levelName = lvl_load.levelName;
			LevelData inLevel;
			data_lookup = lvl_load.supplyLevel(ref tileMap, out inLevel); // <4>
			levelData = inLevel;
			activateLayer(activeLayer); // <5>
		} else
			Destroy(gameObject); // <6>

		/*
		<1> set singleton instance
		<2> initializations for private variables
		<3> initializations for state variables
		<4> file is loaded and parsed
		<5> first layer is activated
		<6> only one singleton can exist
		*/
	}

	void Update ()
	{
		updateInputs(); // <1>
		updateUI(); // <2>
		if (paletteMode) return;
		updateLevel(); // <3>
		if (createMode) updateCreate(); // <4>
		if (editMode) updateEdit();
		if (selectMode) updateSelect();

		/*
		<1> getKeys and getKeyDowns are updated
		<2> hudPanel and palettePanel are updated
		<3> anchorIcon and layer changes are updated
		<4> whichever tool is currently active is updated
		*/
	}

	/* Public Functions */

	// simply returns whether the given keys were being held during this frame
	public bool CheckKeys (InputKeys inKeys)
	{ return (getKeys & inKeys) == inKeys; }

	// simply returns whether the given keys were pressed on this frame
	public bool CheckKeyDowns (InputKeys inKeys)
	{ return (getKeyDowns & inKeys) == inKeys; }

	// simply returns the z value of the current layer's transform
	public float GetLayerDepth ()
	{ return tileMap.transform.GetChild(activeLayer).position.z; }

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
		<1> If the passed tile isn't part of the map, output default values and return false
		<2> If it is, output the tile's layer by parent's sibling index
		<3> then output the TileData itself via data_lookup, and return true
		*/
	}

	// deletes the current scene and loads the MainMenu scene
	public void ReturnToMainMenu ()
	{
		// (!!) should prompt if unsaved
		SceneManager.LoadScene(0);
	}

	// switches into createMode
	public void EnterCreate ()
	{
		if (createMode) return;
		if (editMode || selectMode) {
			if (tile_buffer.HasValue) tileCreator.SetProperties(tile_buffer.Value); // <1>	
			tile_buffer = selected_tile; // <2>
		}

		tileCreator.SetActive(true);
		createMode = true;
		editMode = false;
		selectMode = false;
	}

	// switches into editMode
	public void EnterEdit ()
	{
		if (editMode) return;
		if (createMode) {
			selected_tile = tile_buffer;
			tile_buffer = tileCreator.GetTileData(); // <3>
		}
		if (selectMode) {
			// something
		}

		if (selected_tile.HasValue) {
			tileCreator.SetProperties(selected_tile.Value); // <4>
			tileCreator.SetActive(true);
		} else tileCreator.SetActive(false);
		createMode = false;
		editMode = true;
		selectMode = false; // <6>

		/*
		<3> if we're in creation mode, current state of tileCreator is stored
		<4> if there's a tile selected, it's properties are restored from tile_buffer
		<6> either way, editMode is toggled and flag is set
		*/
	}

	//
	public void EnterSelect ()
	{
		if (selectMode) return;
		if (createMode) {
			selected_tile = tile_buffer;
			tile_buffer = tileCreator.GetTileData(); // <1>
		}
		if (editMode) {
			// something
		}

		tileCreator.SetActive(false);
		createMode = false;
		editMode = false;
		selectMode = true;
	}

	// (!!)(testing) save level to a file in plain text format
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

		for (int i = 0; i < 0x400001; i = (i == 0) ? 1 : i * 2) { // <1>
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
		bool sbCKD = CheckKeyDowns(InputKeys.Space);
		bool tabCK = CheckKeys(InputKeys.Tab);

		if (sbCKD) hudPanel.SetActive(!hudPanel.activeSelf); // <1>

		if (paletteMode != tabCK) {
			palettePanel.TogglePalette(); // <2>
			current_tool.SetActive(!current_tool.activeSelf);
		}
		paletteMode = palettePanel.gameObject.activeSelf;

		/*
		<1> UI is toggled whenever spacebar is pressed
		<2> palette is toggled on whenever tab key is held down
		*/
	}

	// makes changes associated with anchorIcon and layer changes
	private void updateLevel ()
	{
		if (CheckKeyDowns(InputKeys.Click1)) anchorIcon.FindNewAnchor(); // <2>

		if (CheckKeyDowns(InputKeys.F)) activateLayer(activeLayer - 1); // <3>
		if (CheckKeyDowns(InputKeys.R)) activateLayer(activeLayer + 1);

		/*
		<2> right-click will update snap cursor location
		<3> F and R will change active layer
		*/
	}

	// makes changes associated with being in createMode
	private void updateCreate ()
	{
		bool b1 = current_tool == chkpntTool;
		bool b2 = current_tool == tileCreator.gameObject;
		bool b3 = current_tool == warpTool;
		bool bRot = false;

		if (b1 || b2 || b3) {
			if (CheckKeyDowns(InputKeys.Q)) {
				tool_rotation++; // <2>
				bRot = true;
			}
			if (CheckKeyDowns(InputKeys.E)) {
				tool_rotation--;
				bRot = true;
			}

			if (CheckKeyDowns(InputKeys.One)) tileCreator.SelectType(0); // <3>
			if (CheckKeyDowns(InputKeys.Two)) tileCreator.SelectType(1);
			if (CheckKeyDowns(InputKeys.Three)) tileCreator.SelectType(2);
			if (CheckKeyDowns(InputKeys.Four)) tileCreator.SelectType(3);
			if (CheckKeyDowns(InputKeys.Five)) tileCreator.SelectType(4);
			if (CheckKeyDowns(InputKeys.Six)) tileCreator.SelectType(5);

			InputKeys typeKeys = InputKeys.One | InputKeys.Two;
			typeKeys |= InputKeys.Three | InputKeys.Four;
			typeKeys |= InputKeys.Five | InputKeys.Six;
			if (!b2 && CheckKeyDowns(typeKeys)) setTool(tileCreator.gameObject);
		}

		if (b2) {
			if (bRot) tileCreator.SetRotation(tool_rotation);
			if (CheckKeyDowns(InputKeys.Z)) tileCreator.CycleColor(false); // <1>
			if (CheckKeyDowns(InputKeys.X)) tileCreator.CycleColor(true);

			if (CheckKeyDowns(InputKeys.Click0)) addTile(); // <4>
		}

		Vector3 rot = new Vector3(0, 0, 30 * tool_rotation);
		Vector3 pos = anchorIcon.focus.ToUnitySpace();
		pos.z = anchorIcon.transform.position.z;

		if (b1) {
			chkpntTool.transform.position = pos;
			chkpntTool.transform.eulerAngles = rot;
			if (CheckKeyDowns(InputKeys.Click0)) Debug.Log("Place checkpoint.");
		}

		if (b3) {
			warpTool.transform.position = pos;
			warpTool.transform.eulerAngles = rot;
			if (CheckKeyDowns(InputKeys.Click0)) Debug.Log("Place warp.");
		}

		/*
		<2> Q and E rotate tile C-CW and CW, respectively
		<3> numeric keys assign tile type
		<1> Z and X rotate through colors
		<4> if left click is made, tile is added to the level
		*/
	}

	// makes changes associated with being in editMode
	private void updateEdit ()
	{
		if (selected_tile.HasValue) {
			Vector3 v3 = anchorIcon.focus.ToUnitySpace();
			v3.z = GetLayerDepth();
			tileCreator.transform.position = v3; // <1>

			if (CheckKeyDowns(InputKeys.Click0)) {
				addTile(); // <2>

				tileCreator.SetActive(false); // <3>
				selected_tile = null;
				return;
			}

			if (CheckKeyDowns(InputKeys.Delete)) { // <4>
				tileCreator.SetActive(false);
				selected_tile = null;
			}
		} else if (CheckKeyDowns(InputKeys.Click0)) { // <5>
/*
			float d;
			Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
			Plane p = new Plane(Vector3.back, GetLayerDepth());
			if (!p.Raycast(r, out d)) {
				Debug.LogError("Screen click ray did not intersect with layer plane.");
				return;
			}
			Vector2 v2 = r.GetPoint(d);
*/
			Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
			Collider2D c2d = Physics2D.GetRayIntersection(r).collider; // <6>
			Debug.Log(c2d);
			if (!c2d) return; // <7>
			if (c2d.transform.IsChildOf(tileMap.transform)) removeTile(c2d.gameObject); // <8>
			// else remove chkpnt or warp
		}

		/*
		<1> in edit mode, a selected tile will follow the focus
		<2> if there is a selected tile, left click will place it again
		<3> we then restore tileCreator to its backup, reset flag, and return
		<4> if there is a selected tile, Delete will simply forget it
		<5> if there is no selected tile, left-click selects a tile
		<6> first we find out what (if anything) has been clicked on
		<7> if nothing is clicked on, return
		<8> if a tile was clicked on, it is removed
		*/
	}

	// makes changes associated with being in selectMode
	private void updateSelect ()
	{
		//
	}

	// adds a tile to the level based on current state of tileCreator
	private void addTile ()
	{
		levelData.layerSet[activeLayer].tileSet.Add(tileCreator.GetTileData()); // <1>

		Transform tl = tileMap.transform.GetChild(activeLayer);
		GameObject go = tileCreator.GetActiveTile();
		go.transform.SetParent(tl); // <2>

		/*
		<1> first, TileData is added to levelData
		<2> second, a corresponding tile is added to tileMap
		*/
	}

	// removes the specified tile from the level
	private void removeTile (GameObject inTile)
	{
		tile_buffer = tileCreator.GetTileData(); // <1>
		int tLayer;
		TileData tData;
		bool b = GetDataFromTile(inTile, out tData, out tLayer); // <2>
		if (b) selected_tile = tData;
		else return; // <3>
		tileCreator.SetActive(true);
		tileCreator.SetProperties(selected_tile.Value); // <4>

		levelData.layerSet[tLayer].tileSet.Remove(selected_tile.Value); // <5>
		// is_tile_selected = true; // <6>
		data_lookup.Remove(inTile);
		Destroy(inTile);

		/*
		<1> first, back up tileCreator state
		<2> next, lookup the tile's TileData
		<3> if the specified tile is not part of tileMap, we ignore
		<4> then set the tileCreator up to act like the selected tile
		<5> after all that, levelData is updated
		<6> reset flag, remove from the lookup, and delete the tile
		*/
	}

	// sets the currently active tool
	private void setTool (GameObject inTool)
	{
		bool b = false;
		b |= inTool == chkpntTool;
		b |= inTool == tileCreator.gameObject;
		b |= inTool == warpTool;
		if (!b) return;

		current_tool.SetActive(false);
		current_tool = inTool;
		current_tool.SetActive(true);
	}

	// cycles through all layers, calculates distance, and sets opacity accordingly
	private void activateLayer (int inLayer)
	{
		bool b = (inLayer < 0) || (inLayer >= tileMap.transform.childCount);
		if (b) return; // <1>
		else activeLayer = inLayer; // <2>

		foreach (Transform layer in tileMap.transform) {
			int d = layer.GetSiblingIndex();
			d = Math.Abs(d - activeLayer);
			setLayerOpacity(layer, d); // <3>
		}

		Vector3 v3 = anchorIcon.transform.position;
		v3.z = GetLayerDepth();
		anchorIcon.transform.position = v3; // <4>

		/*
		<1> if invalid layer index is given, fail quietly
		<2> otherwise update activeLayer and continue
		<3> ordinal distance from activeLayer is calculated, and opacity set accordingly
		<4> add active layer depth and move the snap cursor to the new location
		*/
	}

	// sets the opacity of all tiles within a layer using ordinal distance from activeLayer
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
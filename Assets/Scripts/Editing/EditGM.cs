using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using circleXsquares;

public partial class EditGM : MonoBehaviour {

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
	public GameObject chkpntMap;
	public GameObject warpMap;

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
	private SelectedItem? selected_item;
	private GameObject current_tool;
	private TileData tile_buffer;
	private Dictionary<GameObject, TileData> tile_lookup;
	private Dictionary<GameObject, ChkpntData> chkpnt_lookup;
	private Dictionary<GameObject, WarpData> warp_lookup;

	void Awake ()
	{
		if (!instance) {
			instance = this; // <1>

			current_tool = tileCreator.gameObject; // <2>
			selected_item = null;
			tile_buffer = new TileData();
			tile_lookup = new Dictionary<GameObject, TileData>();
			chkpnt_lookup = new Dictionary<GameObject, ChkpntData>();
			warp_lookup = new Dictionary<GameObject, WarpData>();

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
			levelData = lvl_load.supplyLevel(); // <4>
			buildLevel(levelData);

			activateLayer(activeLayer); // <5>
		} else
			Destroy(gameObject); // <6>

		/*
		<1> set singleton instance
		<2> initializations for private variables
		<3> initializations for connected state variables
		<4> file is loaded and parsed
		<5> first layer is activated
		<6> only one singleton can exist
		*/
	}

	void Update ()
	{
		updateInputs(); // <1>
		updateUI(); // <2>
		if (paletteMode) return; // <3>
		updateLevel(); // <4>
		if (createMode) updateCreate(); // <5>
		if (editMode) updateEdit(); // <6>
		if (selectMode) updateSelect(); // <7>

		/*
		<1> getKeys and getKeyDowns are updated
		<2> hudPanel and palettePanel are updated
		<3> if the palette is active, skip the rest
		<4> anchorIcon and layer changes are updated
		<5> current tool is updated for createMode
		<6> current tool is updated for editMode
		<7> current tool is updated for selectMode
		*/
	}

	/* Public Functions */

	// switches into createMode
	public void EnterCreate ()
	{
		if (createMode || !(editMode || selectMode)) return; // <1>
		if (editMode && selected_item.HasValue) addSelectedItem(selected_item.Value); // <2>

		tileCreator.SetProperties(tile_buffer); // <3>
		setTool(tileCreator.gameObject);
		createMode = true;
		editMode = false;
		selectMode = false;

		/*
		<1> only do anything if currently in editMode or selectMode
		<2> conditional logic for switching out of editMode while an object is selected
		<3> tileCreator values are recovered from tile_buffer, and is then activated
		*/
	}

	// switches into editMode
	public void EnterEdit ()
	{
		if (editMode || !(createMode || selectMode)) return; // <1>
		if (createMode) tile_buffer = tileCreator.GetTileData(); // <2>

		if (selected_item.HasValue) removeSelectedItem(selected_item.Value); // <3>
		else current_tool.SetActive(false); // <4>
		createMode = false;
		editMode = true;
		selectMode = false;

		/*
		<1> only do anyting if currently in creationMode or selectMode
		<2> if we're in creation mode, current state of tileCreator is stored in tile_buffer
		<3> conditional logic for switching into editMode while an object is selected
		<4> if nothing is selected, make sure current_tool is disabled
		*/
	}

	// switches into selectMode
	public void EnterSelect ()
	{
		if (selectMode || !(createMode || editMode)) return; // <1>
		if (createMode) tile_buffer = tileCreator.GetTileData(); // <2>

		if (editMode && selected_item.HasValue) addSelectedItem(selected_item.Value); // <3>
		current_tool.SetActive(false); // <4>
		createMode = false;
		editMode = false;
		selectMode = true;

		/*
		<1> only do anyting if currently in creationMode or editMode
		<2> if we're in creation mode, current state of tileCreator is stored in tile_buffer
		<3> conditional logic for switching out of editMode while an object is selected
		<4> current_tool should always be disabled in selectMode
		*/
	}
}
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
	public InputKeys getInputs { get; private set; }
	public InputKeys getInputDowns { get; private set; }
	public string levelName { get; private set; }
	public LevelData levelData { get; private set; }
	public int activeLayer { get; private set; }
	public bool paletteMode { get; private set; }
	public bool selectMode {
		get { return current_mode == EditorMode.Select; }
		set {}
	}
	public bool editMode {
		get { return current_mode == EditorMode.Edit; }
		set {}
	}
	public bool createMode {
		get { return current_mode == EditorMode.Create; }
		set {}
	}
	public bool paintMode {
		get { return current_mode == EditorMode.Paint; }
		set {}
	}

	// private variables
	private EditLoader lvl_load;
	private EditorMode current_mode;
	private SelectedItem selected_item;
	private GameObject current_tool;
	private Dictionary<GameObject, TileData> tile_lookup;
	private Dictionary<GameObject, ChkpntData> chkpnt_lookup;
	private Dictionary<GameObject, WarpData> warp_lookup;

	void Awake ()
	{
		if (!instance) {
			instance = this; // <1>

			current_tool = tileCreator.gameObject; // <2>
			current_mode = EditorMode.Create;
			selected_item = new SelectedItem();
			tile_lookup = new Dictionary<GameObject, TileData>();
			chkpnt_lookup = new Dictionary<GameObject, ChkpntData>();
			warp_lookup = new Dictionary<GameObject, WarpData>();

			hudPanel.SetActive(false); // <3>
			chkpntTool.SetActive(false);
			warpTool.SetActive(false);

			getInputs = InputKeys.None; // <4>
			getInputDowns = InputKeys.None;
			activeLayer = 0;
			paletteMode = false;

			lvl_load = GameObject.FindWithTag("Loader").GetComponent<EditLoader>();
			levelName = lvl_load.levelName;
			levelData = lvl_load.supplyLevel(); // <5>
			buildLevel(levelData);

			activateLayer(activeLayer); // <6>
		} else
			Destroy(gameObject); // <7>

		/*
		<1> set singleton instance
		<2> initializations for private state variables
		<3> initializations for connected state variables
		<4> initializations for public state variables
		<5> file is loaded and parsed
		<6> first layer is activated
		<7> only one singleton can exist
		*/
	}

	void Update ()
	{
		updateInputs(); // <1>
		updateUI(); // <2>
		if (paletteMode) return; // <3>
		updateLevel(); // <4>
		if (selectMode) updateSelect(); // <7>
		if (editMode) updateEdit(); // <6>
		if (createMode) updateCreate(); // <5>
		if (paintMode) updatePaint();

		/*
		<1> getInputs and getInputDowns are updated
		<2> hudPanel and palettePanel are updated
		<3> if the palette is active, skip the rest
		<4> anchorIcon and layer changes are updated
		<5> current tool is updated for createMode
		<6> current tool is updated for editMode
		<7> current tool is updated for selectMode
		*/
	}
}
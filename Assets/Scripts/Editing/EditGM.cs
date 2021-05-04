using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using circleXsquares;

public partial class EditGM : MonoBehaviour {

	// singleton instance
	[HideInInspector] public static EditGM instance = null;

	// references to UI elements, snap cursor, creation tool, checkpoint tool, warp tool, and tile hierarchy
	public EventSystem eventSystem;
	public GraphicRaycaster uiRaycaster;
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
	public string levelName {
		get { return level_name; }
		set { setLevelName(value); }
	}
	public LevelData levelData { get; private set; }
	public int activeLayer { get; private set; }
	public SelectedItem selectedItem {
		get { return selected_item; }
		set {}
	}
	public bool hoveringHUD { get; private set; }
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
	private string level_name;
	private EditLoader lvl_load;
	private EditorMode current_mode;
	private EditTools tool_mode;
	private SelectedItem selected_item;
	private GameObject current_tool;
	private SpecialCreator warp_tool;
	private Dictionary<GameObject, TileData> tile_lookup;
	private Dictionary<GameObject, ChkpntData> chkpnt_lookup;
	private Dictionary<GameObject, WarpData> warp_lookup;
	private List<RaycastResult> currentHUDhover;

	// constants
	private const int INACTIVE_LAYER = 9;
	private const int DEFAULT_LAYER = 0;

	void Awake ()
	{
		if (!instance) {
			instance = this; // <1>

			current_mode = EditorMode.Create; // <2>
			tool_mode = EditTools.Tile;
			current_tool = tileCreator.gameObject;
			TileData td = new TileData(0, 0, 0, new HexOrient());
			selected_item = new SelectedItem(td);
			warp_tool = warpTool.GetComponent<SpecialCreator>();
			tile_lookup = new Dictionary<GameObject, TileData>();
			chkpnt_lookup = new Dictionary<GameObject, ChkpntData>();
			warp_lookup = new Dictionary<GameObject, WarpData>();

			hudPanel.SetActive(false); // <3>
			palettePanel.gameObject.SetActive(false);
			tileCreator.gameObject.SetActive(true);
			chkpntTool.SetActive(false);
			warpTool.SetActive(false);

			getInputs = InputKeys.None; // <4>
			getInputDowns = InputKeys.None;
			activeLayer = 0;
			hoveringHUD = false;
			paletteMode = false;

			activateLayer(activeLayer); // <5>
		} else
			Destroy(gameObject); // <6>

		/*
		<1> set singleton instance
		<2> initializations for private state variables
		<3> initializations for connected state variables
		<4> initializations for public state variables
		<5> first layer is activated
		<6> only one singleton can exist
		*/
	}

	void Start ()
	{
		lvl_load = GameObject.FindWithTag("Loader").GetComponent<EditLoader>(); // <1>
		level_name = lvl_load.levelName;
		levelData = lvl_load.supplyLevel(); // <2>
		buildLevel(levelData);

		/*
		<1> EditLoader is found by tag
		<2> file is loaded and parsed
		*/
	}

	void Update ()
	{
		updateInputs(); // <1>
		updateUI(); // <2>
		if (hoveringHUD || paletteMode) return; // <3>
		updateLevel(); // <4>
		if (selectMode) updateSelect(); // <5>
		if (editMode) updateEdit(); // <5>
		if (createMode) updateCreate(); // <5>
		if (paintMode) updatePaint(); // <5>

		/*
		<1> getInputs and getInputDowns are updated
		<2> hudPanel and palettePanel are updated
		<3> if the palette is active, skip the rest
		<4> anchorIcon and layer changes are updated
		<5> call update function for respective mode
		*/
	}
}

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

    // references to UI elements, snap cursor, creation tool,
    // checkpoint tool, warp tool, and tile hierarchy
    public SnapCursor anchorIcon;
    public GameObject chkpntMap;
    public GameObject chkpntTool;
    public GameObject hudPanel;
    public PaletteControl palettePanel;
    public TileCreator tileCreator;
    public GameObject tileMap;
    public GameObject warpMap;
    public GameObject warpTool;

    // public read-accessibility state variables
    public int activeLayer { get; private set; }
    public InputKeys getInputs { get; private set; }
    public InputKeys getInputDowns { get; private set; }
    public LevelData levelData { get; private set; }
    public string levelName { get; private set; }
    public SelectedItem selectedItem {
        get { return _selectedItem; }
        set {}
    }
    // public boolean flags
    public bool createMode {
        get { return _currentMode == EditorMode.Create; }
        set {}
    }
    public bool editMode {
        get { return _currentMode == EditorMode.Edit; }
        set {}
    }
    public bool paintMode {
        get { return _currentMode == EditorMode.Paint; }
        set {}
    }
    public bool paletteMode { get; private set; }
    public bool selectMode {
        get { return _currentMode == EditorMode.Select; }
        set {}
    }

    // private constants
    private const int DEFAULT_LAYER = 0;
    private const int INACTIVE_LAYER = 9;

    // private variables
    private Dictionary<GameObject, ChkpntData> _chkpntLookup;
    private EditorMode _currentMode;
    private GameObject _currentTool;
    private EditLoader _lvlLoad;
    private SelectedItem _selectedItem;
    private Dictionary<GameObject, TileData> _tileLookup;
    private EditTools _toolMode;
    private Dictionary<GameObject, WarpData> _warpLookup;
    private SpecialCreator _warpTool;

    void Awake ()
    {
        if (!instance) {
            // set singleton instance
            instance = this;

            // initializations for private state variables
            _currentMode = EditorMode.Create;
            _toolMode = EditTools.Tile;
            _currentTool = tileCreator.gameObject;
            TileData td = new TileData(0, 0, 0, new HexOrient());
            _selectedItem = new SelectedItem(td);
            _warpTool = warpTool.GetComponent<SpecialCreator>();
            _tileLookup = new Dictionary<GameObject, TileData>();
            _chkpntLookup = new Dictionary<GameObject, ChkpntData>();
            _warpLookup = new Dictionary<GameObject, WarpData>();

            // initializations for connected state variables
            hudPanel.SetActive(false);

            // initializations for public state variables
            getInputs = InputKeys.None;
            getInputDowns = InputKeys.None;
            activeLayer = 0;
            paletteMode = false;

            // file is loaded and parsed
            _lvlLoad = GameObject.FindWithTag("Loader").GetComponent<EditLoader>();
            levelName = _lvlLoad.levelName;
            levelData = _lvlLoad.supplyLevel();
            buildLevel(levelData);

            // first layer is activated
            activateLayer(activeLayer);
        } else
            // only one singleton can exist
            Destroy(gameObject);
    }

    void Update ()
    {
        // getInputs and getInputDowns are updated
        updateInputs();
        // hudPanel and palettePanel are updated
        updateUI();
        // if the palette is active, skip the rest
        if (paletteMode)
            return;
        // anchorIcon and layer changes are updated
        updateLevel();
        // current tool is updated for selectMode
        if (selectMode)
            updateSelect();
        // current tool is updated for editMode
        if (editMode)
            updateEdit();
        // current tool is updated for createMode
        if (createMode)
            updateCreate();
        // current tool is updated for paintMode
        if (paintMode)
            updatePaint();
    }
}

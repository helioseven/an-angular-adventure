using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using circleXsquares;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class EditGM : MonoBehaviour
{
    // singleton instance
    [HideInInspector]
    public static EditGM instance = null;

    // references to UI elements, snap cursor, creation tool,
    // checkpoint tool, warp tool, and tile hierarchy
    public SnapCursor anchorIcon;
    public GameObject chkpntMap;
    public GameObject chkpntTool;
    public EventSystem eventSystem;
    public GameObject hudPanel;
    public PaletteControl palettePanel;
    public SoundManager soundManager;
    public TileCreator tileCreator;
    public GameObject tileMap;
    public GraphicRaycaster uiRaycaster;
    public GameObject victoryMap;
    public GameObject victoryTool;
    public GameObject warpMap;
    public GameObject warpTool;
    public GameObject playLoader;

    // Level Info
    public LevelInfo levelInfo;

    // public read-accessibility state variables
    public int activeLayer { get; private set; }
    public bool createMode
    {
        get { return _currentMode == EditorMode.Create; }
        set { }
    }
    public bool editMode
    {
        get { return _currentMode == EditorMode.Edit; }
        set { }
    }
    public InputKeys getInputs { get; private set; }
    public InputKeys getInputDowns { get; private set; }
    public bool hoveringHUD { get; private set; }
    public LevelData levelData { get; private set; }
    public SelectedItem selectedItem
    {
        get { return _selectedItem; }
        set { }
    }
    public bool paintMode
    {
        get { return _currentMode == EditorMode.Paint; }
        set { }
    }
    public bool paletteMode { get; private set; }
    public bool selectMode
    {
        get { return _currentMode == EditorMode.Select; }
        set { }
    }

    // public read- and set-accessibility state variables
    public bool inputMode
    {
        get { return _inputMode; }
        set { _inputMode = value; }
    }
    public string levelName
    {
        get { return _levelName; }
        set { SetLevelName(value); }
    }

    // private constants
    private const int DEFAULT_LAYER = 0;
    private const int INACTIVE_LAYER = 9;

    // private variables
    private Dictionary<GameObject, ChkpntData> _chkpntLookup;
    private List<RaycastResult> _currentHUDhover;
    private EditorMode _currentMode;
    private GameObject _currentTool;
    private bool _inputMode;
    private EditLoader _lvlLoad;
    private string _levelName;
    private SelectedItem _selectedItem;
    private Dictionary<GameObject, TileData> _tileLookup;
    private EditTools _toolMode;
    private Dictionary<GameObject, WarpData> _warpLookup;
    private SpecialCreator _warpTool;
    private Dictionary<GameObject, VictoryData> _victoryLookup;

    void Awake()
    {
        if (!instance)
        {
            // set singleton instance
            instance = this;

            // initializations for private state variables
            _currentHUDhover = new List<RaycastResult>();
            _inputMode = false;
            _currentMode = EditorMode.Create;
            _toolMode = EditTools.Tile;
            _currentTool = tileCreator.gameObject;
            TileData td = new TileData(0, 0, 0, new HexOrient());
            _selectedItem = new SelectedItem(td);
            _warpTool = warpTool.GetComponent<SpecialCreator>();
            _tileLookup = new Dictionary<GameObject, TileData>();
            _chkpntLookup = new Dictionary<GameObject, ChkpntData>();
            _warpLookup = new Dictionary<GameObject, WarpData>();
            _victoryLookup = new Dictionary<GameObject, VictoryData>();

            // initializations for connected state variables
            hudPanel.SetActive(true);

            // initializations for public state variables
            getInputs = InputKeys.None;
            getInputDowns = InputKeys.None;
            activeLayer = 0;
            hoveringHUD = false;
            paletteMode = false;

            // file is loaded and parsed
            _lvlLoad = GameObject.FindWithTag("Loader").GetComponent<EditLoader>();
            levelName = string.IsNullOrEmpty(_lvlLoad.levelName) ? "My Tessel" : _lvlLoad.levelName;
            levelInfo = _lvlLoad.levelInfo;
            levelData = _lvlLoad.supplyLevel();
            buildLevel(levelData);

            // first layer is activated
            activateLayer(activeLayer);

            // set sound manager
            soundManager = GameObject.FindWithTag("SoundManager").GetComponent<SoundManager>();
            // play the edit scene wakeup sound
            int variant = UnityEngine.Random.Range(1, 3);
            soundManager.Play($"loading-{variant}");
        }
        else
        {
            // only one singleton can exist
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // getInputs and getInputDowns are updated
        updateInputs();
        // hudPanel and palettePanel are updated
        updateUI();
        // if the palette is active, skip the rest
        if (hoveringHUD || paletteMode || inputMode)
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

using System.Collections.Generic;
using circleXsquares;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class EditGM : MonoBehaviour
{
    // singleton instance
    [HideInInspector]
    public static EditGM instance = null;

    // references to UI elements, snap cursor, creation tool,
    // checkpoint tool, warp tool, and tile hierarchy
    public SnapCursor anchorIcon;
    public GameObject checkpointMap;
    public GameObject checkpointTool;
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
    public LevelInfo levelInfo;
    public GameObject quitDialogPanel;

    // public read-accessibility state variables
    public int activeLayer { get; private set; }
    public bool isEditorInCreateMode
    {
        get { return _currentEditorMode == EditorMode.Create; }
        set { }
    }
    public bool isEditorInEditMode
    {
        get { return _currentEditorMode == EditorMode.Edit; }
        set { }
    }
    public bool hoveringHUD { get; private set; }
    public LevelData levelData { get; private set; }
    public SelectedItem selectedItem
    {
        get { return _selectedItem; }
        set { }
    }

    public EditCreatorTool currentCreatorTool
    {
        get { return _currentCreatorTool; }
        set { }
    }
    public bool paintMode
    {
        get { return _currentEditorMode == EditorMode.Paint; }
        set { }
    }
    public bool paletteMode { get; private set; }
    public bool selectMode
    {
        get { return _currentEditorMode == EditorMode.Select; }
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
    private List<RaycastResult> _currentHUDhover;
    private EditorMode _currentEditorMode;
    private GameObject _currentCreatorToolGameObject;
    private bool _inputMode;
    private EditLoader _lvlLoad;
    private string _levelName;

    // Selected item is the one that was last placed or just clicked on from select mode
    // Selected item is NOT the "current creator tool's live knowledge of what tile you'd like to place"
    private SelectedItem _selectedItem;

    // _currentCreatorTool is used in creation mode to keep track of the current live knowledge of what tile you'd like to place
    private EditCreatorTool _currentCreatorTool;

    // Item lookups
    private Dictionary<GameObject, TileData> _tileLookup;
    private Dictionary<GameObject, CheckpointData> _checkpointLookup;
    private Dictionary<GameObject, WarpData> _warpLookup;
    private Dictionary<GameObject, VictoryData> _victoryLookup;
    private bool _suppressClickThisFrame = false;

    void Awake()
    {
        if (!instance)
        {
            // set singleton instance
            instance = this;

            // initializations for private state variables
            _currentHUDhover = new List<RaycastResult>();
            _inputMode = false;
            _currentEditorMode = EditorMode.Create;
            _currentCreatorTool = EditCreatorTool.Tile;
            _currentCreatorToolGameObject = tileCreator.gameObject;
            TileData td = new TileData(0, 0, 0, new HexOrient());
            _selectedItem = new SelectedItem(td);
            _tileLookup = new Dictionary<GameObject, TileData>();
            _checkpointLookup = new Dictionary<GameObject, CheckpointData>();
            _warpLookup = new Dictionary<GameObject, WarpData>();
            _victoryLookup = new Dictionary<GameObject, VictoryData>();

            // initializations for connected state variables
            hudPanel.SetActive(true);

            activeLayer = 0;
            hoveringHUD = false;
            paletteMode = false;

            // file is loaded and parsed
            _lvlLoad = GameObject.FindWithTag("Loader").GetComponent<EditLoader>();
            levelName = string.IsNullOrEmpty(_lvlLoad.levelName)
                ? LevelNameGenerator.GenerateLevelName()
                : _lvlLoad.levelName;
            // TODO: Only do this when coming from playing mode
            levelName = CleanAutosaveName(levelName);
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

    void Start()
    {
        InputManager.Instance.SetSceneInputs("Editing");
    }

    void Update()
    {
        // Check for escape key and pop up the quit (exit to main menu) dialog
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            quitDialogPanel.gameObject.SetActive(true);
        }
        // get raycast results for this frame's mouse position
        _currentHUDhover = raycastAllHUD();
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
        // current tool is updated for isEditorInEditMode
        if (isEditorInEditMode)
            updateEdit();
        // current tool is updated for isEditorInCreateMode
        if (isEditorInCreateMode)
            updateCreate();
        // current tool is updated for paintMode
        if (paintMode)
            updatePaint();
    }
}

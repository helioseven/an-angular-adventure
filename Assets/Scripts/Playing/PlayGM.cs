using circleXsquares;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public partial class PlayGM : MonoBehaviour
{
    [HideInInspector]
    public static PlayGM instance = null;
    public Boundary boundaryDown;
    public Boundary boundaryLeft;
    public Boundary boundaryRight;
    public Boundary boundaryUp;
    public GameObject chkpntRef;
    public GameObject checkpointMap;
    public GameObject deathParticles;
    public Player_Controller player;
    public SoundManager soundManager;
    public GameObject tileCreator;
    public GameObject tileMap;
    public GameObject victoryRef;
    public GameObject victoryMap;
    public GameObject warpRef;
    public GameObject warpMap;
    public EditLoader editLoader;
    public GameObject playtestWatermark;
    public TMP_Text levelNameText;
    public PlayModeContext playModeContext = PlayModeContext.FromMainMenuPlayButton;
    public LevelInfo levelInfo;

    [SerializeField]
    private PauseMenu pauseMenu;

    // public read-accessibile state variables
    public GameObject activeCheckpoint { get; private set; }
    public CheckpointData activeCheckpointData { get; private set; }
    public GravityDirection activeCheckpointGravity { get; private set; }
    public int activeLayer { get; private set; }
    public GravityDirection gravDirection => _gravDir;
    public LevelData levelData { get; private set; }
    public string levelName { get; private set; }
    public bool victoryAchieved { get; private set; }
    public GameObject levelCompletePanel;
    public TMP_Text victoryTimeText;

    [Header("Victory Grab")]
    [SerializeField]
    private float victoryPullDuration = 0.4f;

    [SerializeField]
    private float victoryHoldDuration = 0.6f;

    public static readonly string[] INT_TO_NAME =
    {
        "Zero",
        "One",
        "Two",
        "Three",
        "Four",
        "Five",
        "Six",
        "Seven",
        "Eight",
        "Nine",
    };

    public enum PlayModeContext
    {
        FromEditor,
        FromBrowser,
        FromMainMenuPlayButton,
    }

    public PlayLoader levelLoader = null;

    // private consts
    private const int ARROW_OR_KEY_CHILD_INDEX = 2;
    private const int DEFAULT_LAYER = 0;
    private const int INACTIVE_LAYER = 9;
    private const int LOCK_CHILD_INDEX = 1;

    // private variables
    private Clock _clock;
    private HexOrient _playerStart;

    // for swapping mobile controls on and off
    [SerializeField]
    private GameObject mobileControlsLayer;
    private GravityDirection _gravDir;

    void Awake()
    {
        if (!instance)
        {
            instance = this;
            levelLoader = GameObject.FindWithTag("Loader").GetComponent<PlayLoader>();
            player = GameObject.FindWithTag("Player").GetComponent<Player_Controller>();
            soundManager = GameObject.FindWithTag("SoundManager").GetComponent<SoundManager>();
            _clock = GameObject.FindWithTag("Clock").GetComponent<Clock>();
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // turn off mobile controls on non mobile
#if !UNITY_IOS
        if (mobileControlsLayer != null)
            mobileControlsLayer.SetActive(false);
#endif
    }

    void Start()
    {
        InputManager.Instance.SetSceneInputs("Playing");

        Debug.Log("[PlayGM] [Start()] levelLoader.levelName: " + levelLoader.levelName);
        levelName = levelLoader.levelName;
        playModeContext = levelLoader.playModeContext;

        if (playModeContext == PlayModeContext.FromEditor)
        {
            levelName = EditGM.CleanAutosaveName(levelName);
            Debug.Log("[PlayGM] [Start()] Name Cleaned: " + levelName);
        }

        levelInfo = levelLoader.levelInfo;
        levelData = levelLoader.supplyLevel();
        buildLevel(levelData);

        activeLayer = 0;
        activateLayer(0);
        player.transform.position = _playerStart.locus.ToUnitySpace();
        victoryAchieved = false;

        _gravDir = GravityDirection.Down;
        Physics2D.gravity = new Vector2(0.0f, -9.81f);
        player.UpdateJumpForceVector(GravityDirection.Down);

        Boundary[] bs = { boundaryDown, boundaryLeft, boundaryRight, boundaryUp };
        foreach (Boundary b in bs)
            b.SetBoundary();
        foreach (Boundary b in bs)
            b.SetBoundarySpanFromPeers();

        GameObject checkpoint = checkpointMap.transform.GetChild(0).gameObject;
        SetCheckpoint(checkpoint);
        SetCheckpointData(checkpoint.GetComponent<Checkpoint>().data);

        levelNameText.text = levelName;

        playtestWatermark.SetActive(playModeContext == PlayModeContext.FromEditor);

        player.gameObject.SetActive(false);
        Rigidbody2D rb2d = player.GetComponent<Rigidbody2D>();
        bool isIntroSpawn = true;
        StartCoroutine(ResetToCheckpoint(rb2d, isIntroSpawn));
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            pauseMenu.TogglePause();
        else if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
            pauseMenu.TogglePause();
    }

    public void QuitToMenu()
    {
        if (playModeContext == PlayModeContext.FromEditor)
        {
            var loaderGO = Instantiate(editLoader);
            var loader = loaderGO.GetComponent<EditLoader>();
            loader.levelInfo = levelInfo;
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}

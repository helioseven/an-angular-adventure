using circleXsquares;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class PlayGM : MonoBehaviour
{
    // singleton instance
    [HideInInspector]
    public static PlayGM instance = null;

    // public references
    public Boundary boundaryDown;
    public Boundary boundaryLeft;
    public Boundary boundaryRight;
    public Boundary boundaryUp;
    public GameObject chkpntRef;
    public GameObject chkpntMap;
    public GameObject deathParticles;
    public Player_Controller player;
    public SoundManager soundManager;
    public GameObject tileCreator;
    public GameObject tileMap;
    public GameObject victoryRef;
    public GameObject victoryMap;
    public GameObject warpRef;
    public GameObject warpMap;

    // public read-accessibility state variables
    public GameObject activeCheckpoint { get; private set; }

    public ChkpntData activeCheckpointData { get; private set; }
    public int activeLayer { get; private set; }
    public GravityDirection gravDirection
    {
        get { return _gravDir; }
        set { }
    }
    public LevelData levelData { get; private set; }
    public string levelName { get; private set; }
    public bool victoryAchieved { get; private set; }
    public GameObject levelCompletePanel;
    public TMP_Text victoryTimeText;

    // private constants
    private const int DEFAULT_LAYER = 0;
    private const int DEFAULT_NUM_LAYERS = 10;
    private const int INACTIVE_LAYER = 9;
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

    // private references
    public PlayLoader levelLoader = null;

    // private variables
    private GravityDirection _gravDir;
    private HexOrient _playerStart;
    private Clock clock;

    void Awake()
    {
        if (!instance)
        {
            // set singleton instance, then references
            instance = this;
            levelLoader = GameObject.FindWithTag("Loader").GetComponent<PlayLoader>();
            player = GameObject.FindWithTag("Player").GetComponent<Player_Controller>();
            soundManager = GameObject.FindWithTag("SoundManager").GetComponent<SoundManager>();
            clock = GameObject.FindWithTag("Clock").GetComponent<Clock>();
            clock = GameObject.FindWithTag("Clock").GetComponent<Clock>();
        }
        else
            // if another singleton already exists, this one cannot
            Destroy(gameObject);
    }

    void Start()
    {
        // load the level
        Debug.Log("PlayGM Start levelid: " + levelLoader.levelName);
        levelName = levelLoader.levelName;
        levelData = levelLoader.supplyLevel();
        buildLevel(levelData);

        // initialize variables
        activeLayer = 0;
        activateLayer(0);
        player.transform.position = _playerStart.locus.ToUnitySpace();
        victoryAchieved = false;

        // reset gravity for real
        _gravDir = GravityDirection.Down;
        Physics2D.gravity = new Vector2(0.0f, -9.81f);
        player.UpdateJumpForce(GravityDirection.Down);

        // set position of each boundary
        Boundary[] bs = { boundaryDown, boundaryLeft, boundaryRight, boundaryUp };
        foreach (Boundary b in bs)
            b.SetBoundary();

        // set first checkpoint
        GameObject checkpoint = chkpntMap.transform.GetChild(0).gameObject;
        SetCheckpoint(checkpoint);
        SetCheckpointData(checkpoint.GetComponent<Checkpoint>().data);

        // intro
        player.gameObject.SetActive(false);
        Rigidbody2D rb2d = player.GetComponent<Rigidbody2D>();
        bool isIntroSpawn = true;
        StartCoroutine(ResetToCheckpoint(rb2d, isIntroSpawn));
    }

    void Update()
    {
        // escape key bails to MainMenu, for now
        if (Input.GetKeyDown(KeyCode.Escape))
            SceneManager.LoadScene(0);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using circleXsquares;

public partial class PlayGM : MonoBehaviour {

    // singleton instance
    [HideInInspector] public static PlayGM instance = null;

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
    public ChkpntData activeChkpnt { get; private set; }
    public int activeLayer { get; private set; }
    public GravityDirection gravDirection {
        get { return _gravDir; }
        set {}
    }
    public LevelData levelData { get; private set; }
    public bool victoryAchieved {get; private set;}

    // private constants
    private const int DEFAULT_LAYER = 0;
    private const int DEFAULT_NUM_LAYERS = 10;
    private const int INACTIVE_LAYER = 9;
    public static readonly string[] INT_TO_NAME = {
        "Zero",
        "One",
        "Two",
        "Three",
        "Four",
        "Five",
        "Six",
        "Seven",
        "Eight",
        "Nine"
    };

    // private references
    private PlayLoader _lvlLoad = null;

    // private variables
    private GravityDirection _gravDir;
    private HexOrient _playerStart;

    void Awake ()
    {
        if (!instance) {
            // set singleton instance, then references
            instance = this;
            _lvlLoad = GameObject.FindWithTag("Loader").GetComponent<PlayLoader>();
            player = GameObject.FindWithTag("Player").GetComponent<Player_Controller>();
            soundManager = GameObject.FindWithTag("SoundManager").GetComponent<SoundManager>();
        } else
            // if another singleton already exists, this one cannot
            Destroy(gameObject);
    }

    void Start ()
    {
        // load the level
        levelData = _lvlLoad.supplyLevel();
        buildLevel(levelData);

        // initialize variables
        activeLayer = 0;
        activateLayer(0);
        player.transform.position = _playerStart.locus.ToUnitySpace();
        _gravDir = GravityDirection.Down;
        victoryAchieved = false;

        // set position of each boundary
        Boundary[] bs = {boundaryDown, boundaryLeft, boundaryRight, boundaryUp};
        foreach (Boundary b in bs)
            b.SetBoundary();

        // set first checkpoint
        GameObject chkpnt = chkpntMap.transform.GetChild(0).gameObject;
        SetCheckpoint(chkpnt.GetComponent<Checkpoint>().data);
    }

    void Update ()
    {
        // escape key bails to MainMenu, for now
        if (Input.GetKeyDown(KeyCode.Escape))
            SceneManager.LoadScene(0);
    }
}

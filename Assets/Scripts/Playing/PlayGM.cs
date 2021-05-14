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

	// public GameObject or component script references
	public Boundary boundaryDown;
	public Boundary boundaryLeft;
	public Boundary boundaryRight;
	public Boundary boundaryUp;
	public GameObject chkpntRef;
	public GameObject chkpntMap;
	public GameObject victoryRef;
	public GameObject victoryMap;
	public GameObject deathParticles;
	public GameObject tileCreator;
	public GameObject tileMap;
	public Player_Controller player;
	public GameObject warpRef;
	public GameObject warpMap;

	// public sound manager
	public SoundManager soundManager;

	// public read-accessibility state variables
	public LevelData levelData { get; private set; }
	public ChkpntData activeChkpnt { get; private set; }
	public int activeLayer { get; private set; }
	public GravityDirection gravDirection {
		get { return grav_dir; }
		set {}
	}

	public bool VictoryAchieved {get; set;}

	// private variables
	private PlayLoader lvl_load = null;
	private HexOrient player_start;
	private GravityDirection grav_dir;

	// constants
	// default number of layers to load from file
	private const int DEFAULT_NUM_LAYERS = 10;
	public static readonly string[] INT_TO_NAME = { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine" };
	private const int INACTIVE_LAYER = 9;
	private const int DEFAULT_LAYER = 0;

	void Awake ()
	{
		if (!instance) {
			instance = this; // <1>
			lvl_load = GameObject.FindWithTag("Loader").GetComponent<PlayLoader>();
			player = GameObject.FindWithTag("Player").GetComponent<Player_Controller>();
			soundManager = GameObject.FindWithTag("SoundManager").GetComponent<SoundManager>();
		} else Destroy(gameObject); // <2>

		/*
		<1> set singleton instance
		<2> if another singleton already exists, this one cannot
		*/
	}

	void Start ()
	{
		levelData = lvl_load.supplyLevel(); // <1>
		buildLevel(levelData);

		VictoryAchieved = false;

		activeLayer = 0; // <2>
		activateLayer(0);

		Boundary[] bs = {boundaryDown, boundaryLeft, boundaryRight, boundaryUp};
		foreach (Boundary b in bs) b.SetBoundary();

		player.transform.position = player_start.locus.ToUnitySpace(); // <3>

		grav_dir = GravityDirection.Down;

		// set checkpoint
		GameObject chkpnt = chkpntMap.transform.GetChild(0).gameObject;
		SetCheckpoint(chkpnt.GetComponent<Checkpoint>().data);

		/*
		<1> load the level
		<2> set currently active layer
		<3> set player position
		*/
	}

	void Update ()
	{
		if (Input.GetKeyDown(KeyCode.Escape)) SceneManager.LoadScene(0); // <1>

		/*
		<1> escape key bails to MainMenu, for now
		*/
	}
}

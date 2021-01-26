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
	public GameObject boundaryDown;
	public GameObject boundaryLeft;
	public GameObject boundaryRight;
	public GameObject boundaryUp;
	public GameObject checkpointRef;
	public GameObject deathParticles;
	public GameObject tileCreator;
	public GameObject tileMap;
	public GameObject player;
	public GameObject warpRef;

	// public read-accessibility state variables
	public GameObject currentCheckpoint { get; private set; }
	public int currentLayer { get; private set; }
	public LevelData levelData { get; private set; }

	// private variables
	private PlayLoader lvl_load = null;
	private HexOrient playerStart;

	void Awake ()
	{
		if (!instance) {
			instance = this; // <1>
			lvl_load = GameObject.FindWithTag("Loader").GetComponent<PlayLoader>();
			player = GameObject.FindWithTag("Player");
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

		currentLayer = 0; // <2>
		activateLayer(0);

		// set boundaries

		player.transform.position = playerStart.locus.ToUnitySpace(); // <3>

		// set checkpoint
		// SetCheckpoint(Instantiate(checkpointRef, v2, Quaternion.identity) as GameObject);

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
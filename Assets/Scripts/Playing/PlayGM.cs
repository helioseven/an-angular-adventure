using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using circleXsquares;

public class PlayGM : MonoBehaviour {

	// singleton instance
	[HideInInspector] public static PlayGM instance = null;

	public GameObject boundary_down;
	public GameObject boundary_left;
	public GameObject boundary_right;
	public GameObject boundary_up;
	public GameObject checkpointRef;
	public GameObject deathParticles;
	public GameObject player;
	public GameObject warpRef;

	private PlayLoader lvl_load = null;
	public GameObject tileMap;

	public LevelData levelData { get; private set; }
	public GameObject currentCheckpoint { get; private set; }
	public int currentLayer { get; private set; }

	void Awake ()
	{
		if (!instance) {
			// set singleton instance
			instance = this;
			// find the loader
			lvl_load = GameObject.FindWithTag("Loader").GetComponent<PlayLoader>();
			// find the player
			player = GameObject.FindWithTag("Player");
		} else
			Destroy(gameObject);
	}

	void Start ()
	{
		// load the level
		Vector2 v2;
		LevelData inLvl;
		lvl_load.supplyLevel(ref tileMap, out inLvl, out v2);
		levelData = inLvl;

		// set layer activity
		currentLayer = 0;
		ActivateLayer(0);

		// set boundaries

		// set player position
		player.transform.position = v2;

		// set checkpoint
		SetCheckpoint(Instantiate(checkpointRef, v2, Quaternion.identity) as GameObject);
	}

	void Update ()
	{
		// this should only be temporary, but Escape key bails to MainMenu
		if (Input.GetKeyDown(KeyCode.Escape)) SceneManager.LoadScene(0);
	}

	public void KillPlayer ()
	{
		player.SetActive(false);
		Vector3 p = player.transform.position;
		UnityEngine.Object dp = Instantiate(deathParticles, p, Quaternion.identity);
		Invoke("ResetToCheckpoint", 1f);
		Destroy(dp, 1.0f);
	}

	public void Reset ()
	{
		foreach (Transform layer in tileMap.transform)
			foreach (Transform tile in layer) DontDestroyOnLoad(tile.gameObject);

		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
	}

	public void SetCheckpoint (GameObject checkpoint)
	{
		currentCheckpoint = checkpoint;
	}

	public void ResetToCheckpoint ()
	{
		// move player to last checkpoint
		player.transform.position = currentCheckpoint.transform.position;
		// acivate
		player.SetActive(true);
	}

	public void WarpPlayer (Warp warp)
	{
		// if player's current level matches either base or target, select the other
		int next_layer;
		if (currentLayer == warp.baseLayer)
			next_layer = warp.targetLayer;
		else if (currentLayer == warp.targetLayer)
			next_layer = warp.baseLayer;
		else
			next_layer = currentLayer;

		// change layers & transparency for base and target layers
		ActivateLayer(next_layer);

		// change players position and current_level
		Vector3 p = player.transform.position;
		p.z = tileMap.transform.GetChild(next_layer).position.z;
		player.transform.position = p;
		currentLayer = next_layer;
	}

	public void ActivateLayer (int layerIndex)
	{
		// simply cycles through all layers and calls SetLayerOpacity appropriately
		foreach (Transform tileLayer in tileMap.transform) {
			int d = tileLayer.GetSiblingIndex();
			d = Math.Abs(d - layerIndex);
			SetLayerOpacity(tileLayer, d);
		}
	}

	private void SetLayerOpacity (Transform tileLayer, int distance)
	{
		// a represents an alpha value
		float a = 1f;
		// the physics layer we will be setting
		int l = 0;
		if (distance != 0) {
			a = 1f / (distance + 1f);
			l = 9;
		}
		Color c = new Color(1f, 1f, 1f, a);

		foreach (Transform tile in tileLayer) {
			tile.gameObject.layer = l;
			tile.GetChild(0).GetComponent<SpriteRenderer>().color = c;
		}
	}
}
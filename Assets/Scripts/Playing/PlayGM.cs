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

	private PlayLoader lvlLoad = null;
	public GameObject tile_map;

	public levelData lvlData { get; private set; }
	public GameObject player { get; private set; }
	public GameObject curr_checkpoint { get; private set; }
	public int curr_layer { get; private set; }

	public GameObject player_ref;
	public GameObject death_particles;
	public GameObject checkpoint_ref;
	public GameObject warp_ref;

	void Awake ()
	{
		if (!instance) {
			// set singleton instance
			instance = this;
			// find the loader
			lvlLoad = GameObject.FindWithTag("Loader").GetComponent<PlayLoader>();
			// load the level
			Vector2 v2;
			levelData inLvl;
			lvlLoad.supplyLevel(ref tile_map, out inLvl, out v2);
			lvlData = inLvl;
			// set layer activity
			ActivateLayer(0);
			// set checkpoint
			SetCheckpoint(Instantiate(checkpoint_ref, v2, Quaternion.identity) as GameObject);
			// instantiate player
			player = Instantiate(player_ref, v2, Quaternion.identity) as GameObject;
			curr_layer = 0;
		} else
			Destroy(gameObject);
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
		UnityEngine.Object dp = Instantiate(death_particles, p, Quaternion.identity);
		Invoke("ResetToCheckpoint", 1f);
		Destroy(dp, 1.0f);
	}

	public void Reset ()
	{
		foreach (Transform layer in tile_map.transform)
			foreach (Transform tile in layer) DontDestroyOnLoad(tile.gameObject);

		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
	}

	public void SetCheckpoint (GameObject checkpoint)
	{
		curr_checkpoint = checkpoint;
	}

	public void ResetToCheckpoint ()
	{
		// move player to last checkpoint
		player.transform.position = curr_checkpoint.transform.position;
		// acivate
		player.SetActive(true);
	}

	public void WarpPlayer (Warp warp)
	{
		// if player's current level matches either base or target, select the other
		int next_layer;
		if (curr_layer == warp.base_level)
			next_layer = warp.target_level;
		else if (curr_layer == warp.target_level)
			next_layer = warp.base_level;
		else
			next_layer = curr_layer;

		// change layers & transparency for base and target layers
		ActivateLayer(next_layer);

		// change players position and current_level
		Vector3 p = player.transform.position;
		p.z = tile_map.transform.GetChild(next_layer).position.z;
		player.transform.position = p;
		curr_layer = next_layer;
	}

	public void ActivateLayer (int layerIndex)
	{
		// simply cycles through all layers and calls SetLayerOpacity appropriately
		foreach (Transform tileLayer in tile_map.transform) {
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
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

	public GameObject player_ref;
	public GameObject death_particles;
	public GameObject checkpoint_ref;
	public GameObject warp_ref;

	private Player_Controller pc_ref;

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
			foreach (Transform tile_layer in tile_map.transform) {
				if (tile_layer.GetSiblingIndex() == 0)
					continue;
				ActivateLayer(tile_layer.GetSiblingIndex(), false);
			}
			// set checkpoint
			SetCheckpoint(Instantiate(checkpoint_ref, v2, Quaternion.identity) as GameObject);
			// instantiate player
			player = Instantiate(player_ref, v2, Quaternion.identity) as GameObject;
			pc_ref = player.GetComponent<Player_Controller>();
		} else
			Destroy(gameObject);
	}

	void Update ()
	{
		// this should only be temporary, but Escape key bails to MainMenu
		if (Input.GetKeyDown(KeyCode.Escape)) SceneManager.LoadScene(0);
	}

	public void SetCheckpoint (GameObject checkpoint)
	{
		curr_checkpoint = checkpoint;
	}

	public void ResetToCheckpoint ()
	{
		// reset player to last checkpoint's layer
		player.layer = curr_checkpoint.layer;
		// move player to last checkpoint
		player.transform.position = curr_checkpoint.transform.position;
		// acivate
		player.SetActive(true);
	}

	public void WarpPlayer (Warp activated)
	{
		// if player's current level matches either base or target, select the other
		int next_level;
		if (pc_ref.current_level == activated.base_level)
			next_level = activated.target_level;
		else if (pc_ref.current_level == activated.target_level)
			next_level = activated.base_level;
		else
			next_level = pc_ref.current_level;

		// change layers & transparency for base and target layers
		ActivateLayer(pc_ref.current_level, false);
		ActivateLayer(next_level, true);

		// change players position and current_level
		Vector3 p = player.transform.position;
		p.z = tile_map.transform.GetChild(next_level).position.z;
		player.transform.position = p;
		pc_ref.current_level = next_level;
	}

	public void ActivateLayer (int layerIndex, bool turningOn)
	{
		// first, get a reference to layer by given index
		Transform tileLayer = tile_map.transform.GetChild(layerIndex);
		if (turningOn) {
			// layer 0 (default) and full transparency is on
			tileLayer.gameObject.layer = 0;
			foreach (Transform tile in tileLayer) {
				tile.gameObject.layer = 0;
				Color c = new Color(1f, 1f, 1f, 1f);
				tile.GetChild(0).GetComponent<SpriteRenderer>().color = c;
			}
		} else {
			// layer 9 (inactive) and 40% transparency is off
			tileLayer.gameObject.layer = 9;
			foreach (Transform tile in tileLayer) {
				tile.gameObject.layer = 9;
				Color c = new Color(1f, 1f, 1f, 0.4f);
				tile.GetChild(0).GetComponent<SpriteRenderer>().color = c;
			}
		}
	}

	public void KillPlayer ()
	{
		player.SetActive(false);
		Object dp = Instantiate(death_particles, player.transform.position, Quaternion.identity);
		Invoke("ResetToCheckpoint", 1f);
		Destroy(dp, 1.0f);
	}

	public void Reset ()
	{
		foreach (Transform layer in tile_map.transform)
			foreach (Transform tile in layer) DontDestroyOnLoad(tile.gameObject);

		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
	}
}
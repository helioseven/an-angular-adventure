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
	private HashSet<GameObject> lvlTiles;

	public levelData lvlData { get; private set; }
	public GameObject player { get; private set; }
	public GameObject curr_checkpoint { get; private set; }

	public GameObject player_ref;
	public GameObject death_particles;
	public GameObject checkpoint_ref;

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
			lvlLoad.supplyLevel(out lvlTiles, out inLvl, out v2);
			lvlData = inLvl;
			// set checkpoint
			SetCheckPoint(Instantiate(checkpoint_ref, v2, Quaternion.identity) as GameObject);
			// instantiate player
			player = Instantiate(player_ref, v2, Quaternion.identity) as GameObject;
		} else
			Destroy (gameObject);
	}

	void Update ()
	{
		// this should only be temporary, but Escape key bails to MainMenu
		if (Input.GetKeyDown(KeyCode.Escape)) SceneManager.LoadScene(0);
	}

	public void SetCheckPoint (GameObject checkpoint)
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

	public void KillPlayer ()
	{
		player.SetActive(false);
		Instantiate (death_particles, player.transform.position, Quaternion.identity);
		Invoke("ResetToCheckpoint", 1.0f);
	}

	public void Reset ()
	{
		foreach (GameObject go in lvlTiles) DontDestroyOnLoad(go);
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
	}
}
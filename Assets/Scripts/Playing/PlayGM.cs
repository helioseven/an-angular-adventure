using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class PlayGM : MonoBehaviour {

	// singleton instance
	[HideInInspector]
	public static PlayGM instance = null;
	private PlayLoader lvlLoad = null;
	private HashSet<GameObject> lvlTiles;

	public GameObject pRef;
	public GameObject player;
	public GameObject death_particles;

	public GameObject curr_checkpoint;

	void Awake () 
	{
		if (!instance) {
			// set singleton instance
			instance = this;
			// find the loader
			lvlLoad = GameObject.FindWithTag("Loader").GetComponent<PlayLoader>();
			Vector2 v2;
			// load the level
			lvlLoad.supplyLevel(out lvlTiles, out v2);
			// instantiate player
			player = Instantiate(pRef, v2, Quaternion.identity) as GameObject;
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
		// log
		Debug.Log("Player Respawn at Checkpoint");
		// acivate
		player.SetActive(true);
		// layer
		player.layer = curr_checkpoint.layer;
		//
		player.transform.position = curr_checkpoint.transform.position;
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
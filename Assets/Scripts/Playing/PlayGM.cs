using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayGM : MonoBehaviour {

	// singleton instance
	[HideInInspector] public static PlayGM instance = null;
	private PlayLoader lvlLoad = null;
	private HashSet<GameObject> lvlTiles;

	private Vector2 pV2;
	public GameObject player_ref;
	public GameObject death_particles;
	public GameObject checkpoint_ref;

	public GameObject player { get; private set; }
	public GameObject curr_checkpoint { get; private set; }

	void Awake ()
	{
		if (!instance) {
			// set singleton instance
			instance = this;
			// find the loader
			lvlLoad = GameObject.FindWithTag("Loader").GetComponent<PlayLoader>();
			// load the level
			StartCoroutine(loadLevel());
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
//		Debug.Log("Player Respawn at Checkpoint (" + GetInstanceID() + ")");
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

	// (??)
	private IEnumerator loadLevel ()
	{
		// need to wait until the asset is downloaded
		while(!lvlLoad.isFileFound) yield return new WaitForSeconds(0.1f);

		// level is loaded from file
		lvlLoad.supplyLevel(out lvlTiles, out pV2);
		// instantiate and set checkpoint
		SetCheckPoint(Instantiate(checkpoint_ref, pV2, Quaternion.identity) as GameObject);
		// instantiate player
		player = Instantiate(player_ref, pV2, Quaternion.identity) as GameObject;
	}
}
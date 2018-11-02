using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GM : MonoBehaviour {


	//  instance
	[HideInInspector]
	public static GM instance = null;

	public GameObject player;
	public GameObject death_particles;

	private int coins = 0;
	public Text coinsText;

	public GameObject curr_checkpoint;
//	private Player_Controller player_controller;

	// Use this for initialization
	void Awake () 
	{
		if (!instance)
			instance = this;
		else
			Destroy (gameObject);
		
		Setup ();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void SetCheckPoint( GameObject checkpoint )
	{
		curr_checkpoint = checkpoint;
	}

	public void addCoin(){
		coins++;
		coinsText.text = "Coins: " + coins;
	}

	public void ResetToCheckpoint()
	{
		// acivate
		player.SetActive (true);
		// layer
		GM.instance.player.layer = GM.instance.curr_checkpoint.layer;
		// update position
		player.transform.position = curr_checkpoint.transform.position;
	}

	public void KillPlayer(){
		player.SetActive(false);
		Instantiate (death_particles, player.transform.position, Quaternion.identity);
		Invoke ("ResetToCheckpoint", 1.0f);
	}

	public void Reset()
	{
		SceneManager.LoadScene(0);
	}

}

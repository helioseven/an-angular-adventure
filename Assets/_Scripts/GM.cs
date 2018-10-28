using UnityEngine;
using System.Collections;
using UnityEngine.UI; // for UI text

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
	
	public void Setup()
	{
//		player_controller = FindObjectOfType<Player_Controller> ();
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
		// log
		Debug.Log ("Player Respawn at Checkpoint");
		// acivate
		player.SetActive (true);
		// layer
		GM.instance.player.layer = GM.instance.curr_checkpoint.layer;
		//
		player.transform.position = curr_checkpoint.transform.position;
	}

	public void KillPlayer(){
		player.SetActive(false);
		Instantiate (death_particles, player.transform.position, Quaternion.identity);
		Invoke ("ResetToCheckpoint", 1.0f);
	}

	public void Reset()
	{
		Application.LoadLevel(Application.loadedLevel);
	}

}

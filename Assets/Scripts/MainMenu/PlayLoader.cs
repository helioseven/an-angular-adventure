using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using circleXsquares;

public class PlayLoader : MonoBehaviour {

	// short-form pathname for the level to be loaded
	private string path;
	// 6x7 array of prefab references
	private GameObject[,] prefabRefs;

	// prefab references
	// very crude
	// (!!) changes needed
	public GameObject dia_black;
	public GameObject dia_blue;
	public GameObject dia_brown;
	public GameObject dia_green;
	public GameObject dia_orange;
	public GameObject dia_purple;
	public GameObject dia_red;
	public GameObject hex_black;
	public GameObject hex_blue;
	public GameObject hex_brown;
	public GameObject hex_green;
	public GameObject hex_orange;
	public GameObject hex_purple;
	public GameObject hex_red;
	public GameObject sqr_black;
	public GameObject sqr_blue;
	public GameObject sqr_brown;
	public GameObject sqr_green;
	public GameObject sqr_orange;
	public GameObject sqr_purple;
	public GameObject sqr_red;
	public GameObject trap_black;
	public GameObject trap_blue;
	public GameObject trap_brown;
	public GameObject trap_green;
	public GameObject trap_orange;
	public GameObject trap_purple;
	public GameObject trap_red;
	public GameObject tri_black;
	public GameObject tri_blue;
	public GameObject tri_brown;
	public GameObject tri_green;
	public GameObject tri_orange;
	public GameObject tri_purple;
	public GameObject tri_red;
	public GameObject wed_black;
	public GameObject wed_blue;
	public GameObject wed_brown;
	public GameObject wed_green;
	public GameObject wed_orange;
	public GameObject wed_purple;
	public GameObject wed_red;

	void Awake ()
	{
		// crude
		// (!!) changes needed
		prefabRefs = new GameObject[6,7] {
			{tri_black, tri_blue, tri_brown, tri_green, tri_orange, tri_purple, tri_red},
			{dia_black, dia_blue, dia_brown, dia_green, dia_orange, dia_purple, dia_red},
			{trap_black, trap_blue, trap_brown, trap_green, trap_orange, trap_purple, trap_red},
			{hex_black, hex_blue, hex_brown, hex_green, hex_orange, hex_purple, hex_red},
			{sqr_black, sqr_blue, sqr_brown, sqr_green, sqr_orange, sqr_purple, sqr_red},
			{wed_black, wed_blue, wed_brown, wed_green, wed_orange, wed_purple, wed_red},
		};

		// filepath of level to be loaded
		// (!) currently just change the string and recompile :|
		// (!!) prompt for string instead
		path = "testLevel.txt";
		DontDestroyOnLoad(gameObject);
		// load Playing scene (PlayGM will call supplyLevel)
		SceneManager.LoadScene(1);
	}

	// supplies a HashSet of tiles representing the level, and a Vector2 representing a starting location
	public void supplyLevel (out HashSet<GameObject> level, out Vector2 playerStart)
	{
		// initialization
		level = new HashSet<GameObject>();
		playerStart = Vector2.zero;
		string[] lines = File.ReadAllLines("Assets\\Levels\\" + path);

		// begin parsing file and building level
		if (lines.Length < 3) {
			Debug.LogError("File could not be read correctly.");
			return;
		}

		// the first line of the file is for comments and is ignored
		// the second line of the file represents the player location
		string[] pVals = lines[1].Split(new Char[] {' '});
		hexLocus pHL = new hexLocus(
			Int32.Parse(pVals[0]),
			Int32.Parse(pVals[1]),
			Int32.Parse(pVals[2]),
			Int32.Parse(pVals[3]),
			Int32.Parse(pVals[4]),
			Int32.Parse(pVals[5]));
		playerStart = pHL.toUnitySpace();

		// after the first two lines of the file, all remaining lines represent tiles
		for (int i = 2; i < lines.Length; i++) {
			string[] vals = lines[i].Split(new Char[] {' '});
			int j = Int32.Parse(vals[0]);
			int k = Int32.Parse(vals[1]);
			hexLocus hl = new hexLocus(
				Int32.Parse(vals[2]),
				Int32.Parse(vals[3]),
				Int32.Parse(vals[4]),
				Int32.Parse(vals[5]),
				Int32.Parse(vals[6]),
				Int32.Parse(vals[7]));
			int r = Int32.Parse(vals[8]);

			GameObject go = Instantiate(prefabRefs[j,k], hl.toUnitySpace(), Quaternion.Euler(0, 0, 30 * r)) as GameObject;
			level.Add(go);
		}

		// terminates this script when done
		Destroy(gameObject);
	}
}
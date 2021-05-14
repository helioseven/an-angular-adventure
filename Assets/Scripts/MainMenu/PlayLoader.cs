using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using circleXsquares;

public class PlayLoader : MonoBehaviour {

	// public read-accessibility state variables
	public string levelName { get; private set; }

	// private variables
	private string path;

	void Awake ()
	{
		levelName = "testLevel"; // <1>
		string filename = levelName + ".txt";
		path = Path.Combine(new string[]{"Levels", filename});
		DontDestroyOnLoad(gameObject); // <2>
		SceneManager.LoadScene(1); // <3>

		/*
		<1> levelName is hard coded (!!), should be prompted
		<2> this loader stays awake when next scene is loaded
		<3> load Editing scene (EditGM will call supplyLevel)
		*/
	}

	// web build only - build from string array directly
	public LevelData supplyLevel ()
	{
		string[] lines = new string[]{"-- level comments goes here --",
            "-- player start info goes here --",
            " ",
            "-- Tiles --",
            "5 3 222 -2 0 0 0 0 2 11 0",
            "4 0 0 -2 0 0 0 0 0 9 0",
            "3 0 222 2 0 0 0 0 0 9 0",
            "1 4 3 2 2 0 0 0 -2 7 0",
            "1 4 0 2 4 0 -2 0 0 7 0",
            "2 6 0 2 2 0 -4 0 0 7 0",
            "4 1 0 4 2 0 -4 0 0 6 0",
            "4 1 0 4 2 0 -4 0 0 9 0",
            "0 1 0 2 4 0 -2 0 0 0 0",
            "5 1 0 2 2 0 -2 0 0 1 0",
            "1 0 444 2 4 0 -2 -2 0 10 0",
            "2 0 0 6 2 0 -8 0 0 5 0",
            "5 0 0 8 2 0 -8 0 0 5 0",
            "0 0 0 -2 0 0 0 0 0 7 0",
            "4 2 0 -2 0 2 0 0 0 7 0",
            "4 6 0 0 0 0 -2 0 2 6 0",
            "0 6 0 0 0 0 -2 0 4 6 0",
            "0 6 0 2 0 0 -2 0 4 10 0",
            "1 2 0 2 0 -2 -2 0 4 0 0",
            "4 3 444 4 0 -2 -4 0 4 2 0",
            "5 0 0 -2 0 0 0 0 2 11 1",
            "4 0 0 -2 0 0 0 0 0 9 1",
            "1 4 1 4 0 -2 -4 0 4 0 0",
            "3 2 0 6 0 -4 -4 0 4 0 0",
            "4 1 0 10 0 0 -4 0 4 10 0",
            "1 1 0 10 0 0 -6 0 2 10 0",
            "0 5 0 12 0 0 -6 0 2 10 0",
            "5 5 0 14 0 0 -8 0 0 7 0",
            "2 6 0 14 2 0 -4 0 0 9 0",
            "2 0 0 4 0 -2 -4 0 4 0 1",
            "2 5 0 2 0 0 -4 -2 4 10 1",
            "4 6 0 8 0 -2 -4 0 6 0 1",
            "3 5 0 0 0 -2 -4 0 4 0 1",
            "3 1 0 0 0 -2 -2 2 4 0 0",
            "4 6 0 6 0 -2 -4 0 4 9 1",
            "5 6 0 0 0 0 -2 0 4 2 1",
            "4 4 2 0 0 0 -2 0 4 11 1",
            "4 5 0 0 0 0 -2 0 2 6 1",
            "0 2 0 12 2 0 -4 -2 0 10 0",
            "4 5 0 14 2 0 -4 -2 0 3 0",
            "4 6 0 14 2 0 -6 0 0 2 0",
            "5 0 0 -2 0 0 0 0 2 11 2",
            "5 0 0 -2 0 0 0 0 2 11 3",
            "1 0 0 -2 -2 0 0 0 2 11 2",
            "5 5 0 -2 0 0 0 0 2 6 2",
            "2 0 0 -4 0 0 2 0 0 9 2",
            "1 6 0 -4 0 0 4 0 0 9 2",
            "1 1 0 -2 0 0 -2 0 2 10 2",
            "2 1 0 0 0 -2 -2 2 2 0 2",
            "0 1 0 0 0 0 -2 2 2 10 2",
            "0 1 0 2 0 -4 -2 0 2 2 2",
            "1 1 0 2 0 -2 -2 0 2 0 2",
            "3 0 0 2 0 -4 -2 0 2 0 3",
            "2 0 0 4 0 0 -2 0 2 6 3",
            "1 0 0 0 0 -2 -2 0 2 4 3",
            "0 7 0 -2 0 4 0 0 -2 10 0",
            "4 0 0 0 0 0 -4 -2 2 0 1",
            "1 0 0 2 0 0 -4 -2 2 1 1",
            "0 0 0 2 0 0 -6 -2 0 1 1",
            "4 0 0 0 0 0 -4 -2 0 0 1",
            "5 0 0 0 2 0 0 -2 0 11 1",
            "2 4 0 2 2 0 0 -2 0 4 1",
            "-- End Tiles --",
            " ",
            "-- Checkpoints --",
            "-1 0 0 0 0 -2 0",
            "10 0 0 0 0 0 0",
            "0 0 1 0 -1 0 1",
            "-- End Checkpoints --",
            " ",
			"-- Victories --",
            "-4 0 0 -1 0 -2 3",
            "-- End Victories --",
            " ",
            "-- Warps --",
            "0 1 6 0 0 -4 0 4 0 0 1",
            "0 1 -4 0 0 0 0 2 0 1 2",
            "0 1 4 0 0 -2 0 2 0 2 3",
            "-- End Warps --"};
        LevelData ld = FileParsing.ReadLevel(lines);


        Destroy(gameObject);
        return ld;

	}
}


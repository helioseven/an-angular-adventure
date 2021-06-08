using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using circleXsquares;

public class EditLoader : MonoBehaviour {

	// public read-accessibility state variables
	public string levelName { get; private set; }

	// private variables
	private string path;

	void Awake ()
	{
		levelName = "jip"; // <1>
		string filename = levelName + ".txt";
		path = Path.Combine(new string[]{"Levels", filename});
		DontDestroyOnLoad(gameObject); // <2>
		SceneManager.LoadScene(2); // <3>

		/*
		<1> levelName is hard coded (!!), should be prompted
		<2> this loader stays awake when next scene is loaded
		<3> load Editing scene (EditGM will call supplyLevel)
		*/
	}

	// supplies a levelData from file
	public LevelData supplyLevel ()
	{
		bool file_exists = File.Exists(path); // <1>
		LevelData ld;
		if (file_exists) {
			string[] lines = File.ReadAllLines(path);
			ld = FileParsing.ReadLevel(lines); // <2>
		} else {
			Debug.LogError("File not found, loading new level.");
			ld = new LevelData(); // <3>
		}

		Destroy(gameObject); // <4>
		return ld;

		/*
		<1> first, check to see whether the file exists
		<2> if file exists, it is loaded and parsed
		<3> if file doesn't exist, empty level is created
		<4> when script is done, it schedules self-termination and returns
		*/
	}
}

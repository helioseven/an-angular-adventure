using System.Collections;
using System.Collections.Generic;
using System.IO;
using circleXsquares;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EditLoader : MonoBehaviour
{
    // public read-accessibility state variables
    public string levelName { get; private set; }

    // private variables
    private string path;

    void Awake()
    {
        // levelName is hard coded (!!), should be prompted
        levelName = "testLevel";
        string filename = levelName + ".txt";
        path = Path.Combine(new string[] { "Levels", filename });
        // this loader stays awake when next scene is loaded
        DontDestroyOnLoad(gameObject);
        // load Editing scene (EditGM will call supplyLevel)
        SceneManager.LoadScene(2);
    }

    /* Public Functions */

    // supplies a levelData from file
    public LevelData supplyLevel()
    {
        // first, check to see whether the file exists
        bool file_exists = File.Exists(path);
        LevelData ld;
        if (file_exists)
        {
            // if file exists, it is loaded and parsed
            string[] lines = File.ReadAllLines(path);
            ld = FileParsing.ReadLevel(lines);
        }
        else
        {
            // if file doesn't exist, empty level is created
            Debug.LogError("File not found, loading new level.");
            ld = new LevelData();
        }

        // when script is done, it schedules self-termination and returns
        Destroy(gameObject);
        return ld;
    }
}

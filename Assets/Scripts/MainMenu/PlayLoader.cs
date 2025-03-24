using System.Collections;
using System.Collections.Generic;
using System.IO;
using circleXsquares;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayLoader : MonoBehaviour
{
    // public read-accessibility state variables
    public string levelName;

    // private variables
    private string path;

    private string[] supabaseLevelPayloadData;

    private LevelData levelData = new LevelData();

    private bool levelReady = false;

    void Start()
    {
        if (string.IsNullOrEmpty(levelName))
        {
            Debug.LogError("[PlayLoader] No level ID set!");
            return;
        }

        // set the path
        string levelsFolder = LevelStorage.LevelsFolder;
        path = Path.Combine(levelsFolder, $"{levelName}.json");

        // this loader stays awake when next scene is loaded
        DontDestroyOnLoad(gameObject);

        // Supabase - switch this to flip the script
        bool loadFromSupabase = false;

        // Supabase - hardcoded test level id
        string supabaseTestLevelId = "7bf4ff67-d3b6-4c60-ab96-0166daa439dc";

        // first, check to see whether the file exists
        bool file_exists = File.Exists(path);

        if (loadFromSupabase)
        {
            SupabaseEditController.Instance.StartCoroutine(SupabaseEditController.Instance.LoadLevel(supabaseTestLevelId, GetLevelFromPayload));
        }
        else
        {
            if (file_exists)
            {
                string json = File.ReadAllText(path);
                var levelDTO = JsonUtility.FromJson<SupabaseLevelDTO>(json); // See below
                supabaseLevelPayloadData = levelDTO.data;

                levelData = LevelLoader.LoadLevel(supabaseLevelPayloadData);
                levelReady = true;
            }
            else
            {
                // if file doesn't exist, empty level is created
                Debug.LogError("File not found, loading empty level.");
            }
        }

    }

    void Update()
    {
        if (levelReady)
        {
            // load Play scene (PlayGM will call supplyLevel)
            SceneManager.LoadScene(1);

            // only do this once
            levelReady = false;
        }

    }

    /* Public Functions */
    // Supabase - callback function after loading
    public void GetLevelFromPayload(SupabaseLevelDTO payload)
    {
        supabaseLevelPayloadData = payload.data;
        Debug.Log("Got level: " + payload.name);

        levelData = LevelLoader.LoadLevel(supabaseLevelPayloadData);
        levelReady = true;
    }

    // Supplies a LevelData
    public LevelData supplyLevel()
    {
        // when script is done, it schedules self-termination and returns
        Destroy(gameObject);
        return levelData;
    }
}

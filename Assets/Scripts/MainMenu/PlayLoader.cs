using System.Collections;
using System.Collections.Generic;
using System.IO;
using circleXsquares;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayLoader : MonoBehaviour
{
    // Public variables
    // The basic human readable level name
    public string levelName;
    // Level Id
    public string id;
    // Cloud load flag - fetch from Supabase instead of local
    public bool loadFromSupabase = false;

    // Private variables

    // This path is the local file (if it's a "Draft") - built from levelName
    private string path;
    // String array representation of the payload data
    private string[] supabaseLevelPayloadData;
    // Built level data that needs to be handed off in the end
    private LevelData levelData = new LevelData();
    // Once this flips to true we hand off to the play scene
    private bool levelReady = false;

    void Start()
    {
        if (string.IsNullOrEmpty(levelName))
        {
            Debug.LogError("[PlayLoader] No level Name set!");
            return;
        }

        // set the path
        string levelsFolder = LevelStorage.LevelsFolder;
        path = Path.Combine(levelsFolder, $"{levelName}.json");

        // this loader stays awake when next scene is loaded
        DontDestroyOnLoad(gameObject);

        // Supabase - hardcoded test level id
        // string supabaseTestLevelId = "7bf4ff67-d3b6-4c60-ab96-0166daa439dc";

        if (loadFromSupabase)
        {
            SupabaseEditController.Instance.StartCoroutine(SupabaseEditController.Instance.LoadLevel(id, GetLevelFromPayload));
        }
        else
        {
            // first, check to see whether the file exists
            bool file_exists = File.Exists(path);

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

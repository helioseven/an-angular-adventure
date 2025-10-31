using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using circleXsquares;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EditLoader : MonoBehaviour
{
    // Public variables
    // The basic human readable level name
    public string levelName = "";

    // Level Id
    public string supabase_uuid;

    // Cloud load flag - fetch from Supabase instead of local
    public bool loadFromSupabase = false;
    public LevelInfo levelInfo;

    // private variables
    private string path;

    // String array representation of the payload data
    private string[] supabaseLevelPayloadData;

    // Built level data that needs to be handed off in the end
    private LevelData levelData = new LevelData();

    // Once this flips to true we hand off to the play scene
    private bool levelReady = false;

    void Start()
    {
        levelName = levelInfo.name;
        supabase_uuid = levelInfo.id; // note this might not be a supabase level
        loadFromSupabase = !levelInfo.isLocal;

        // this loader stays awake when next scene is loaded
        DontDestroyOnLoad(gameObject);

        if (string.IsNullOrEmpty(levelName))
        {
            Debug.Log("[EditLoader] No level Name set! - Will load defaultCreateLevelData");

            string[] defaultCreateLevelData = new string[]
            {
                "-- Tiles --",
                "-- End Tiles --",
                " ",
                "-- Checkpoints --",
                "0 0 0 0 0 0 0",
                "-- End Checkpoints --",
                " ",
                "-- Victories --",
                "2 2 0 0 0 0 0",
                "-- End Victories --",
                " ",
                "-- Warps --",
                "-- End Warps --",
            };

            Debug.Log("defaultCreateLevelData: " + defaultCreateLevelData);
            levelData = LevelLoader.LoadLevel(defaultCreateLevelData);

            // set level info to dummy default level info
            levelInfo = new LevelInfo
            {
                id = "",
                name = "defaultCreateLevelFromMainMenu",
                isLocal = true,
                uploaderId = "",
                createdAt = DateTime.MinValue,
            };

            levelReady = true;
            return;
        }

        // first, check to see whether the folder exists
        if (!Directory.Exists(LevelStorage.LevelsFolder))
            Directory.CreateDirectory(LevelStorage.LevelsFolder);
        // set the path
        path = Path.Combine(LevelStorage.LevelsFolder, $"{levelName}.json");

        if (loadFromSupabase)
        {
            SupabaseController.Instance.StartCoroutine(
                SupabaseController.Instance.LoadLevel(supabase_uuid, GetLevelFromPayload)
            );
        }
        else
        {
            // then, check to see whether the file exists
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
            // load Editing scene (EditGM will call supplyLevel)
            SceneManager.LoadScene("Editing");

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

    // supplies a LevelData
    public LevelData supplyLevel()
    {
        // when script is done, it schedules self-termination and returns
        Destroy(gameObject);
        return levelData;
    }
}

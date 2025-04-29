using System;
using System.IO;
using circleXsquares;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PlayGM;

public class PlayLoader : MonoBehaviour
{
    // Public variables
    // The basic human readable level name
    public string levelName;

    // Level Id (From Supabase)
    public string supabase_uuid;

    // Cloud load flag - fetch from Supabase instead of local
    public bool loadFromSupabase = false;

    public LevelInfo levelInfo;

    public PlayModeContext playModeContext = PlayModeContext.FromMainMenuPlayButton;

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
        levelName = levelInfo.name;
        supabase_uuid = levelInfo.id; // note this might not be a supabase level
        loadFromSupabase = !levelInfo.isLocal;

        // this loader stays awake when next scene is loaded
        DontDestroyOnLoad(gameObject);

        if (string.IsNullOrEmpty(levelName))
        {
            Debug.Log(
                "[PlayLoader] No level Name set! - Will load defaultPlayTesselationFromMainMenu"
            );

            string[] defaultPlayTesselationFromMainMenu = new string[]
            {
                "-- level comments goes here --",
                "-- player start info goes here --",
                " ",
                "-- Tiles --",
                "0 0 0 0 -4 0 0 0 1 10 0 0",
                "0 0 0 2 -4 0 0 0 1 10 0 0",
                "0 0 0 4 -4 0 0 0 1 10 0 0",
                "0 0 0 6 -4 0 0 0 1 10 0 0",
                "0 0 0 2 -4 0 0 0 1 8 0 0",
                "0 0 0 4 -4 0 0 0 1 8 0 0",
                "0 0 0 6 -4 0 0 0 1 8 0 0",
                "0 0 0 8 -4 0 0 0 1 8 0 0",
                "0 0 0 0 -4 0 0 0 1 8 0 0",
                "3 0 0 -2 -4 2 0 0 1 8 0 0",
                "3 0 0 0 -4 6 0 0 1 8 0 0",
                "3 0 0 0 -4 8 0 -2 1 8 0 0",
                "3 0 0 0 -4 6 0 -6 1 8 0 0",
                "3 0 0 0 -4 4 0 -10 1 8 0 0",
                "3 0 0 0 -4 0 0 -12 1 8 0 0",
                "3 0 0 2 -4 0 0 -14 1 8 0 0",
                "3 0 0 6 -4 0 0 -12 1 8 0 0",
                "0 0 0 8 -4 0 0 0 1 10 0 0",
                "0 0 0 10 -4 0 0 0 1 10 0 0",
                "0 0 0 12 -4 0 0 0 1 10 0 0",
                "0 0 0 14 -4 0 0 0 1 10 0 0",
                "0 0 0 16 -4 0 0 0 1 10 0 0",
                "0 0 0 18 -4 0 0 0 1 10 0 0",
                "0 0 0 20 -4 0 0 0 1 10 0 0",
                "0 0 0 10 -4 0 0 0 1 8 0 0",
                "0 0 0 12 -4 0 0 0 1 8 0 0",
                "0 0 0 14 -4 0 0 0 1 8 0 0",
                "0 0 0 16 -4 0 0 0 1 8 0 0",
                "0 0 0 18 -4 0 0 0 1 8 0 0",
                "0 0 0 20 -4 0 0 0 1 8 0 0",
                "0 6 0 10 -4 0 0 -2 1 8 0 0",
                "2 0 0 6 -4 0 0 0 1 0 0 0",
                "3 0 0 10 -4 0 0 -10 1 0 0 0",
                "3 0 0 14 -4 0 0 -8 1 0 0 0",
                "3 0 0 16 -4 0 0 -10 1 0 0 0",
                "3 0 0 20 -4 0 0 -8 1 0 0 0",
                "2 0 0 18 -4 -2 0 0 1 0 1 0",
                "2 0 0 14 -4 -2 0 0 1 0 1 0",
                "2 0 0 2 -4 -2 0 0 1 0 1 0",
                "2 0 0 0 -4 0 0 2 1 0 1 0",
                "2 5 0 6 0 -7 0 0 0 6 1 0",
                "0 0 0 4 -4 0 0 0 1 6 1 0",
                "0 0 0 20 -4 0 0 0 1 6 1 0",
                "1 5 0 4 0 -9 0 0 0 0 1 0",
                "1 5 0 0 0 -7 0 0 0 10 1 0",
                "4 5 0 2 0 -9 0 0 0 9 1 0",
                "4 5 0 4 0 -9 0 0 0 9 1 0",
                "4 5 0 0 0 -9 0 0 0 9 1 0",
                "0 0 0 0 -4 0 0 0 1 10 2 0",
                "0 0 0 2 -4 0 0 0 1 10 2 0",
                "0 0 0 4 -4 0 0 0 1 10 2 0",
                "0 0 0 6 -4 0 0 0 1 10 2 0",
                "2 6 0 6 -4 -2 0 0 1 0 2 0",
                "2 6 0 10 -4 -2 0 0 1 0 2 0",
                "2 6 0 8 -4 0 0 -4 1 8 2 0",
                "2 6 0 14 -4 0 0 0 1 4 2 0",
                "1 6 0 10 -4 0 0 -2 1 2 2 0",
                "1 6 0 8 -4 0 0 -4 1 2 2 0",
                "1 6 0 10 -4 0 0 -4 1 2 2 0",
                "0 6 0 6 -4 0 0 -8 1 10 2 0",
                "1 6 0 10 -4 0 0 -2 1 8 2 0",
                "0 0 0 16 -4 0 0 0 1 6 2 0",
                "0 0 0 18 -4 0 0 0 1 6 2 0",
                "0 0 0 20 -4 0 0 0 1 6 2 0",
                "0 0 0 22 -4 0 0 0 1 6 2 0",
                "0 0 0 2 -4 -2 0 0 1 4 2 0",
                "0 0 0 4 -4 -2 0 0 1 4 2 0",
                "0 0 0 0 -4 -2 0 0 1 4 2 0",
                "0 0 0 6 -4 -2 0 0 1 4 2 0",
                "0 0 0 16 -4 -2 0 0 1 4 2 0",
                "0 0 0 18 -4 -2 0 0 1 4 2 0",
                "0 0 0 20 -4 -2 0 0 1 4 2 0",
                "2 4 2 4 -4 0 0 0 1 0 2 0",
                "2 4 3 0 1 0 0 0 -5 1 2 0",
                "2 4 0 0 14 0 0 0 -1 11 2 0",
                "0 0 0 -2 -4 0 0 0 1 10 2 0",
                "0 0 0 -2 -4 0 0 0 1 8 2 0",
                "2 0 0 -2 -4 2 0 0 1 8 2 0",
                "2 0 0 -4 -4 0 0 2 1 2 2 0",
                "3 0 0 -2 -4 2 0 0 1 2 2 0",
                "3 0 0 0 -4 6 0 0 1 2 2 0",
                "3 0 0 0 -4 4 0 -4 1 2 2 0",
                "3 0 0 0 -4 2 0 -8 1 2 2 0",
                "3 0 0 0 -4 4 0 -10 1 2 2 0",
                "3 0 0 0 -4 2 0 -14 1 2 2 0",
                "3 0 0 2 -4 0 0 -14 1 2 2 0",
                "3 0 0 4 -4 0 0 -16 1 2 2 0",
                "3 0 0 8 -4 0 0 -14 1 2 2 0",
                "3 0 0 10 -4 0 0 -16 1 2 2 0",
                "3 0 0 14 -4 0 0 -14 1 2 2 0",
                "3 0 0 18 -4 0 0 -12 1 2 2 0",
                "3 0 0 20 -4 0 0 -8 1 2 2 0",
                "3 0 0 24 -4 0 0 -6 1 2 2 0",
                "3 0 0 26 -4 0 0 -2 1 2 2 0",
                "3 0 0 26 -4 -2 0 0 1 2 2 0",
                "2 0 0 20 -4 -2 0 0 1 0 2 0",
                "1 0 0 22 -4 -2 0 0 1 2 1 0",
                "1 0 0 24 -4 0 0 0 1 2 1 0",
                "1 0 0 22 -4 0 0 -2 1 2 1 0",
                "1 0 0 22 -4 0 0 -4 1 2 1 0",
                "1 0 0 20 -4 0 0 -4 1 2 1 0",
                "1 0 0 20 -4 0 0 -6 1 2 1 0",
                "1 0 0 0 -4 0 0 2 1 2 1 0",
                "1 0 0 -2 -4 0 0 0 1 2 1 0",
                "1 0 0 0 -4 2 0 0 1 2 1 0",
                "1 0 0 -2 -4 2 0 0 1 2 1 0",
                "1 0 0 0 -4 4 0 0 1 2 1 0",
                "1 0 0 0 -4 2 0 -2 1 2 1 0",
                "1 0 0 0 -4 4 0 -2 1 2 1 0",
                "0 0 0 22 -4 0 0 0 1 10 3 0",
                "0 0 0 20 -4 0 0 0 1 10 3 0",
                "0 0 0 18 -4 0 0 0 1 10 3 0",
                "0 0 0 16 -4 0 0 0 1 10 3 0",
                "0 0 0 14 -4 0 0 0 1 10 3 0",
                "2 3 1 14 -4 0 0 0 1 0 3 0",
                "0 0 0 14 -4 -2 0 0 1 0 3 0",
                "0 0 0 16 -4 -2 0 0 1 0 3 0",
                "0 0 0 18 -4 -2 0 0 1 0 3 0",
                "0 0 0 20 -4 -2 0 0 1 0 3 0",
                "0 0 0 22 -4 -2 0 0 1 0 3 0",
                "0 0 0 24 -4 -2 0 0 1 0 3 0",
                "1 0 0 24 -4 0 0 -2 1 8 3 0",
                "1 0 0 24 -4 0 0 -4 1 8 3 0",
                "1 0 0 22 -4 0 0 -6 1 8 3 0",
                "1 0 0 22 -4 0 0 -8 1 8 3 0",
                "1 0 0 20 -4 0 0 -8 1 8 3 0",
                "1 0 0 20 -4 0 0 -10 1 8 3 0",
                "1 0 0 24 -4 0 0 -6 1 8 3 0",
                "1 0 0 26 -4 0 0 -2 1 8 3 0",
                "1 0 0 26 -4 0 0 -4 1 8 3 0",
                "0 0 0 14 -4 0 0 0 1 8 3 0",
                "3 0 0 8 -4 0 0 -6 1 8 3 1",
                "2 0 0 6 -4 -2 0 0 1 0 3 0",
                "2 0 0 2 -4 -2 0 0 1 0 3 0",
                "2 0 0 0 -4 0 0 2 1 0 3 0",
                "2 0 0 -4 -4 0 0 2 1 0 3 0",
                "0 0 0 6 -4 -2 0 0 1 2 3 0",
                "0 0 0 2 -4 -2 0 0 1 2 3 0",
                "0 0 0 0 -4 0 0 2 1 2 3 0",
                "3 0 0 -4 -4 0 0 2 1 2 3 0",
                "3 0 0 -4 -4 2 0 0 1 2 3 0",
                "3 0 0 -2 -4 6 0 0 1 2 3 0",
                "3 0 0 0 -4 6 0 -2 1 2 3 0",
                "5 1 0 -2 -4 0 0 2 1 3 4 0",
                "5 1 0 0 -4 0 0 4 1 3 4 0",
                "5 1 0 0 -4 -2 0 4 1 3 4 0",
                "5 1 0 0 -4 -4 0 4 1 3 4 0",
                "5 1 0 0 -4 -6 0 4 1 3 4 0",
                "5 1 0 0 -4 -8 0 4 1 3 4 0",
                "5 1 0 0 -4 -10 0 4 1 3 4 0",
                "5 1 0 0 -4 -12 0 4 1 3 4 0",
                "1 1 0 0 -2 -12 0 4 3 3 4 0",
                "1 1 0 0 0 -12 0 4 5 3 4 0",
                "1 1 0 0 0 -12 -2 4 5 3 4 0",
                "4 2 0 4 0 -16 -2 0 5 0 4 0",
                "4 2 0 6 0 -16 -2 0 5 0 4 0",
                "3 0 0 10 -4 0 0 -2 1 8 3 0",
                "3 0 0 8 -4 0 0 -12 1 8 3 0",
                "3 0 0 6 -4 0 0 -10 1 8 3 0",
                "4 2 0 0 0 -14 -2 2 5 0 4 0",
                "4 2 0 0 0 -16 -2 0 5 0 4 0",
                "4 2 0 2 0 -16 -2 0 5 0 4 0",
                "-- End Tiles --",
                " ",
                "-- Checkpoints --",
                "0 0 0 0 0 0 0",
                "14 0 -2 0 0 0 1",
                "0 7 0 -9 0 0 2",
                "7 -4 0 0 -2 1 3",
                "-- End Checkpoints --",
                " ",
                "-- Victories --",
                "6 0 -16 -2 0 1 4",
                "-- End Victories --",
                " ",
                "-- Warps --",
                "10 0 0 -3 -10 9 0",
                "0 -3 0 0 0 0 1",
                "22 -3 0 0 0 0 2",
                "22 -17 0 14 0 0 3",
                "-- End Warps --",
            };

            Debug.Log("defaultPlayTesselationFromMainMenu: " + defaultPlayTesselationFromMainMenu);
            levelData = LevelLoader.LoadLevel(defaultPlayTesselationFromMainMenu);

            // set level info to dummy default level info
            levelInfo = new LevelInfo
            {
                id = "",
                name = "defaultPlayTesselationFromMainMenu",
                isLocal = true,
                created_at = DateTime.MinValue,
            };

            levelReady = true;
            return;
        }

        if (string.IsNullOrEmpty(levelInfo.name))
        {
            Debug.LogError("[PlayLoader] No level Name in Level Info!");
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
            // first, check to see whether the file exists
            bool file_exists = File.Exists(path);
            Debug.Log($"[LOAD] Loading from: {path}"); // when loading

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
                Debug.LogError("[PlayLoader] File not found :(");
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

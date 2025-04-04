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

        if (string.IsNullOrEmpty(levelInfo.name))
        {
            Debug.LogError("[PlayLoader] No level Name in Level Info!");
            return;
        }

        // set the path
        string levelsFolder = LevelStorage.LevelsFolder;
        path = Path.Combine(levelsFolder, $"{levelName}.json");
        path = path.Replace("\\", "/");

        // this loader stays awake when next scene is loaded
        DontDestroyOnLoad(gameObject);

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
                Debug.LogError("File not found :(");
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

﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using circleXsquares;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayLoader : MonoBehaviour
{
    // public read-accessibility state variables
    public string levelName { get; private set; }

    // private variables
    private string path;

    private string[] supabaseLevelPayloadData;

    private LevelData levelData = new LevelData();

    private bool levelReady = false;

    void Awake()
    {
        // levelName is hard coded (!!), should be prompted
        levelName = "testLevel";
        string filename = levelName + ".txt";
        path = Path.Combine(new string[] { "Levels", filename });
        // this loader stays awake when next scene is loaded
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
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
                // if file exists, it is loaded and parsed
                string[] lines = File.ReadAllLines(path);
                levelData = LevelLoader.LoadLevel(lines);
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

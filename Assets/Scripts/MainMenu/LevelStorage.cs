using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class LevelStorage
{
    public static string LevelsFolder =>
        Path.Combine(Application.persistentDataPath, "levels");

    public static List<LevelInfo> LoadLocalLevelMetadata()
    {
        Debug.Log(LevelsFolder);

        var levelInfos = new List<LevelInfo>();

        if (!Directory.Exists(LevelsFolder))
            Directory.CreateDirectory(LevelsFolder);

        string[] files = Directory.GetFiles(LevelsFolder, "*.json");

        foreach (var file in files)
        {
            try
            {
                string json = File.ReadAllText(file);
                var levelData = JsonUtility.FromJson<SupabaseLevelDTO>(json); // See below
                var info = new LevelInfo
                {
                    id = Path.GetFileNameWithoutExtension(file),
                    name = levelData.name,
                    isLocal = true,
                    created_at = File.GetLastWriteTime(file)
                };
                levelInfos.Add(info);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load level file {file}: {e.Message}");
            }
        }

        return levelInfos;
    }

    public static void DeleteLevel(string id)
    {
        string path = Path.Combine(LevelsFolder, $"{id}.json");
        if (File.Exists(path))
            File.Delete(path);
    }

}

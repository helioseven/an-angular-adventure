using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class LevelStorage
{
    public static string LevelsFolder => Path.Combine(Application.persistentDataPath, "levels");

    public static List<LevelInfo> LoadLocalLevelMetadata()
    {
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
                    uploaderId = "you, sucka!",
                    createdAt = File.GetLastWriteTime(file),
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

    public static bool DeleteLocalLevel(string levelName)
    {
        try
        {
            string path = Path.Combine(LevelStorage.LevelsFolder, $"{levelName}.json");
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[LevelStorage] Deleted local level: {levelName}");
                return true;
            }
            else
            {
                Debug.LogWarning($"[LevelStorage] Tried to delete missing level: {levelName}");
                return false;
            }
        }
        catch (IOException e)
        {
            Debug.LogError($"[LevelStorage] Error deleting level {levelName}: {e.Message}");
            return false;
        }
    }
}

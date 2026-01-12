using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class LevelStorage
{
    public const string TessellationExtension = ".tes";
    public const string LegacyJsonExtension = ".json";
    public static string TessellationsFolder =>
        Path.Combine(Application.persistentDataPath, "Tessellations");

    public static string LegacyLevelsFolder =>
        Path.Combine(Application.persistentDataPath, "levels");

    public static string BundledTessellationsFolder =>
        Path.Combine(Application.streamingAssetsPath, "Tessellations");

    private static IEnumerable<string> GetExtensions()
    {
        yield return TessellationExtension;
        yield return LegacyJsonExtension;
    }

    private static IEnumerable<string> GetLocalFolders()
    {
        yield return TessellationsFolder;
        yield return LegacyLevelsFolder;
    }

    public static bool TryGetLocalLevelPath(string levelName, out string path)
    {
        if (StartupManager.SimulateEmptyLocalFolders)
        {
            path = string.Empty;
            return false;
        }

        foreach (var folder in GetLocalFolders())
        {
            foreach (var extension in GetExtensions())
            {
                string candidate = Path.Combine(folder, $"{levelName}{extension}");
                if (File.Exists(candidate))
                {
                    path = candidate;
                    return true;
                }
            }
        }

        path = string.Empty;
        return false;
    }

    public static bool LocalLevelExists(string levelName)
    {
        if (StartupManager.SimulateEmptyLocalFolders)
            return false;

        return TryGetLocalLevelPath(levelName, out _);
    }

    public static bool HasLocalLevels()
    {
        if (StartupManager.SimulateEmptyLocalFolders)
            return false;

        foreach (var folder in GetLocalFolders())
        {
            if (!Directory.Exists(folder))
                continue;

            foreach (var extension in GetExtensions())
            {
                if (Directory.GetFiles(folder, $"*{extension}").Length > 0)
                    return true;
            }
        }

        return false;
    }

    public static bool TryGetBundledLevelPath(string levelName, out string path)
    {
        foreach (var extension in GetExtensions())
        {
            path = Path.Combine(BundledTessellationsFolder, $"{levelName}{extension}");
            if (File.Exists(path))
                return true;
        }

        path = string.Empty;
        return false;
    }

    public static List<LevelInfo> LoadLocalLevelMetadata()
    {
        if (StartupManager.SimulateEmptyLocalFolders)
            return new List<LevelInfo>();

        var levelInfos = new List<LevelInfo>();
        var dedupe = new Dictionary<string, LevelInfo>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(TessellationsFolder))
            Directory.CreateDirectory(TessellationsFolder);

        foreach (var folder in GetLocalFolders())
        {
            if (!Directory.Exists(folder))
                continue;

            foreach (var extension in GetExtensions())
            {
                string[] files = Directory.GetFiles(folder, $"*{extension}");
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
                            preview = levelData.preview,
                            dataHash = BestTimeStore.ComputeDataHash(levelData.data),
                            uploaderId = "you, sucka!",
                            uploaderDisplayName = StartupManager.DemoModeEnabled ? "Custom" : "You",
                            createdAt = File.GetLastWriteTime(file),
                        };

                        if (!dedupe.ContainsKey(info.id))
                            dedupe[info.id] = info;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Failed to load level file {file}: {e.Message}");
                    }
                }
            }
        }

        levelInfos.AddRange(dedupe.Values);
        return levelInfos;
    }

    public static bool DeleteLocalLevel(string levelName)
    {
        try
        {
            bool deletedAny = false;
            foreach (var folder in GetLocalFolders())
            {
                foreach (var extension in GetExtensions())
                {
                    string path = Path.Combine(folder, $"{levelName}{extension}");
                    if (!File.Exists(path))
                        continue;

                    File.Delete(path);
                    Debug.Log($"[LevelStorage] Deleted local level: {levelName}");
                    deletedAny = true;
                }
            }

            if (!deletedAny)
                Debug.LogWarning($"[LevelStorage] Tried to delete missing level: {levelName}");

            return deletedAny;
        }
        catch (IOException e)
        {
            Debug.LogError($"[LevelStorage] Error deleting level {levelName}: {e.Message}");
            return false;
        }
    }

    public static List<LevelInfo> LoadBundledLevelMetadata()
    {
        var levelInfos = new List<LevelInfo>();

        if (!Directory.Exists(BundledTessellationsFolder))
            return levelInfos;

        foreach (var extension in GetExtensions())
        {
            string[] files = Directory.GetFiles(BundledTessellationsFolder, $"*{extension}");
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
                        isLocal = false,
                        isBundled = true,
                        preview = levelData.preview,
                        dataHash = BestTimeStore.ComputeDataHash(levelData.data),
                        uploaderId = "tessellations",
                        uploaderDisplayName = "Demo Tessellations",
                        createdAt = File.GetLastWriteTime(file),
                    };
                    levelInfos.Add(info);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to load bundled level file {file}: {e.Message}");
                }
            }
        }

        return levelInfos;
    }
}

using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class BestTimeStore
{
    public static bool RecordBestTime(string levelName, string dataHash, float seconds)
    {
        if (string.IsNullOrWhiteSpace(levelName) || string.IsNullOrWhiteSpace(dataHash))
            return false;

        string key = BuildKey(levelName, dataHash);
        float existing = PlayerPrefs.GetFloat(key, -1f);
        if (existing < 0f || seconds < existing)
        {
            PlayerPrefs.SetFloat(key, seconds);
            PlayerPrefs.Save();
            return true;
        }

        return false;
    }

    public static bool RecordBestTimeForRemote(string levelId, float seconds)
    {
        if (string.IsNullOrWhiteSpace(levelId))
            return false;

        string key = BuildRemoteKey(levelId);
        float existing = PlayerPrefs.GetFloat(key, -1f);
        if (existing < 0f || seconds < existing)
        {
            PlayerPrefs.SetFloat(key, seconds);
            PlayerPrefs.Save();
            return true;
        }

        return false;
    }

    public static bool TryGetBestTime(string levelName, string dataHash, out float seconds)
    {
        seconds = 0f;
        if (string.IsNullOrWhiteSpace(levelName) || string.IsNullOrWhiteSpace(dataHash))
            return false;

        string key = BuildKey(levelName, dataHash);
        if (!PlayerPrefs.HasKey(key))
            return false;

        seconds = PlayerPrefs.GetFloat(key);
        return seconds > 0f;
    }

    public static bool TryGetBestTimeForRemote(string levelId, out float seconds)
    {
        seconds = 0f;
        if (string.IsNullOrWhiteSpace(levelId))
            return false;

        string key = BuildRemoteKey(levelId);
        if (!PlayerPrefs.HasKey(key))
            return false;

        seconds = PlayerPrefs.GetFloat(key);
        return seconds > 0f;
    }

    public static string ComputeDataHash(string[] lines)
    {
        if (lines == null || lines.Length == 0)
            return string.Empty;

        string joined = string.Join("\n", lines);
        byte[] bytes = Encoding.UTF8.GetBytes(joined);
        using SHA256 sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(bytes);
        return ToHex(hash);
    }

    private static string BuildKey(string levelName, string dataHash)
    {
        string safeName = levelName.Trim().ToLowerInvariant();
        return $"best_time:{safeName}:{dataHash}";
    }

    private static string BuildRemoteKey(string levelId)
    {
        string safeId = levelId.Trim().ToLowerInvariant();
        return $"best_time:remote:{safeId}";
    }

    private static string ToHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (byte b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}

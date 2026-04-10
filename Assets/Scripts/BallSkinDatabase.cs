using UnityEngine;

[CreateAssetMenu(fileName = "BallSkinDatabase", menuName = "ScriptableObjects/BallSkinDatabase")]
public class BallSkinDatabase : ScriptableObject
{
    public const string SelectedSkinPrefKey = "SelectedBallSkin";

    private const string UnlockPrefPrefix = "BallSkinUnlocked:";

    public Sprite[] skins;
    public string[] skinIds;
    public string[] displayNames;
    public string[] unlockBundledLevelNames;

    public int Count => skins != null ? skins.Length : 0;

    public Sprite GetSprite(int index)
    {
        return IsValidIndex(index) ? skins[index] : null;
    }

    public string GetDisplayName(int index)
    {
        string configuredName = GetArrayValue(displayNames, index);
        if (!string.IsNullOrWhiteSpace(configuredName))
            return configuredName;

        Sprite sprite = GetSprite(index);
        return sprite != null ? sprite.name : $"Ball Skin {index + 1}";
    }

    public bool IsUnlocked(int index)
    {
        if (!IsValidIndex(index))
            return false;

        if (index == 0)
            return true;

        string unlockLevel = GetArrayValue(unlockBundledLevelNames, index);
        if (string.IsNullOrWhiteSpace(unlockLevel))
            return true;

        return PlayerPrefs.GetInt(GetUnlockPrefKey(index), 0) == 1;
    }

    public int GetFirstUnlockedIndex()
    {
        for (int i = 0; i < Count; i++)
        {
            if (IsUnlocked(i))
                return i;
        }

        return 0;
    }

    public int GetNextUnlockedIndex(int currentIndex)
    {
        if (Count <= 0)
            return 0;

        for (int step = 1; step <= Count; step++)
        {
            int nextIndex = PositiveModulo(currentIndex + step, Count);
            if (IsUnlocked(nextIndex))
                return nextIndex;
        }

        return GetFirstUnlockedIndex();
    }

    public bool TryUnlockForBundledLevel(LevelInfo levelInfo, out int unlockedIndex)
    {
        unlockedIndex = -1;

        if (levelInfo == null || !levelInfo.isBundled || string.IsNullOrWhiteSpace(levelInfo.name))
            return false;

        for (int i = 1; i < Count; i++)
        {
            string unlockLevel = GetArrayValue(unlockBundledLevelNames, i);
            if (string.IsNullOrWhiteSpace(unlockLevel))
                continue;

            if (
                !string.Equals(
                    unlockLevel,
                    levelInfo.name,
                    System.StringComparison.OrdinalIgnoreCase
                )
            )
                continue;

            if (IsUnlocked(i))
                return false;

            PlayerPrefs.SetInt(GetUnlockPrefKey(i), 1);
            PlayerPrefs.Save();
            unlockedIndex = i;
            return true;
        }

        return false;
    }

    public bool IsValidIndex(int index)
    {
        return index >= 0 && index < Count;
    }

    private string GetUnlockPrefKey(int index)
    {
        string id = GetArrayValue(skinIds, index);
        if (string.IsNullOrWhiteSpace(id))
        {
            Sprite sprite = GetSprite(index);
            id =
                sprite != null && !string.IsNullOrWhiteSpace(sprite.name)
                    ? sprite.name
                    : $"skin_{index}";
        }

        return UnlockPrefPrefix + id;
    }

    private static string GetArrayValue(string[] values, int index)
    {
        if (values == null || index < 0 || index >= values.Length)
            return string.Empty;

        return values[index];
    }

    private static int PositiveModulo(int value, int modulo)
    {
        return ((value % modulo) + modulo) % modulo;
    }
}

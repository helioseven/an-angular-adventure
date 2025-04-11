using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class SaveDialogControl : MonoBehaviour
{
    public OverwriteDialogControl overwriteDialogControl;
    private TMP_InputField _inputField;

    void Awake()
    {
        // establishes a reference to the relevant text component
        _inputField = transform.Find("Save Name Input").GetComponent<TMP_InputField>();

        gameObject.SetActive(false);
    }

    void Start()
    {
        _inputField.text = EditGM.instance.levelName;
    }

    /* Public Functions */

    // pauses what the EditGM is doing to invoke the save dialog
    public void invokeDialog()
    {
        _inputField.text = EditGM.instance.levelName;
        EditGM.instance.gameObject.SetActive(false);
        gameObject.SetActive(true);
        _inputField.Select();
    }

    // cancels the save dialog by deactivating the panel and resuming EditGM
    public void cancelDialog()
    {
        gameObject.SetActive(false);
        EditGM.instance.gameObject.SetActive(true);
    }

    // confirms the file save by passing the entered filename to the EditGM
    public void confirmSave()
    {
        string name = _inputField.text;

        // first, check to see whether the folder exists
        if (!Directory.Exists(LevelStorage.LevelsFolder))
            Directory.CreateDirectory(LevelStorage.LevelsFolder);

        // then, check to see whether the file exists
        string path = Path.Combine(LevelStorage.LevelsFolder, $"{name}.json");

        string levelNameIncremented = GetIncrementedName(name);

        // if there's no overwite, save the file outright
        if (!File.Exists(path))
        {
            ForceSaveLocalLevel(name);
            cancelDialog();
        }
        else
        {
            // otherwise open overwrite dialog if there's a potential overwrite afoot
            overwriteDialogControl.ShowPrompt(
                name,
                levelNameIncremented,
                onCancel: () => invokeDialog(),
                onOverwrite: () => ForceSaveLocalLevel(name, true),
                onIncrement: () => ForceSaveLocalLevel(levelNameIncremented)
            );

            // close the save dialog
            cancelDialog();
        }
    }

    // Saves the level locally with no safeguards
    public void ForceSaveLocalLevel(string name, bool updateName = false)
    {
        EditGM.instance.SaveLevelLocal(name);

        // Optional - set the level name to the (possibly) new-ish (overwritten) name
        if (updateName)
            EditGM.instance.levelName = name;
    }

    private string GetIncrementedName(string name)
    {
        string baseName = StripIncrementSuffix(name);
        int i = 1;
        string newName = baseName;
        string path = Path.Combine(LevelStorage.LevelsFolder, $"{baseName}.json");

        // increment i until the path doesn't exist anymore
        while (File.Exists(path))
        {
            newName = $"{baseName} ({i++})";
            path = Path.Combine(LevelStorage.LevelsFolder, $"{newName}.json");
        }

        return newName;
    }

    private string StripIncrementSuffix(string name)
    {
        var match = Regex.Match(name, @"^(.*)\s\(\d+\)$");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return name;
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelListItemUI : MonoBehaviour
{
    public TMP_Text levelNameText;
    public Button playButton;
    public Button editOrRemixButton;
    public TMP_Text editOrRemixButtonText;
    public Button deleteButton;
    public LevelBrowser parent;

    public void Setup(
        LevelInfo info,
        System.Action onPlay,
        System.Action onEditOrRemix,
        LevelBrowser browser
    )
    {
        levelNameText.text = info.name + (info.isLocal ? " (Draft)" : "");
        editOrRemixButtonText.text = info.isLocal ? "Edit" : "Remix";

        parent = browser;

        playButton.onClick.RemoveAllListeners();
        playButton.onClick.AddListener(() => onPlay());

        editOrRemixButton.onClick.RemoveAllListeners();
        editOrRemixButton.onClick.AddListener(() => onEditOrRemix());

        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(() =>
        {
            SupabaseController.Instance.StartCoroutine(
                SupabaseController.Instance.SoftDeleteLevelById(info.id, callback)
            );
        });
    }

    // Supabase - callback function after deleting
    public void callback(bool success)
    {
        Debug.Log("Delete successful: " + success);
        parent.RefreshList();
    }
}

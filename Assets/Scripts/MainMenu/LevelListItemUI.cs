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

        // Only show delete for owned levels
        bool isOwner = info.uploaderId == AuthState.SteamId;

        deleteButton.gameObject.SetActive(isOwner);

        if (isOwner)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() =>
            {
                parent.ShowConfirmDelete(info.id, info.name);
            });
        }
    }

    // Supabase - callback function after deleting
    public void callback(bool success)
    {
        Debug.Log("Delete successful: " + success);
        parent.RefreshList();
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelListItemUI : MonoBehaviour
{
    public TMP_Text levelNameText;
    public TMP_Text creatorNameText;
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
        string creatorLabel = info.uploaderDisplayName;

        // attempt to fall back to uploader id if no creatorLabel present
        if (string.IsNullOrEmpty(creatorLabel))
            creatorLabel = string.IsNullOrEmpty(info.uploaderId)
                ? "Unknown creator"
                : info.uploaderId;

        creatorNameText.text = creatorLabel;

        editOrRemixButtonText.text = info.isLocal ? "Edit" : "Remix";

        parent = browser;

        playButton.onClick.RemoveAllListeners();
        playButton.onClick.AddListener(() => onPlay());

        editOrRemixButton.onClick.RemoveAllListeners();
        editOrRemixButton.onClick.AddListener(() => onEditOrRemix());

        if (StartupManager.DemoModeEnabled && editOrRemixButton != null)
            editOrRemixButton.gameObject.SetActive(false);

        // for the delete button level ownership check, we consider them the owner if
        // they uploaded it OR the level is local
        bool isOwner = info.uploaderId == AuthState.Instance.SteamId || info.isLocal;
        if (StartupManager.DemoModeEnabled)
            isOwner = false;

        // Only show delete for "owned" levels
        deleteButton.gameObject.SetActive(isOwner);
        if (isOwner)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() =>
            {
                parent.ShowConfirmDelete(
                    info.isLocal ? info.name : info.id, // pass name for local, id for cloud
                    info.name,
                    info.isLocal
                );
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

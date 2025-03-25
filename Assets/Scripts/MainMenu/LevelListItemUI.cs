using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelListItemUI : MonoBehaviour
{
    public TMP_Text levelNameText;
    public Button playButton;
    public Button editOrRemixButton;
    public TMP_Text editOrRemixButtonText;

    public void Setup(LevelInfo info, System.Action onPlay, System.Action onEditOrRemix)
    {
        levelNameText.text = info.name + (info.isLocal ? " (Draft)" : "");
        editOrRemixButtonText.text = info.isLocal ? "Edit" : "Remix";

        playButton.onClick.RemoveAllListeners();
        playButton.onClick.AddListener(() => onPlay());

        editOrRemixButton.onClick.RemoveAllListeners();
        editOrRemixButton.onClick.AddListener(() => onEditOrRemix());
    }
}

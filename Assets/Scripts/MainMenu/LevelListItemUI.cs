using UnityEngine;
using UnityEngine.UI;

public class LevelListItemUI : MonoBehaviour
{
    public Text levelNameText;
    public Button playButton;

    public void Setup(LevelInfo info, System.Action onPlay)
    {
        levelNameText.text = info.name + (info.isLocal ? " (Draft)" : "");
        playButton.onClick.AddListener(() => onPlay());
    }
}

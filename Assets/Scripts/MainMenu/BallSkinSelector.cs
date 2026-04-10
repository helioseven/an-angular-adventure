using UnityEngine;
using UnityEngine.UI;

public class BallSkinSelector : MonoBehaviour
{
    public BallSkinDatabase skinDB;
    public Image previewImage; // Drag the button's own Image here

    private int currentIndex;

    void Start()
    {
        currentIndex = PlayerPrefs.GetInt(BallSkinDatabase.SelectedSkinPrefKey, 0);
        if (
            skinDB == null
            || !skinDB.IsValidIndex(currentIndex)
            || !skinDB.IsUnlocked(currentIndex)
        )
        {
            currentIndex = skinDB != null ? skinDB.GetFirstUnlockedIndex() : 0;
            PlayerPrefs.SetInt(BallSkinDatabase.SelectedSkinPrefKey, currentIndex);
            PlayerPrefs.Save();
        }
        UpdatePreview();
    }

    public void CycleSkin()
    {
        if (skinDB == null || skinDB.Count <= 0)
            return;

        currentIndex = skinDB.GetNextUnlockedIndex(currentIndex);
        PlayerPrefs.SetInt(BallSkinDatabase.SelectedSkinPrefKey, currentIndex);
        PlayerPrefs.Save();
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (previewImage == null || skinDB == null)
            return;

        previewImage.sprite = skinDB.GetSprite(currentIndex);
    }
}

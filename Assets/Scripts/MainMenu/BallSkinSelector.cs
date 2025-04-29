using UnityEngine;
using UnityEngine.UI;

public class BallSkinSelector : MonoBehaviour
{
    public BallSkinDatabase skinDB;
    public Image previewImage; // Drag the button's own Image here

    private int currentIndex;
    private const string SkinPrefKey = "SelectedBallSkin";

    void Start()
    {
        currentIndex = PlayerPrefs.GetInt(SkinPrefKey, 0);
        UpdatePreview();
    }

    public void CycleSkin()
    {
        currentIndex = (currentIndex + 1) % skinDB.skins.Length;
        PlayerPrefs.SetInt(SkinPrefKey, currentIndex);
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        previewImage.sprite = skinDB.skins[currentIndex];
    }
}

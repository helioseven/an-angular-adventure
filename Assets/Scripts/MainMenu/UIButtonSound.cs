using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        SoundManager.instance.Play("mainMenuHover");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        SoundManager.instance.Play("mainMenuClick");
    }
}

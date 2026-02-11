using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonSound
    : MonoBehaviour,
        IPointerEnterHandler,
        IPointerClickHandler,
        ISelectHandler,
        ISubmitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (SoundManager.instance != null)
            SoundManager.instance.Play("mainMenuHover");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (SoundManager.instance != null)
            SoundManager.instance.Play("mainMenuClick");
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (InputModeTracker.Instance == null
            || InputModeTracker.Instance.CurrentMode != InputMode.Navigation)
            return;

        if (SoundManager.instance != null)
            SoundManager.instance.Play("mainMenuHover");
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (InputModeTracker.Instance == null
            || InputModeTracker.Instance.CurrentMode != InputMode.Navigation)
            return;

        if (SoundManager.instance != null)
            SoundManager.instance.Play("mainMenuClick");
    }
}

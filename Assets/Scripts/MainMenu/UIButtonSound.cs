using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonSound
    : MonoBehaviour,
        IPointerEnterHandler,
        IPointerClickHandler,
        ISelectHandler,
        ISubmitHandler
{
    private float suppressUntilTime;

    private void OnEnable()
    {
        // Menu activation can trigger synthetic select/hover events before the user interacts.
        suppressUntilTime = Time.unscaledTime + 0.1f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Time.unscaledTime < suppressUntilTime)
            return;

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
        if (Time.unscaledTime < suppressUntilTime)
            return;

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

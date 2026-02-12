using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectedJiggle : MonoBehaviour
{
    public enum JiggleAxis
    {
        Vertical,
        Horizontal,
    }

    [SerializeField]
    private float amplitude = 6f;

    [SerializeField]
    private float frequency = 2.5f;

    [SerializeField]
    private Transform scopeRoot;

    [SerializeField]
    private JiggleAxis axis = JiggleAxis.Vertical;

    private RectTransform current;
    private GameObject currentOwner;
    private Vector2 basePosition;

    private void Update()
    {
        if (
            InputModeTracker.Instance != null
            && InputModeTracker.Instance.CurrentMode != InputMode.Navigation
        )
        {
            ResetCurrent();
            return;
        }

        var selected =
            EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected == null || (scopeRoot != null && !selected.transform.IsChildOf(scopeRoot)))
        {
            ResetCurrent();
            return;
        }

        var rect = selected.GetComponent<RectTransform>();
        if (rect == null)
        {
            ResetCurrent();
            return;
        }

        if (currentOwner != selected)
        {
            ResetCurrent();
            currentOwner = selected;
            current = ResolveJiggleTarget(rect, out JiggleAxis targetAxis);
            axis = targetAxis;
            if (current == null)
            {
                ResetCurrent();
                return;
            }
            basePosition = current.anchoredPosition;
        }

        float offset = Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * frequency) * amplitude;
        if (axis == JiggleAxis.Horizontal)
            current.anchoredPosition = new Vector2(basePosition.x + offset, basePosition.y);
        else
            current.anchoredPosition = new Vector2(basePosition.x, basePosition.y + offset);
    }

    private void ResetCurrent()
    {
        if (current != null)
            current.anchoredPosition = basePosition;

        current = null;
        currentOwner = null;
    }

    public void SetScope(Transform scope)
    {
        scopeRoot = scope;
    }

    public void SetAmplitude(float newAmplitude)
    {
        amplitude = newAmplitude;
        ResetCurrent();
    }

    private static RectTransform ResolveJiggleTarget(
        RectTransform selectedRect,
        out JiggleAxis targetAxis
    )
    {
        targetAxis = JiggleAxis.Vertical;
        if (selectedRect == null)
            return null;

        var slider = selectedRect.GetComponent<Slider>();
        if (slider != null && slider.handleRect != null)
        {
            // Sliders should jiggle up/down even if the slider itself is horizontal.
            targetAxis = JiggleAxis.Vertical;
            return slider.handleRect;
        }

        var scrollbar = selectedRect.GetComponent<Scrollbar>();
        if (scrollbar != null && scrollbar.handleRect != null)
        {
            // Only vertical scrollbars should jiggle left/right.
            if (
                scrollbar.direction == Scrollbar.Direction.BottomToTop
                || scrollbar.direction == Scrollbar.Direction.TopToBottom
            )
                targetAxis = JiggleAxis.Horizontal;
            else
                targetAxis = JiggleAxis.Vertical;
            return scrollbar.handleRect;
        }

        return selectedRect;
    }
}

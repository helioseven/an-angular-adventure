using UnityEngine;
using UnityEngine.EventSystems;

public class SelectedJiggle : MonoBehaviour
{
    [SerializeField]
    private float amplitude = 6f;
    [SerializeField]
    private float frequency = 2.5f;
    [SerializeField]
    private Transform scopeRoot;

    private RectTransform current;
    private Vector2 basePosition;

    private void Update()
    {
        if (InputModeTracker.Instance != null
            && InputModeTracker.Instance.CurrentMode != InputMode.Navigation)
        {
            ResetCurrent();
            return;
        }

        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
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

        if (current != rect)
        {
            ResetCurrent();
            current = rect;
            basePosition = current.anchoredPosition;
        }

        float offset = Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * frequency) * amplitude;
        current.anchoredPosition = new Vector2(basePosition.x, basePosition.y + offset);
    }

    private void ResetCurrent()
    {
        if (current != null)
            current.anchoredPosition = basePosition;

        current = null;
    }

    public void SetScope(Transform scope)
    {
        scopeRoot = scope;
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MenuFocusUtility
{
    public static void ClearPointerHoverState(GameObject root)
    {
        if (root == null || EventSystem.current == null)
            return;

        var eventData = new PointerEventData(EventSystem.current);
        Selectable[] selectables = root.GetComponentsInChildren<Selectable>(true);
        foreach (Selectable selectable in selectables)
        {
            if (selectable == null)
                continue;

            ExecuteEvents.Execute(
                selectable.gameObject,
                eventData,
                ExecuteEvents.pointerExitHandler
            );
        }
    }

    public static void ApplyHighlightedAsSelected(GameObject root)
    {
        if (root == null)
            return;

        Selectable[] selectables = root.GetComponentsInChildren<Selectable>(true);
        foreach (Selectable selectable in selectables)
        {
            ColorBlock colors = selectable.colors;
            colors.selectedColor = colors.highlightedColor;
            selectable.colors = colors;
        }
    }

    public static void SelectPreferred(GameObject root, Selectable preferred = null)
    {
        if (EventSystem.current == null)
            return;

        if (
            preferred != null
            && preferred.gameObject.activeInHierarchy
            && preferred.IsInteractable()
        )
        {
            EventSystem.current.SetSelectedGameObject(preferred.gameObject);
            return;
        }

        if (root == null)
            return;

        Selectable[] selectables = root.GetComponentsInChildren<Selectable>(true);
        foreach (Selectable selectable in selectables)
        {
            if (!selectable.gameObject.activeInHierarchy || !selectable.IsInteractable())
                continue;

            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
            return;
        }
    }

    public static void SeedModalSelectionIfNeeded(GameObject root, Selectable preferred = null)
    {
        if (EventSystem.current == null)
            return;

        InputModeTracker.EnsureInstance();

        bool pointerMode =
            InputModeTracker.Instance != null
            && InputModeTracker.Instance.CurrentMode == InputMode.Pointer;
        bool controllerVirtualPointer =
            PointerSource.Instance != null && PointerSource.Instance.IsVirtualActive;

        if (pointerMode && !controllerVirtualPointer)
        {
            EventSystem.current.SetSelectedGameObject(null);
            return;
        }

        SelectPreferred(root, preferred);
    }

    public static void EnsureSelectedJiggle(GameObject root)
    {
        if (root == null)
            return;

        InputModeTracker.EnsureInstance();

        SelectedJiggle jiggle = root.GetComponent<SelectedJiggle>();
        if (jiggle == null)
            jiggle = root.AddComponent<SelectedJiggle>();

        jiggle.SetScope(root.transform);
    }

    public static void SetSelectedJiggleEnabled(GameObject root, bool enabled)
    {
        if (root == null)
            return;

        SelectedJiggle jiggle = root.GetComponent<SelectedJiggle>();
        if (jiggle == null)
            return;

        jiggle.SetScope(root.transform);
        jiggle.SetAnimationEnabled(enabled);
    }
}

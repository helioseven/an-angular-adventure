using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuInputModeAdapter : MonoBehaviour
{
    [SerializeField]
    private Transform scopeRoot;
    [SerializeField]
    private bool clearSelectionOnPointer = true;
    [SerializeField]
    private Selectable preferred;

    private readonly Dictionary<Selectable, Color> originalSelectedColors = new();

    private void OnEnable()
    {
        InputModeTracker.EnsureInstance();
        CacheSelectables();

        InputModeTracker.OnModeChanged += HandleModeChanged;
        HandleModeChanged(InputModeTracker.Instance != null
            ? InputModeTracker.Instance.CurrentMode
            : InputMode.Pointer);
    }

    private void OnDisable()
    {
        InputModeTracker.OnModeChanged -= HandleModeChanged;
    }

    public void SetScope(Transform scope)
    {
        scopeRoot = scope;
        CacheSelectables();
    }

    public void SetPreferred(Selectable selection)
    {
        preferred = selection;
    }

    private void HandleModeChanged(InputMode mode)
    {
        if (mode == InputMode.Navigation)
        {
            ApplyControllerHighlighting();
            if (preferred != null)
                MenuFocusUtility.SelectPreferred(preferred.gameObject);
            else
                MenuFocusUtility.SelectPreferred(scopeRoot != null ? scopeRoot.gameObject : gameObject);
        }
        else
        {
            RestoreSelectedColors();
            if (clearSelectionOnPointer && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void CacheSelectables()
    {
        originalSelectedColors.Clear();
        var selectables = GetScopeRoot().GetComponentsInChildren<Selectable>(true);
        foreach (var selectable in selectables)
        {
            if (selectable == null)
                continue;
            originalSelectedColors[selectable] = selectable.colors.selectedColor;
        }
    }

    private void ApplyControllerHighlighting()
    {
        foreach (var kvp in originalSelectedColors)
        {
            var selectable = kvp.Key;
            if (selectable == null)
                continue;

            var colors = selectable.colors;
            colors.selectedColor = colors.highlightedColor;
            selectable.colors = colors;
        }
    }

    private void RestoreSelectedColors()
    {
        foreach (var kvp in originalSelectedColors)
        {
            var selectable = kvp.Key;
            if (selectable == null)
                continue;

            var colors = selectable.colors;
            colors.selectedColor = kvp.Value;
            selectable.colors = colors;
        }
    }

    private Transform GetScopeRoot()
    {
        return scopeRoot != null ? scopeRoot : transform;
    }
}

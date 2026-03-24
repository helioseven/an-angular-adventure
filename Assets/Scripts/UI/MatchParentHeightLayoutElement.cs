using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(LayoutElement))]
public class MatchParentHeightLayoutElement : MonoBehaviour
{
    [SerializeField]
    private RectTransform sourceRect;

    [SerializeField]
    private LayoutElement layoutElement;

    [SerializeField]
    private bool matchMinWidth = false;

    [SerializeField]
    private bool matchPreferredWidth = true;

    [SerializeField]
    private bool matchMinHeight = false;

    [SerializeField]
    private bool matchPreferredHeight = true;

    [SerializeField]
    private float multiplier = 1f;

    [SerializeField]
    private float additive;

    [SerializeField]
    private float minValue = 0f;

    [SerializeField]
    private float maxValue = -1f;

    private float _lastAppliedValue = float.NaN;

    private void Reset()
    {
        CacheReferences();
        ApplyParentHeight();
    }

    private void OnEnable()
    {
        CacheReferences();
        ApplyParentHeight();
    }

    private void OnValidate()
    {
        CacheReferences();
        ApplyParentHeight();
    }

    private void LateUpdate()
    {
        ApplyParentHeight();
    }

    private void OnTransformParentChanged()
    {
        ApplyParentHeight();
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplyParentHeight();
    }

    private void CacheReferences()
    {
        if (layoutElement == null)
            layoutElement = GetComponent<LayoutElement>();

        if (sourceRect == null)
            sourceRect = transform as RectTransform;
    }

    private void ApplyParentHeight()
    {
        CacheReferences();
        if (layoutElement == null || sourceRect == null)
            return;

        float targetValue = sourceRect.rect.height * multiplier + additive;
        if (maxValue >= 0f)
            targetValue = Mathf.Min(targetValue, maxValue);

        targetValue = Mathf.Max(targetValue, minValue);
        if (Mathf.Approximately(targetValue, _lastAppliedValue))
            return;

        if (matchMinWidth)
            layoutElement.minWidth = targetValue;
        if (matchPreferredWidth)
            layoutElement.preferredWidth = targetValue;
        if (matchMinHeight)
            layoutElement.minHeight = targetValue;
        if (matchPreferredHeight)
            layoutElement.preferredHeight = targetValue;

        _lastAppliedValue = targetValue;

        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform != null)
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }
}

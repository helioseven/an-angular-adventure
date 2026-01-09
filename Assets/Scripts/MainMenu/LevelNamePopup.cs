using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelNamePopup : MonoBehaviour
{
    [SerializeField]
    private TMP_Text textLabel;

    [SerializeField]
    private Vector2 offset = new Vector2(12f, -12f);

    [SerializeField]
    private Vector2 padding = new Vector2(12f, 8f);

    [SerializeField]
    private float maxWidth = 420f;

    private RectTransform _rect;
    private RectTransform _canvasRect;
    private Canvas _canvas;

    public void Initialize(Canvas canvas, TMP_Text label)
    {
        _canvas = canvas;
        _canvasRect = canvas.GetComponent<RectTransform>();
        textLabel = label;

        _rect = GetComponent<RectTransform>();
        if (_rect != null)
            _rect.pivot = new Vector2(0f, 1f);

        if (textLabel != null)
        {
            RectTransform labelRect = textLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(padding.x, padding.y);
            labelRect.offsetMax = new Vector2(-padding.x, -padding.y);
        }

        Hide();
    }

    public void Show(string content, TMP_Text source, Vector2 screenPos)
    {
        if (textLabel == null || string.IsNullOrEmpty(content))
            return;

        if (source != null)
        {
            textLabel.font = source.font;
            textLabel.fontSize = source.fontSize;
            textLabel.fontStyle = source.fontStyle;
            textLabel.color = Color.white;
        }

        gameObject.SetActive(true);

        textLabel.text = content;
        textLabel.textWrappingMode = TextWrappingModes.Normal;
        textLabel.overflowMode = TextOverflowModes.Overflow;
        textLabel.ForceMeshUpdate(true, true);

        float width = Mathf.Min(maxWidth, textLabel.preferredWidth) + (padding.x * 2f);
        float height = textLabel.preferredHeight + (padding.y * 2f);

        if (_rect != null)
            _rect.sizeDelta = new Vector2(width, height);

        MoveTo(screenPos);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void MoveTo(Vector2 screenPos)
    {
        if (_rect == null || _canvasRect == null)
            return;

        Camera cam = _canvas != null ? _canvas.worldCamera : null;
        if (
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                screenPos,
                cam,
                out Vector2 localPoint
            )
        )
        {
            return;
        }

        Vector2 target = localPoint + offset;
        Vector2 size = _rect.rect.size;
        Vector2 halfCanvas = _canvasRect.rect.size * 0.5f;

        float minX = -halfCanvas.x + padding.x;
        float maxX = halfCanvas.x - padding.x - size.x;
        float maxY = halfCanvas.y - padding.y;
        float minY = -halfCanvas.y + padding.y + size.y;

        target.x = Mathf.Clamp(target.x, minX, maxX);
        target.y = Mathf.Clamp(target.y, minY, maxY);

        _rect.anchoredPosition = target;
    }
}

using UnityEngine;
using UnityEngine.UI;

public class LevelPreviewPopup : MonoBehaviour
{
    [SerializeField]
    private Image previewImage;

    [SerializeField]
    private Vector2 popupSize = new Vector2(320f, 180f);

    [SerializeField]
    private Vector2 offset = new Vector2(16f, -16f);

    [SerializeField]
    private Vector2 padding = new Vector2(12f, 12f);

    private RectTransform _rect;
    private RectTransform _canvasRect;
    private Canvas _canvas;

    public void Initialize(Canvas canvas, Image image)
    {
        _canvas = canvas;
        _canvasRect = canvas.GetComponent<RectTransform>();
        previewImage = image;

        _rect = GetComponent<RectTransform>();
        if (_rect != null)
        {
            _rect.pivot = new Vector2(0f, 1f);
            _rect.sizeDelta = popupSize;
        }

        if (previewImage != null)
        {
            previewImage.preserveAspect = true;
            RectTransform imageRect = previewImage.rectTransform;
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
        }

        Hide();
    }

    public void Show(Sprite sprite, Vector2 screenPos)
    {
        if (sprite == null || previewImage == null)
            return;

        previewImage.sprite = sprite;
        gameObject.SetActive(true);
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

using UnityEngine;
using UnityEngine.UI;

public class AlphaHitTestImage : Image
{
    [Range(0, 1)]
    public float alphaThreshold = 0.001f;

    public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        Sprite sprite = this.sprite;
        if (sprite == null) return false;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out Vector2 localPoint);

        Rect rect = GetPixelAdjustedRect();
        Vector2 normalized = new Vector2(
            (localPoint.x - rect.x) / rect.width,
            (localPoint.y - rect.y) / rect.height);

        if (normalized.x < 0 || normalized.x > 1 || normalized.y < 0 || normalized.y > 1)
            return false;

        try
        {
            Texture2D tex = sprite.texture;
            Vector2 texCoord = new Vector2(
                sprite.rect.x + sprite.rect.width * normalized.x,
                sprite.rect.y + sprite.rect.height * normalized.y);

            Color color = tex.GetPixel((int)texCoord.x, (int)texCoord.y);
            return color.a >= alphaThreshold;
        }
        catch
        {
            return true;
        }
    }
}

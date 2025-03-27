using System.Collections;
using UnityEngine;

public class RippleEffect : MonoBehaviour
{
    public float duration = 0.75f;
    public float startScale = 0.33f;
    public float endScale = 0.75f;
    public float startAlpha = 0.9f;
    public SpriteRenderer rippleSprite;

    void OnEnable()
    {
        StartCoroutine(PlayRipple());
    }

    private IEnumerator PlayRipple()
    {
        float timer = 0f;
        rippleSprite.color = new Color(1f, 1f, 1f, startAlpha);
        rippleSprite.transform.localScale = Vector3.one * startScale;

        while (timer < duration)
        {
            float t = timer / duration;
            float scale = Mathf.Lerp(startScale, endScale, t);
            float alpha = Mathf.Lerp(startAlpha, 0f, t);

            rippleSprite.transform.localScale = Vector3.one * scale;
            rippleSprite.color = new Color(1f, 1f, 1f, alpha);

            timer += Time.deltaTime;
            yield return null;
        }

        gameObject.SetActive(false);
    }
}

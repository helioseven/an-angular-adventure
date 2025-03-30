using System.Collections;
using UnityEngine;

public class PlaytestWatermark : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private float minAlpha = 0.3f;

    [SerializeField]
    private float maxAlpha = 1f;

    [SerializeField]
    private float pulseSpeed = 1.5f;

    private void OnEnable()
    {
        StartCoroutine(Pulse());
    }

    private IEnumerator Pulse()
    {
        while (true)
        {
            // Fade out
            yield return StartCoroutine(Fade(maxAlpha, minAlpha));
            // Fade in
            yield return StartCoroutine(Fade(minAlpha, maxAlpha));
        }
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * pulseSpeed;
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}

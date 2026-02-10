using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// How-to-play modal with confirm/cancel actions.
/// </summary>
public class HowToPlayModal : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text headerText;
    public TMP_Text bodyText;
    public Button confirmButton;
    public Button cancelButton;
    public CanvasGroup canvasGroup;

    private Action onConfirm;
    private Action onCancel;

    private void Awake()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(() =>
            {
                onConfirm?.Invoke();
                Hide();
            });
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(() =>
            {
                onCancel?.Invoke();
                Hide();
            });
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }

    public void Show(string header, string body, Action confirmAction, Action cancelAction = null)
    {
        if (headerText != null)
            headerText.text = header;
        if (bodyText != null)
            bodyText.text = body;

        onConfirm = confirmAction;
        onCancel = cancelAction;

        SetButtonActive(confirmButton, confirmAction != null);
        SetButtonActive(cancelButton, cancelAction != null);

        gameObject.SetActive(true);
    }

    private void OnEnable()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            StartCoroutine(FadeIn());
        }
    }

    public void Hide()
    {
        StopAllCoroutines();
        if (canvasGroup != null)
            StartCoroutine(FadeOut());
        else
            gameObject.SetActive(false);
    }

    private IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 4f;
            canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
            yield return null;
        }
    }

    private IEnumerator FadeOut()
    {
        float t = 1f;
        while (t > 0f)
        {
            t -= Time.unscaledDeltaTime * 4f;
            canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
            yield return null;
        }
        gameObject.SetActive(false);
    }

    private static void SetButtonActive(Button button, bool active)
    {
        if (button == null)
            return;

        button.gameObject.SetActive(active);
    }
}

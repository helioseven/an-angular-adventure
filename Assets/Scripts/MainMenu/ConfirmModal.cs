using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple reusable confirmation modal with header, body, and cancel/confirm buttons.
/// </summary>
public class ConfirmModal : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text headerText;
    public TMP_Text bodyText;
    public Button cancelButton;
    public Button confirmButton;
    public CanvasGroup canvasGroup;

    private Action onConfirm;
    private Action onCancel;

    private void Awake()
    {
        // Ensure buttons are wired up once
        cancelButton.onClick.AddListener(() =>
        {
            onCancel?.Invoke();
            Hide();
        });
        confirmButton.onClick.AddListener(() =>
        {
            onConfirm?.Invoke();
            Hide();
        });
        if (canvasGroup != null)
            canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Displays the modal with given text and actions.
    /// </summary>
    public void Show(string header, string body, Action confirmAction, Action cancelAction = null)
    {
        headerText.text = header;
        bodyText.text = body;
        onConfirm = confirmAction;
        onCancel = cancelAction;

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

    /// <summary>
    /// Hides the modal (optionally with fade-out).
    /// </summary>
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
        float t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 4f;
            canvasGroup.alpha = Mathf.SmoothStep(0, 1, t);
            yield return null;
        }
    }

    private IEnumerator FadeOut()
    {
        float t = 1f;
        while (t > 0f)
        {
            t -= Time.unscaledDeltaTime * 4f;
            canvasGroup.alpha = Mathf.SmoothStep(0, 1, t);
            yield return null;
        }
        gameObject.SetActive(false);
    }
}

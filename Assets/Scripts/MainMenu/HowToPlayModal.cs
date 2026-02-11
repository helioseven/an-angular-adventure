using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
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

    [Header("Scroll")]
    public ScrollRect scrollRect;
    public float thumbstickScrollSpeed = 2.2f;
    public float thumbstickDeadzone = 0.1f;
    public float thumbstickInputSmoothing = 20f;
    public float thumbstickScrollPixelsPerSecond = 1200f;

    private Action onConfirm;
    private Action onCancel;
    private float thumbstickInput;
    private bool suppressNavigate;
    private InputSystemUIInputModule uiInputModule;

    private void Awake()
    {
        if (scrollRect == null)
            scrollRect = GetComponentInChildren<ScrollRect>(true);

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
        InputModeTracker.EnsureInstance();

        var jiggle = GetComponent<SelectedJiggle>();
        if (jiggle == null)
            jiggle = gameObject.AddComponent<SelectedJiggle>();
        jiggle.SetScope(transform);
        jiggle.SetAmplitude(3f);

        var adapter = GetComponent<MenuInputModeAdapter>();
        if (adapter == null)
            adapter = gameObject.AddComponent<MenuInputModeAdapter>();
        adapter.SetScope(transform);
        adapter.SetPreferred(confirmButton);

        if (
            InputModeTracker.Instance != null
            && InputModeTracker.Instance.CurrentMode == InputMode.Navigation
        )
        {
            MenuFocusUtility.SelectPreferred(gameObject, confirmButton);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            StartCoroutine(FadeIn());
        }
    }

    private void Update()
    {
        var pad = Gamepad.current;
        if (pad == null)
            return;

        if (pad.buttonEast.wasPressedThisFrame)
        {
            onCancel?.Invoke();
            Hide();
            return;
        }

        if (scrollRect == null)
            return;

        float rawInput = pad.rightStick.ReadUnprocessedValue().y;
        float target = Mathf.Abs(rawInput) < thumbstickDeadzone ? 0f : rawInput;
        float t = 1f - Mathf.Exp(-thumbstickInputSmoothing * Time.unscaledDeltaTime);
        thumbstickInput = Mathf.Lerp(thumbstickInput, target, t);

        if (Mathf.Abs(thumbstickInput) < 0.001f)
        {
            if (suppressNavigate)
            {
                suppressNavigate = false;
                InputManager.Instance?.Controls.UI.Navigate.Enable();
                SetUINavigateEnabled(true);
            }
            return;
        }

        if (!suppressNavigate)
        {
            suppressNavigate = true;
            InputManager.Instance?.Controls.UI.Navigate.Disable();
            SetUINavigateEnabled(false);
        }

        ApplyThumbstickScroll(scrollRect, thumbstickInput, thumbstickScrollPixelsPerSecond);
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

    private static void ApplyThumbstickScroll(ScrollRect target, float input, float pixelsPerSecond)
    {
        if (target == null || target.content == null)
            return;

        target.StopMovement();
        target.velocity = Vector2.zero;

        RectTransform viewport =
            target.viewport != null ? target.viewport : target.GetComponent<RectTransform>();
        if (viewport == null)
            return;

        float contentHeight = target.content.rect.height;
        float viewHeight = viewport.rect.height;
        float maxScroll = Mathf.Max(0f, contentHeight - viewHeight);
        if (maxScroll <= 0.001f)
            return;

        Vector2 anchored = target.content.anchoredPosition;
        float delta = -input * pixelsPerSecond * Time.unscaledDeltaTime;
        anchored.y = Mathf.Clamp(anchored.y + delta, 0f, maxScroll);
        target.content.anchoredPosition = anchored;
    }

    private void SetUINavigateEnabled(bool enabled)
    {
        if (uiInputModule == null)
        {
            var current = EventSystem.current;
            if (current != null)
                uiInputModule = current.GetComponent<InputSystemUIInputModule>();
        }

        var move = uiInputModule != null ? uiInputModule.move : null;
        if (move == null || move.action == null)
            return;

        if (enabled)
            move.action.Enable();
        else
            move.action.Disable();
    }
}

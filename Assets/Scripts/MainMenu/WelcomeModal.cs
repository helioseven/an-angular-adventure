using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Welcome modal with four optional actions: confirm, cancel, discord, and wishlist.
/// </summary>
public class WelcomeModal : MonoBehaviour
{
    [Header("UI References")]
    public Button confirmButton;
    public Button cancelButton;
    public Button discordButton;
    public Button wishlistButton;

    private Action onConfirm;
    private Action onCancel;
    private Action onDiscord;
    private Action onWishlist;
    private Coroutine focusRoutine;

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

        if (discordButton != null)
        {
            discordButton.onClick.AddListener(() =>
            {
                onDiscord?.Invoke();
            });
        }

        if (wishlistButton != null)
        {
            wishlistButton.onClick.AddListener(() =>
            {
                onWishlist?.Invoke();
            });
        }

        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        InputModeTracker.EnsureInstance();

        var jiggle = GetComponent<SelectedJiggle>();
        if (jiggle == null)
            jiggle = gameObject.AddComponent<SelectedJiggle>();
        jiggle.SetScope(transform);

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
    }

    private void OnDisable()
    {
        if (focusRoutine != null)
        {
            StopCoroutine(focusRoutine);
            focusRoutine = null;
        }
    }

    private void Update()
    {
        var pad = Gamepad.current;
        if (pad == null)
            return;

        if (pad.buttonEast.wasPressedThisFrame || pad.startButton.wasPressedThisFrame)
        {
            onCancel?.Invoke();
            Hide();
        }
    }

    public void Show(
        string header,
        string body,
        Action confirmAction,
        Action cancelAction = null,
        Action discordAction = null,
        Action wishlistAction = null
    )
    {
        onConfirm = confirmAction;
        onCancel = cancelAction;
        onDiscord = discordAction;
        onWishlist = wishlistAction;

        SetButtonActive(confirmButton, confirmAction != null);
        SetButtonActive(cancelButton, cancelAction != null);
        SetButtonActive(discordButton, discordAction != null);
        SetButtonActive(wishlistButton, wishlistAction != null);

        gameObject.SetActive(true);
        if (focusRoutine != null)
            StopCoroutine(focusRoutine);
        focusRoutine = StartCoroutine(EnsureFocusNextFrame());
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private static void SetButtonActive(Button button, bool active)
    {
        if (button == null)
            return;

        button.gameObject.SetActive(active);
    }

    private Selectable GetPreferredButton()
    {
        if (confirmButton != null && confirmButton.gameObject.activeInHierarchy)
            return confirmButton;
        if (cancelButton != null && cancelButton.gameObject.activeInHierarchy)
            return cancelButton;
        if (discordButton != null && discordButton.gameObject.activeInHierarchy)
            return discordButton;
        if (wishlistButton != null && wishlistButton.gameObject.activeInHierarchy)
            return wishlistButton;

        return null;
    }

    private IEnumerator EnsureFocusNextFrame()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        yield return new WaitForEndOfFrame();

        if (!gameObject.activeInHierarchy)
            yield break;

        if (
            InputModeTracker.Instance != null
            && InputModeTracker.Instance.CurrentMode == InputMode.Navigation
        )
        {
            MenuFocusUtility.SelectPreferred(gameObject, GetPreferredButton());
        }
    }
}

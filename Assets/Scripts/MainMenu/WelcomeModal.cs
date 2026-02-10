using System;
using System.Collections;
using TMPro;
using UnityEngine;
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
}

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelListItemUI
    : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerMoveHandler
{
    public TMP_Text levelNameText;
    public TMP_Text creatorNameText;
    public Button playButton;
    public Button editOrRemixButton;
    public TMP_Text editOrRemixButtonText;
    public Button deleteButton;
    public Image previewImage;
    public LevelBrowser parent;

    // Reuse decoded preview sprites across list rebuilds to avoid repeated base64 decoding.
    private static readonly Dictionary<string, Sprite> PreviewCache = new();

    private bool _isHovering;
    private bool _isNameHovering;
    private LevelInfo _info;

    public void Setup(
        LevelInfo info,
        System.Action onPlay,
        System.Action onEditOrRemix,
        LevelBrowser browser
    )
    {
        _info = info;
        levelNameText.text = info.name + (info.isLocal ? " (Draft)" : "");
        string creatorLabel = info.uploaderDisplayName;

        // attempt to fall back to uploader id if no creatorLabel present
        if (string.IsNullOrEmpty(creatorLabel))
            creatorLabel = string.IsNullOrEmpty(info.uploaderId)
                ? "Unknown creator"
                : info.uploaderId;

        string bestTimeLabel = null;
        bool isRemote = !info.isLocal && !info.isBundled;
        bool hasBest = isRemote
            ? BestTimeStore.TryGetBestTimeForRemote(info.id, out float bestSeconds)
            : BestTimeStore.TryGetBestTime(info.name, info.dataHash, out bestSeconds);
        if (hasBest)
        {
            bestTimeLabel = Clock.FormatTimeSeconds(bestSeconds);
        }

        creatorNameText.text =
            bestTimeLabel == null ? creatorLabel : $"{creatorLabel} - Best {bestTimeLabel}";

        editOrRemixButtonText.text = info.isLocal ? "Edit" : "Remix";

        parent = browser;

        ApplyPreview(info);

        playButton.onClick.RemoveAllListeners();
        playButton.onClick.AddListener(() => onPlay());

        editOrRemixButton.onClick.RemoveAllListeners();
        editOrRemixButton.onClick.AddListener(() => onEditOrRemix());

        if (StartupManager.DemoModeEnabled && editOrRemixButton != null)
            editOrRemixButton.gameObject.SetActive(false);

        // for the delete button level ownership check, we consider them the owner if
        // they uploaded it OR the level is local
        bool isOwner = info.uploaderId == AuthState.Instance.SteamId || info.isLocal;
        if (StartupManager.DemoModeEnabled)
            isOwner = false;

        // Only show delete for "owned" levels
        deleteButton.gameObject.SetActive(isOwner);
        if (isOwner)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() =>
            {
                parent.ShowConfirmDelete(
                    info.isLocal ? info.name : info.id, // pass name for local, id for cloud
                    info.name,
                    info.isLocal
                );
            });
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        UpdateHover(eventData);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        UpdateHover(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        _isNameHovering = false;
        parent?.HidePreview();
        parent?.HideNamePopup();
    }

    private void ApplyPreview(LevelInfo info)
    {
        if (previewImage == null)
            return;

        previewImage.preserveAspect = true;
        previewImage.raycastTarget = false;

        if (info == null || info.preview == null || string.IsNullOrEmpty(info.preview.data))
        {
            previewImage.sprite = null;
            previewImage.gameObject.SetActive(false);
            return;
        }

        string cacheKey = BuildPreviewCacheKey(info);
        if (!PreviewCache.TryGetValue(cacheKey, out Sprite sprite))
        {
            try
            {
                byte[] pngBytes = Convert.FromBase64String(info.preview.data);
                if (pngBytes.Length == 0)
                    throw new InvalidOperationException("Preview data is empty.");

                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (!texture.LoadImage(pngBytes))
                    throw new InvalidOperationException("Preview PNG failed to load.");

                sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
                PreviewCache[cacheKey] = sprite;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LevelListItemUI] Preview decode failed: {e.Message}");
                previewImage.sprite = null;
                previewImage.gameObject.SetActive(false);
                return;
            }
        }

        previewImage.sprite = sprite;
        previewImage.gameObject.SetActive(true);
    }

    private void TryShowPreview(Vector2 screenPos)
    {
        if (parent == null || previewImage == null || previewImage.sprite == null)
            return;

        parent.ShowPreview(previewImage.sprite, screenPos);
    }

    private void UpdateHover(PointerEventData eventData)
    {
        bool overPreview = IsOverPreview(eventData);
        if (overPreview && !_isHovering)
        {
            _isHovering = true;
            TryShowPreview(eventData.position);
        }
        else if (!overPreview && _isHovering)
        {
            _isHovering = false;
            parent?.HidePreview();
        }

        if (_isHovering && parent != null)
            parent.MovePreview(eventData.position);

        UpdateNameHover(eventData);
    }

    private bool IsOverPreview(PointerEventData eventData)
    {
        if (previewImage == null)
            return false;

        RectTransform previewRect = previewImage.rectTransform;
        Camera cam =
            eventData.enterEventCamera != null
                ? eventData.enterEventCamera
                : eventData.pressEventCamera;
        return RectTransformUtility.RectangleContainsScreenPoint(
            previewRect,
            eventData.position,
            cam
        );
    }

    private void UpdateNameHover(PointerEventData eventData)
    {
        if (levelNameText == null || parent == null)
            return;

        bool overName = IsOverText(levelNameText, eventData);
        levelNameText.ForceMeshUpdate();
        float visibleWidth = levelNameText.rectTransform.rect.width;
        bool isOverflowing =
            levelNameText.isTextOverflowing || levelNameText.preferredWidth > (visibleWidth + 0.5f);
        bool shouldShow = overName && isOverflowing;

        if (shouldShow && !_isNameHovering)
        {
            _isNameHovering = true;
            parent.ShowNamePopup(levelNameText.text, levelNameText, eventData.position);
        }
        else if (!shouldShow && _isNameHovering)
        {
            _isNameHovering = false;
            parent.HideNamePopup();
        }

        if (_isNameHovering)
            parent.MoveNamePopup(eventData.position);
    }

    private static bool IsOverText(TMP_Text text, PointerEventData eventData)
    {
        RectTransform rect = text.rectTransform;
        Camera cam =
            eventData.enterEventCamera != null
                ? eventData.enterEventCamera
                : eventData.pressEventCamera;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, eventData.position, cam);
    }

    private static string BuildPreviewCacheKey(LevelInfo info)
    {
        string id = string.IsNullOrEmpty(info.id) ? info.name : info.id;
        int dataHash = info.preview?.data?.GetHashCode() ?? 0;
        return $"{id}:{dataHash}";
    }

    // Supabase - callback function after deleting
    public void callback(bool success)
    {
        Debug.Log("Delete successful: " + success);
        parent.RefreshList();
    }
}

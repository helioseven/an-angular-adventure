using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum PromptDeviceFamily
{
    KeyboardMouse,
    Xbox,
    PlayStation,
}

[ExecuteAlways]
public class PromptRowView : MonoBehaviour
{
    [Serializable]
    private struct DevicePromptContent
    {
        public Sprite[] sprites;
    }

    [Header("Authored UI")]
    [SerializeField]
    private Image[] iconSlots = Array.Empty<Image>();

    [SerializeField]
    private GameObject[] iconSlotContainers = Array.Empty<GameObject>();

    [SerializeField]
    private TMP_Text labelText;

    [Header("Per-Device Content")]
    [SerializeField]
    private DevicePromptContent keyboardMouse;

    [SerializeField]
    private DevicePromptContent xbox;

    [SerializeField]
    private DevicePromptContent playStation;

    public void Refresh(PromptDeviceFamily deviceFamily)
    {
        DevicePromptContent content = GetContent(deviceFamily);
        ApplySprites(content.sprites);
        ApplyLabelVisibility();
    }

    public void ClearIcons()
    {
        ApplySprites(Array.Empty<Sprite>());
        ApplyLabelVisibility();
    }

    private DevicePromptContent GetContent(PromptDeviceFamily deviceFamily)
    {
        return deviceFamily switch
        {
            PromptDeviceFamily.Xbox => xbox,
            PromptDeviceFamily.PlayStation => playStation,
            _ => keyboardMouse,
        };
    }

    private void ApplySprites(Sprite[] sprites)
    {
        int spriteCount = sprites == null ? 0 : sprites.Length;
        for (int i = 0; i < iconSlots.Length; i++)
        {
            Image slot = iconSlots[i];
            if (slot == null)
                continue;

            bool hasSprite = i < spriteCount && sprites[i] != null;
            GameObject slotContainer = GetSlotContainer(i, slot);
            if (slotContainer.activeSelf != hasSprite)
                slotContainer.SetActive(hasSprite);

            slot.sprite = hasSprite ? sprites[i] : null;
            if (hasSprite)
                EnableImagesUnderContainer(slotContainer);
        }
    }

    private GameObject GetSlotContainer(int index, Image slot)
    {
        if (
            iconSlotContainers != null
            && index < iconSlotContainers.Length
            && iconSlotContainers[index] != null
        )
        {
            return iconSlotContainers[index];
        }

        return slot.gameObject;
    }

    private static void EnableImagesUnderContainer(GameObject slotContainer)
    {
        if (slotContainer == null)
            return;

        Image[] images = slotContainer.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null)
                images[i].enabled = true;
        }
    }

    private void ApplyLabelVisibility()
    {
        if (labelText == null)
            return;

        bool hasText = !string.IsNullOrWhiteSpace(labelText.text);
        GameObject labelObject = labelText.gameObject;
        if (labelObject.activeSelf != hasText)
            labelObject.SetActive(hasText);
    }

    private void OnValidate()
    {
        if (iconSlots == null)
            iconSlots = Array.Empty<Image>();
        if (iconSlotContainers == null)
            iconSlotContainers = Array.Empty<GameObject>();

        ApplyLabelVisibility();
    }
}

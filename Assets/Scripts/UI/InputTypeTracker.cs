using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[ExecuteAlways]
public class InputTypeTracker : MonoBehaviour
{
    [Header("Rows")]
    [SerializeField]
    private PromptRowView[] rows = Array.Empty<PromptRowView>();

    [SerializeField]
    private bool autoCollectRows = true;

    [SerializeField]
    private Transform rowScopeRoot;

    [Header("Editor Preview")]
    [SerializeField]
    private PromptDeviceFamily editorPreviewDevice = PromptDeviceFamily.KeyboardMouse;

    private PromptDeviceFamily _latchedRuntimeDevice = PromptDeviceFamily.KeyboardMouse;
    private PromptDeviceFamily _appliedDevice;
    private bool _hasAppliedDevice;

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            InputModeTracker.EnsureInstance();
            InputModeTracker.OnModeChanged += HandleInputModeChanged;
            _latchedRuntimeDevice = PromptDeviceFamily.KeyboardMouse;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
            return;

        InputModeTracker.OnModeChanged -= HandleInputModeChanged;
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        UpdateLatchedRuntimeDevice();
        RefreshIfNeeded();
    }

    public void Refresh()
    {
        ApplyDeviceFamily(GetCurrentDeviceFamily());
    }

    public void Refresh(PromptDeviceFamily deviceFamily)
    {
        ApplyDeviceFamily(deviceFamily);
    }

    public void RebuildRows()
    {
        if (!autoCollectRows)
            return;

        rows = CollectRows();
    }

    private void HandleInputModeChanged(InputMode _)
    {
        UpdateLatchedRuntimeDevice();
        Refresh();
    }

    private void RefreshIfNeeded()
    {
        PromptDeviceFamily current = GetCurrentDeviceFamily();
        if (_hasAppliedDevice && _appliedDevice == current)
            return;

        ApplyDeviceFamily(current);
    }

    private void ApplyDeviceFamily(PromptDeviceFamily deviceFamily)
    {
        if (autoCollectRows)
            rows = CollectRows();

        _appliedDevice = deviceFamily;
        _hasAppliedDevice = true;

        foreach (PromptRowView row in rows)
        {
            if (row == null)
                continue;

            row.Refresh(deviceFamily);
        }
    }

    private PromptDeviceFamily GetCurrentDeviceFamily()
    {
        if (!Application.isPlaying)
            return editorPreviewDevice;

        return _latchedRuntimeDevice;
    }

    private void UpdateLatchedRuntimeDevice()
    {
        if (HasKeyboardOrPointerInputThisFrame())
        {
            _latchedRuntimeDevice = PromptDeviceFamily.KeyboardMouse;
            return;
        }

        if (HasGamepadInputThisFrame())
            _latchedRuntimeDevice = DetectGamepadFamily(Gamepad.current);
    }

    private static bool HasKeyboardOrPointerInputThisFrame()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            return true;

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            if (
                mouse.leftButton.wasPressedThisFrame
                || mouse.rightButton.wasPressedThisFrame
                || mouse.middleButton.wasPressedThisFrame
                || mouse.scroll.ReadValue().sqrMagnitude > 0.01f
                || mouse.delta.ReadValue().sqrMagnitude > 0.01f
            )
            {
                return true;
            }
        }

        Touchscreen touch = Touchscreen.current;
        if (touch == null)
            return false;

        var primaryTouch = touch.primaryTouch;
        return primaryTouch.press.wasPressedThisFrame
            || (primaryTouch.press.isPressed && primaryTouch.delta.ReadValue().sqrMagnitude > 1f);
    }

    private static bool HasGamepadInputThisFrame()
    {
        Gamepad pad = Gamepad.current;
        if (pad == null)
            return false;

        if (pad.leftStick.ReadValue().sqrMagnitude > 0.04f)
            return true;
        if (pad.rightStick.ReadValue().sqrMagnitude > 0.04f)
            return true;
        if (pad.dpad.ReadValue().sqrMagnitude > 0.1f)
            return true;

        return pad.buttonSouth.wasPressedThisFrame
            || pad.buttonNorth.wasPressedThisFrame
            || pad.buttonEast.wasPressedThisFrame
            || pad.buttonWest.wasPressedThisFrame
            || pad.startButton.wasPressedThisFrame
            || pad.selectButton.wasPressedThisFrame
            || pad.leftShoulder.wasPressedThisFrame
            || pad.rightShoulder.wasPressedThisFrame
            || pad.leftTrigger.ReadValue() > 0.5f
            || pad.rightTrigger.ReadValue() > 0.5f;
    }

    private static PromptDeviceFamily DetectGamepadFamily(Gamepad gamepad)
    {
        if (gamepad == null)
            return PromptDeviceFamily.Xbox;

        string probe =
            $"{gamepad.layout} {gamepad.displayName} {gamepad.description.interfaceName} "
            + $"{gamepad.description.manufacturer} {gamepad.description.product}";
        string normalized = probe.ToLowerInvariant();

        if (
            normalized.Contains("playstation")
            || normalized.Contains("dualshock")
            || normalized.Contains("dualsense")
            || normalized.Contains("wireless controller")
            || normalized.Contains("sony")
        )
        {
            return PromptDeviceFamily.PlayStation;
        }

        return PromptDeviceFamily.Xbox;
    }

    private PromptRowView[] CollectRows()
    {
        Transform scope = rowScopeRoot != null ? rowScopeRoot : transform.root;
        if (scope == null)
            return Array.Empty<PromptRowView>();

        PromptRowView[] found = scope.GetComponentsInChildren<PromptRowView>(true);
        if (found == null || found.Length == 0)
            return Array.Empty<PromptRowView>();

        List<PromptRowView> collected = new(found.Length);
        foreach (PromptRowView row in found)
        {
            if (row == null)
                continue;

            collected.Add(row);
        }

        return collected.ToArray();
    }

    private void OnValidate()
    {
        if (rows == null)
            rows = Array.Empty<PromptRowView>();

        RebuildRows();
        Refresh();
    }
}

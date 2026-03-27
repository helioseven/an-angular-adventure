using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public enum InputMode
{
    Pointer,
    Navigation,
}

public class InputModeTracker : MonoBehaviour
{
    public static InputModeTracker Instance { get; private set; }
    public static event Action<InputMode> OnModeChanged;

    public InputMode CurrentMode { get; private set; } = InputMode.Navigation;
    public bool IsGamepadNavigationActive { get; private set; }
    public PromptDeviceFamily LastPromptDeviceFamily { get; private set; } =
        PromptDeviceFamily.KeyboardMouse;

    private Vector2 lastMousePos;

    [SerializeField]
    private float pointerGraceSeconds = 0.5f;

    [SerializeField]
    private bool hideCursorDuringGamepadNavigation = true;
    private float startTime;
    private bool pointerBaselineReady;
    private bool _cursorHiddenByTracker;

    public static void EnsureInstance()
    {
        if (Instance != null)
            return;

        var go = new GameObject("InputModeTracker");
        Instance = go.AddComponent<InputModeTracker>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        startTime = Time.unscaledTime;

        if (Mouse.current != null)
            lastMousePos = Mouse.current.position.ReadValue();

        ApplyCursorVisibility();
    }

    private void OnDisable()
    {
        RestoreCursorIfNeeded();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        RestoreCursorIfNeeded();
    }

    private void Update()
    {
        if (IsPointerActive())
        {
            IsGamepadNavigationActive = false;
            LastPromptDeviceFamily = PromptDeviceFamily.KeyboardMouse;
            ApplyCursorVisibility();
            SetMode(InputMode.Pointer);
            return;
        }

        if (IsNavigationActive())
        {
            ApplyCursorVisibility();
            SetMode(InputMode.Navigation);
        }
    }

    private void SetMode(InputMode mode)
    {
        if (CurrentMode == mode)
            return;

        CurrentMode = mode;
        OnModeChanged?.Invoke(mode);
    }

    private bool IsPointerActive()
    {
        if (Time.unscaledTime - startTime < pointerGraceSeconds)
            return false;

        if (Mouse.current != null)
        {
            if (
                Mouse.current.leftButton.wasPressedThisFrame
                || Mouse.current.rightButton.wasPressedThisFrame
                || Mouse.current.middleButton.wasPressedThisFrame
                || Mouse.current.scroll.ReadValue().sqrMagnitude > 0.01f
            )
                return true;

            Vector2 pos = Mouse.current.position.ReadValue();
            if (!pointerBaselineReady)
            {
                lastMousePos = pos;
                pointerBaselineReady = true;
                return false;
            }
            if ((pos - lastMousePos).sqrMagnitude > 1f)
            {
                lastMousePos = pos;
                return true;
            }
        }

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame)
                return true;
            if (touch.press.isPressed && touch.delta.ReadValue().sqrMagnitude > 1f)
                return true;
        }

        return false;
    }

    private bool IsNavigationActive()
    {
        if (IsGamepadActive())
        {
            IsGamepadNavigationActive = true;
            return true;
        }

        IsGamepadNavigationActive = false;

        var keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        if (IsTextInputFocused())
        {
            if (
                keyboard.tabKey.wasPressedThisFrame
                || keyboard.enterKey.wasPressedThisFrame
                || keyboard.escapeKey.wasPressedThisFrame
            )
            {
                return true;
            }

            return false;
        }

        if (
            keyboard.upArrowKey.wasPressedThisFrame
            || keyboard.downArrowKey.wasPressedThisFrame
            || keyboard.leftArrowKey.wasPressedThisFrame
            || keyboard.rightArrowKey.wasPressedThisFrame
            || keyboard.wKey.wasPressedThisFrame
            || keyboard.aKey.wasPressedThisFrame
            || keyboard.sKey.wasPressedThisFrame
            || keyboard.dKey.wasPressedThisFrame
            || keyboard.tabKey.wasPressedThisFrame
            || keyboard.enterKey.wasPressedThisFrame
            || keyboard.spaceKey.wasPressedThisFrame
            || keyboard.escapeKey.wasPressedThisFrame
        )
            return true;

        return false;
    }

    private static bool IsTextInputFocused()
    {
        if (EventSystem.current == null)
            return false;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null)
            return false;

        TMP_InputField inputField = selected.GetComponent<TMP_InputField>();
        return inputField != null && inputField.isFocused;
    }

    private bool IsGamepadActive()
    {
        var pad = Gamepad.current;
        if (pad == null)
            return false;

        if (pad.leftStick.ReadValue().sqrMagnitude > 0.04f)
        {
            LastPromptDeviceFamily = DetectPromptDeviceFamily(pad);
            return true;
        }
        if (pad.rightStick.ReadValue().sqrMagnitude > 0.04f)
        {
            LastPromptDeviceFamily = DetectPromptDeviceFamily(pad);
            return true;
        }
        if (pad.dpad.ReadValue().sqrMagnitude > 0.1f)
        {
            LastPromptDeviceFamily = DetectPromptDeviceFamily(pad);
            return true;
        }

        if (
            pad.buttonSouth.wasPressedThisFrame
            || pad.buttonNorth.wasPressedThisFrame
            || pad.buttonEast.wasPressedThisFrame
            || pad.buttonWest.wasPressedThisFrame
            || pad.startButton.wasPressedThisFrame
            || pad.selectButton.wasPressedThisFrame
            || pad.leftShoulder.wasPressedThisFrame
            || pad.rightShoulder.wasPressedThisFrame
            || pad.leftTrigger.ReadValue() > 0.5f
            || pad.rightTrigger.ReadValue() > 0.5f
        )
        {
            LastPromptDeviceFamily = DetectPromptDeviceFamily(pad);
            return true;
        }

        return false;
    }

    private static PromptDeviceFamily DetectPromptDeviceFamily(Gamepad gamepad)
    {
        return InputTypeTracker.DetectGamepadFamily(gamepad);
    }

    private void ApplyCursorVisibility()
    {
        bool shouldHideCursor = hideCursorDuringGamepadNavigation && IsGamepadNavigationActive;
        Cursor.visible = !shouldHideCursor;
        _cursorHiddenByTracker = shouldHideCursor;
    }

    private void RestoreCursorIfNeeded()
    {
        if (!_cursorHiddenByTracker)
            return;

        Cursor.visible = true;
        _cursorHiddenByTracker = false;
    }
}

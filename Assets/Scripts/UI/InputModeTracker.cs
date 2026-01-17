using System;
using UnityEngine;
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

    private Vector2 lastMousePos;
    [SerializeField]
    private float pointerGraceSeconds = 0.5f;
    private float startTime;
    private bool pointerBaselineReady;

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
    }

    private void Update()
    {
        if (IsPointerActive())
        {
            SetMode(InputMode.Pointer);
            return;
        }

        if (IsNavigationActive())
            SetMode(InputMode.Navigation);
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
            if (Mouse.current.leftButton.wasPressedThisFrame
                || Mouse.current.rightButton.wasPressedThisFrame
                || Mouse.current.middleButton.wasPressedThisFrame
                || Mouse.current.scroll.ReadValue().sqrMagnitude > 0.01f)
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
            return true;

        var keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        if (keyboard.upArrowKey.wasPressedThisFrame
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
            || keyboard.escapeKey.wasPressedThisFrame)
            return true;

        return false;
    }

    private bool IsGamepadActive()
    {
        var pad = Gamepad.current;
        if (pad == null)
            return false;

        if (pad.leftStick.ReadValue().sqrMagnitude > 0.04f)
            return true;
        if (pad.rightStick.ReadValue().sqrMagnitude > 0.04f)
            return true;
        if (pad.dpad.ReadValue().sqrMagnitude > 0.1f)
            return true;

        if (pad.buttonSouth.wasPressedThisFrame
            || pad.buttonNorth.wasPressedThisFrame
            || pad.buttonEast.wasPressedThisFrame
            || pad.buttonWest.wasPressedThisFrame
            || pad.startButton.wasPressedThisFrame
            || pad.selectButton.wasPressedThisFrame
            || pad.leftShoulder.wasPressedThisFrame
            || pad.rightShoulder.wasPressedThisFrame
            || pad.leftTrigger.ReadValue() > 0.5f
            || pad.rightTrigger.ReadValue() > 0.5f)
            return true;

        return false;
    }
}

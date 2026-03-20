using UnityEngine;
using UnityEngine.InputSystem;

public enum PointerSourceKind
{
    Hardware,
    Virtual,
}

public class PointerSource : MonoBehaviour
{
    public static PointerSource Instance { get; private set; }

    public PointerSourceKind CurrentSource { get; private set; } = PointerSourceKind.Hardware;

    [SerializeField]
    private float hardwareMoveThresholdSqr = 1f;

    private Vector2 _lastHardwarePosition;
    private bool _hardwareBaselineReady;
    private Vector2 _virtualScreenPosition;
    private bool _virtualPositionInitialized;
    private int _virtualPrimaryPressedFrame = -100;
    private int _virtualSecondaryPressedFrame = -100;
    private int _virtualPrimaryConsumedFrame = -100;
    private int _virtualSecondaryConsumedFrame = -100;

    public Vector2 ScreenPosition
    {
        get
        {
            if (CurrentSource == PointerSourceKind.Virtual)
                return GetVirtualScreenPosition();

            return GetHardwareScreenPosition();
        }
    }

    public bool IsVirtualActive => CurrentSource == PointerSourceKind.Virtual;
    public bool IsHardwareActive => CurrentSource == PointerSourceKind.Hardware;

    public static void EnsureInstance()
    {
        if (Instance != null)
            return;

        var go = new GameObject("PointerSource");
        Instance = go.AddComponent<PointerSource>();
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
        InitializeHardwareBaseline();
    }

    private void Update()
    {
        if (DetectHardwareActivity())
            CurrentSource = PointerSourceKind.Hardware;
    }

    public bool PrimaryPressedThisFrame()
    {
        if (CurrentSource == PointerSourceKind.Virtual)
            return WasPressedRecently(_virtualPrimaryPressedFrame, _virtualPrimaryConsumedFrame);

        return (Mouse.current?.leftButton.wasPressedThisFrame ?? false)
            || (Touchscreen.current?.primaryTouch.press.wasPressedThisFrame ?? false);
    }

    public bool SecondaryPressedThisFrame()
    {
        if (CurrentSource == PointerSourceKind.Virtual)
            return WasPressedRecently(_virtualSecondaryPressedFrame, _virtualSecondaryConsumedFrame);

        return Mouse.current?.rightButton.wasPressedThisFrame ?? false;
    }

    public void SetVirtualPosition(Vector2 screenPosition)
    {
        _virtualScreenPosition = ClampToScreen(screenPosition);
        _virtualPositionInitialized = true;
        CurrentSource = PointerSourceKind.Virtual;
    }

    public void MoveVirtual(Vector2 screenDelta)
    {
        if (!_virtualPositionInitialized)
            _virtualScreenPosition = GetHardwareScreenPosition();

        SetVirtualPosition(_virtualScreenPosition + screenDelta);
    }

    public void PressVirtualPrimary()
    {
        _virtualPrimaryPressedFrame = Time.frameCount;
        _virtualPrimaryConsumedFrame = -100;
        CurrentSource = PointerSourceKind.Virtual;
    }

    public void PressVirtualSecondary()
    {
        _virtualSecondaryPressedFrame = Time.frameCount;
        _virtualSecondaryConsumedFrame = -100;
        CurrentSource = PointerSourceKind.Virtual;
    }

    public void ConsumeVirtualPrimary()
    {
        _virtualPrimaryConsumedFrame = _virtualPrimaryPressedFrame;
    }

    public void ConsumeVirtualSecondary()
    {
        _virtualSecondaryConsumedFrame = _virtualSecondaryPressedFrame;
    }

    private static bool WasPressedRecently(int pressedFrame, int consumedFrame)
    {
        return (pressedFrame == Time.frameCount || pressedFrame == Time.frameCount - 1)
            && consumedFrame != pressedFrame;
    }

    private bool DetectHardwareActivity()
    {
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame
                || Mouse.current.rightButton.wasPressedThisFrame
                || Mouse.current.middleButton.wasPressedThisFrame
                || Mouse.current.scroll.ReadValue().sqrMagnitude > 0.01f)
            {
                InitializeHardwareBaseline();
                return true;
            }

            Vector2 currentPosition = Mouse.current.position.ReadValue();
            if (!_hardwareBaselineReady)
            {
                _lastHardwarePosition = currentPosition;
                _hardwareBaselineReady = true;
                return false;
            }

            if ((currentPosition - _lastHardwarePosition).sqrMagnitude > hardwareMoveThresholdSqr)
            {
                _lastHardwarePosition = currentPosition;
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

    private void InitializeHardwareBaseline()
    {
        if (Mouse.current == null)
            return;

        _lastHardwarePosition = Mouse.current.position.ReadValue();
        _hardwareBaselineReady = true;
    }

    private Vector2 GetHardwareScreenPosition()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return Touchscreen.current.primaryTouch.position.ReadValue();

        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();

        return GetVirtualScreenPosition();
    }

    private Vector2 GetVirtualScreenPosition()
    {
        if (!_virtualPositionInitialized)
        {
            _virtualScreenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            _virtualPositionInitialized = true;
        }

        return ClampToScreen(_virtualScreenPosition);
    }

    private static Vector2 ClampToScreen(Vector2 screenPosition)
    {
        float maxX = Mathf.Max(0f, Screen.width - 1f);
        float maxY = Mathf.Max(0f, Screen.height - 1f);
        return new Vector2(
            Mathf.Clamp(screenPosition.x, 0f, maxX),
            Mathf.Clamp(screenPosition.y, 0f, maxY)
        );
    }
}

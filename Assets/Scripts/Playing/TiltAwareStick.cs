using System;
using System.Reflection;
using circleXsquares;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

/// <summary>
/// Feeds the on-screen gamepad left stick from a touch D-pad area and, when enabled,
/// falls back to accelerometer tilt while the D-pad is idle.
/// </summary>
[AddComponentMenu("Input/Tilt Aware Stick")]
[DefaultExecutionOrder(-50)]
public class TiltAwareStick : OnScreenControl
{
    private const string LeftName = "Left";
    private const string RightName = "Right";
    private const string TopName = "Top";
    private const string BottomName = "Bottom";

    private static readonly MethodInfo DoStateTransitionMethod = typeof(Selectable).GetMethod(
        "DoStateTransition",
        BindingFlags.Instance | BindingFlags.NonPublic
    );
    private static readonly Type SelectionStateType = typeof(Selectable).GetNestedType(
        "SelectionState",
        BindingFlags.NonPublic
    );
    private static readonly object NormalState = ParseSelectionState("Normal");
    private static readonly object PressedState = ParseSelectionState("Pressed");
    private static readonly object DisabledState = ParseSelectionState("Disabled");

    [Header("Binding")]
    [InputControl(layout = "Vector2")]
    [SerializeField]
    private string m_ControlPath = "<Gamepad>/leftStick";

    [Header("Touch D-Pad")]
    [SerializeField]
    private RectTransform touchArea;

    [SerializeField]
    private Toggle tiltToggle;

    [Header("Tilt")]
    [SerializeField]
    private bool tiltEnabled;

    [SerializeField]
    private bool autoEnableSensors = true;

    [SerializeField]
    [Min(0f)]
    private float tiltSensitivity = 2f;

    [SerializeField]
    [Min(0f)]
    private float tiltSmoothing = 8f;

    [SerializeField]
    [Range(0f, 0.95f)]
    private float tiltDeadZone = 0.15f;

    [SerializeField]
    [Range(0f, 0.95f)]
    private float tiltActivationThreshold = 0.22f;

    [SerializeField]
    private bool calibrateOnEnable = true;

    [SerializeField]
    private bool invertX;

    [SerializeField]
    private bool invertY;

    [Header("Visual Handle (optional)")]
    [SerializeField]
    private RectTransform handle;

    [SerializeField]
    [Min(0f)]
    private float handleRange = 100f;

    [SerializeField]
    [Min(0f)]
    private float handleReturnSpeed = 12f;

    [Header("D-Pad Visuals (optional)")]
    [SerializeField]
    private Button leftButton;

    [SerializeField]
    private Button rightButton;

    [SerializeField]
    private Button topButton;

    [SerializeField]
    private Button bottomButton;

    private Vector2 _smoothedTilt;
    private Vector2 _tiltCalibration;
    private bool _hasCalibration;

    protected override string controlPathInternal
    {
        get => m_ControlPath;
        set => m_ControlPath = value;
    }

    public bool TiltEnabled => tiltEnabled;

    protected override void OnEnable()
    {
        base.OnEnable();
        _smoothedTilt = Vector2.zero;
        SyncTiltStateFromToggle();

        if (tiltEnabled)
            TryEnableSensors();

        if (tiltEnabled && calibrateOnEnable)
            CalibrateTilt();

        CacheDirectionButtons();
        UpdateDirectionVisuals(Vector2.zero, true);
        UpdateHandleImmediate(Vector2.zero);
    }

    protected override void OnDisable()
    {
        UpdateDirectionVisuals(Vector2.zero, true);
        SendValue(Vector2.zero);
        base.OnDisable();
    }

    private void Update()
    {
        Vector2 nextValue = ComputeCurrentValue();
        SendValue(nextValue);
    }

    public void SetTiltEnabled(bool enabled)
    {
        if (tiltEnabled == enabled)
            return;

        tiltEnabled = enabled;
        _smoothedTilt = Vector2.zero;

        if (tiltEnabled)
        {
            TryEnableSensors();
            CalibrateTilt();
        }
        else
        {
            SendValue(Vector2.zero);
        }
    }

    private void SyncTiltStateFromToggle()
    {
        if (tiltToggle == null)
            return;

        tiltEnabled = tiltToggle.isOn;
    }

    public void CalibrateTilt()
    {
        if (TryReadRawTilt(out Vector2 rawTilt))
        {
            _tiltCalibration = rawTilt;
            _hasCalibration = true;
        }
    }

    private Vector2 ComputeCurrentValue()
    {
        Vector2 outputValue;

        if (TryGetTouchDpadValue(out Vector2 touchValue))
        {
            _smoothedTilt = Vector2.zero;
            outputValue = touchValue;
            UpdateDirectionVisuals(outputValue);
            return outputValue;
        }

        if (!tiltEnabled)
        {
            _smoothedTilt = Vector2.zero;
            outputValue = Vector2.zero;
            UpdateDirectionVisuals(outputValue);
            return outputValue;
        }

        if (!TryReadTiltValue(out Vector2 tiltValue))
        {
            _smoothedTilt = Vector2.zero;
            outputValue = Vector2.zero;
            UpdateDirectionVisuals(outputValue);
            return outputValue;
        }

        float dt = Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime : Time.deltaTime;
        if (tiltSmoothing <= 0f || dt <= 0f)
            _smoothedTilt = tiltValue;
        else
            _smoothedTilt = Vector2.Lerp(_smoothedTilt, tiltValue, 1f - Mathf.Exp(-tiltSmoothing * dt));

        outputValue = ApplyMovementThreshold(_smoothedTilt);
        UpdateDirectionVisuals(outputValue);
        return outputValue;
    }

    private bool TryGetTouchDpadValue(out Vector2 value)
    {
        value = Vector2.zero;

        RectTransform area = ResolveTouchArea();
        if (area == null || Touchscreen.current == null)
            return false;

        foreach (var touch in Touchscreen.current.touches)
        {
            if (!touch.press.isPressed)
                continue;

            Vector2 screenPosition = touch.position.ReadValue();
            if (!RectTransformUtility.RectangleContainsScreenPoint(area, screenPosition))
                continue;

            value = ComputeTouchDirection(area, screenPosition);
            return true;
        }

        return false;
    }

    private Vector2 ComputeTouchDirection(RectTransform area, Vector2 screenPosition)
    {
        if (
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                area,
                screenPosition,
                null,
                out Vector2 localPoint
            )
        )
        {
            return Vector2.zero;
        }

        Rect rect = area.rect;
        float halfWidth = Mathf.Max(rect.width * 0.5f, 0.0001f);
        float halfHeight = Mathf.Max(rect.height * 0.5f, 0.0001f);
        Vector2 normalized = new(localPoint.x / halfWidth, localPoint.y / halfHeight);
        normalized = Vector2.ClampMagnitude(normalized, 1f);

        if (normalized.sqrMagnitude <= 0.000001f)
            return Vector2.zero;

        Vector2 direction = Mathf.Abs(normalized.x) >= Mathf.Abs(normalized.y)
            ? new Vector2(Mathf.Sign(normalized.x), 0f)
            : new Vector2(0f, Mathf.Sign(normalized.y));

        return IsDirectionAvailable(direction) ? direction : Vector2.zero;
    }

    private bool TryReadTiltValue(out Vector2 value)
    {
        value = Vector2.zero;

        if (!TryReadRawTilt(out Vector2 rawTilt))
            return false;

        if (!_hasCalibration)
        {
            _tiltCalibration = rawTilt;
            _hasCalibration = true;
        }

        Vector2 adjusted = rawTilt - _tiltCalibration;
        if (invertX)
            adjusted.x = -adjusted.x;
        if (invertY)
            adjusted.y = -adjusted.y;

        value = ApplyRadialDeadZone(adjusted * tiltSensitivity, tiltDeadZone);
        return true;
    }

    private bool TryReadRawTilt(out Vector2 rawTilt)
    {
        rawTilt = Vector2.zero;

        if (Accelerometer.current == null)
            return false;

        if (!Accelerometer.current.enabled)
            InputSystem.EnableDevice(Accelerometer.current);

        Vector3 accel = Accelerometer.current.acceleration.ReadValue();
        rawTilt = new Vector2(accel.x, accel.y);
        return true;
    }

    private void TryEnableSensors()
    {
        if (!autoEnableSensors)
            return;

        if (Accelerometer.current != null && !Accelerometer.current.enabled)
            InputSystem.EnableDevice(Accelerometer.current);

        if (Touchscreen.current != null && !Touchscreen.current.enabled)
            InputSystem.EnableDevice(Touchscreen.current);

        Input.gyro.enabled = true;
    }

    private static Vector2 ApplyRadialDeadZone(Vector2 value, float deadZone)
    {
        float magnitude = value.magnitude;
        if (magnitude <= deadZone)
            return Vector2.zero;

        float scaledMagnitude = Mathf.Clamp01((magnitude - deadZone) / (1f - deadZone));
        return value.normalized * scaledMagnitude;
    }

    private RectTransform ResolveTouchArea()
    {
        if (touchArea != null)
            return touchArea;

        return transform as RectTransform;
    }

    private void CacheDirectionButtons()
    {
        RectTransform area = ResolveTouchArea();
        if (area == null)
            return;

        CacheDirectionButton(ref leftButton, area, LeftName);
        CacheDirectionButton(ref rightButton, area, RightName);
        CacheDirectionButton(ref topButton, area, TopName);
        CacheDirectionButton(ref bottomButton, area, BottomName);
    }

    private static void CacheDirectionButton(ref Button button, RectTransform root, string childName)
    {
        if (button != null || root == null)
            return;

        Transform child = root.Find(childName);
        if (child == null)
            return;

        button = child.GetComponent<Button>();
    }

    private bool IsDirectionAvailable(Vector2 direction)
    {
        Button button = GetButtonForDirection(GetDominantDirection(direction));
        return button == null || button.IsInteractable();
    }

    private void UpdateDirectionVisuals(Vector2 inputValue, bool instant = false)
    {
        CacheDirectionButtons();

        Direction activeDirection = GetMovementDirection(inputValue);

        ApplyButtonState(leftButton, activeDirection == Direction.Left, instant);
        ApplyButtonState(rightButton, activeDirection == Direction.Right, instant);
        ApplyButtonState(topButton, activeDirection == Direction.Up, instant);
        ApplyButtonState(bottomButton, activeDirection == Direction.Down, instant);
    }

    private static void ApplyButtonState(Button button, bool pressed, bool instant)
    {
        if (button == null)
            return;

        object state = !button.IsInteractable() ? DisabledState : pressed ? PressedState : NormalState;
        if (DoStateTransitionMethod != null && state != null)
        {
            DoStateTransitionMethod.Invoke(button, new[] { state, instant });
            return;
        }

        Graphic targetGraphic = button.targetGraphic;
        if (targetGraphic == null)
            return;

        ColorBlock colors = button.colors;
        Color targetColor = !button.IsInteractable()
            ? colors.disabledColor
            : pressed ? colors.pressedColor : colors.normalColor;

        if (instant || colors.fadeDuration <= 0f)
            targetGraphic.color = targetColor;
        else
            targetGraphic.CrossFadeColor(targetColor, colors.fadeDuration, true, true);
    }

    private Button GetButtonForDirection(Direction direction)
    {
        return direction switch
        {
            Direction.Left => leftButton,
            Direction.Right => rightButton,
            Direction.Up => topButton,
            Direction.Down => bottomButton,
            _ => null,
        };
    }

    private static Direction GetDominantDirection(Vector2 value)
    {
        if (value.sqrMagnitude <= 0.0001f)
            return Direction.None;

        if (Mathf.Abs(value.x) >= Mathf.Abs(value.y))
            return value.x >= 0f ? Direction.Right : Direction.Left;

        return value.y >= 0f ? Direction.Up : Direction.Down;
    }

    private static Direction GetMovementDirection(Vector2 value)
    {
        GravityDirection gravity = PlayGM.instance != null
            ? PlayGM.instance.gravDirection
            : GravityDirection.Down;

        switch (gravity)
        {
            case GravityDirection.Down:
            case GravityDirection.Up:
                if (Mathf.Abs(value.x) <= 0.0001f)
                    return Direction.None;
                return value.x >= 0f ? Direction.Right : Direction.Left;
            case GravityDirection.Left:
            case GravityDirection.Right:
                if (Mathf.Abs(value.y) <= 0.0001f)
                    return Direction.None;
                return value.y >= 0f ? Direction.Up : Direction.Down;
            default:
                return Direction.None;
        }
    }

    private Vector2 ApplyMovementThreshold(Vector2 value)
    {
        GravityDirection gravity = PlayGM.instance != null
            ? PlayGM.instance.gravDirection
            : GravityDirection.Down;

        switch (gravity)
        {
            case GravityDirection.Down:
            case GravityDirection.Up:
                return Mathf.Abs(value.x) >= tiltActivationThreshold ? value : Vector2.zero;
            case GravityDirection.Left:
            case GravityDirection.Right:
                return Mathf.Abs(value.y) >= tiltActivationThreshold ? value : Vector2.zero;
            default:
                return Vector2.zero;
        }
    }

    private static object ParseSelectionState(string stateName)
    {
        if (SelectionStateType == null)
            return null;

        try
        {
            return Enum.Parse(SelectionStateType, stateName);
        }
        catch
        {
            return null;
        }
    }

    private void SendValue(Vector2 value)
    {
        value = Vector2.ClampMagnitude(value, 1f);
        SendValueToControl(value);
        UpdateHandle(value);
    }

    private void UpdateHandle(Vector2 value)
    {
        if (handle == null)
            return;

        if (value.sqrMagnitude > 0.0001f)
        {
            handle.anchoredPosition = value * handleRange;
            return;
        }

        float dt = Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime : Time.deltaTime;
        if (handleReturnSpeed <= 0f || dt <= 0f)
        {
            handle.anchoredPosition = Vector2.zero;
            return;
        }

        handle.anchoredPosition = Vector2.MoveTowards(
            handle.anchoredPosition,
            Vector2.zero,
            handleReturnSpeed * handleRange * dt
        );
    }

    private void UpdateHandleImmediate(Vector2 value)
    {
        if (handle == null)
            return;

        handle.anchoredPosition = value * handleRange;
    }

    private enum Direction
    {
        None,
        Left,
        Right,
        Up,
        Down,
    }
}

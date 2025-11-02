using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;

/// <summary>
/// Extended OnScreenStick that supports both touch and accelerometer tilt on iOS.
/// - Works with normal touch joystick input.
/// - When not touched, automatically uses accelerometer tilt.
/// - Auto-enables motion sensors on iOS.
/// - Includes calibration, smoothing, and sensitivity controls.
/// </summary>
[DefaultExecutionOrder(-50)]
public class TiltAwareStick : OnScreenStick
{
    [Header("Tilt Settings")]
    public bool useTilt = true;
    public float tiltSensitivity = 2f;
    public float tiltSmoothing = 5f;
    public bool autoEnableSensors = true;

    [Header("UI Handle (optional)")]
    public RectTransform handle;

    [Header("UI Line (optional)")]
    public RectTransform lineImage;
    public float maxLineWidth = 20f;
    public float minLineWidth = 2f;

    private Vector2 tiltSmoothed;
    private Vector3 calibrationOffset;
    private MethodInfo sendValueVector2;

    protected void Awake()
    {
        // Prepare reflection call once for performance
        var baseMethod = typeof(OnScreenControl).GetMethod(
            "SendValueToControl",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        sendValueVector2 = baseMethod?.MakeGenericMethod(typeof(Vector2));
    }

    void Start()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (autoEnableSensors)
        {
            Input.gyro.enabled = true;

            if (Accelerometer.current != null && !Accelerometer.current.enabled)
                InputSystem.EnableDevice(Accelerometer.current);

            if (Touchscreen.current != null && !Touchscreen.current.enabled)
                InputSystem.EnableDevice(Touchscreen.current);
        }
#endif
    }

    void Update()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (!useTilt)
            return;

        // Don’t override while joystick is being touched
        if (IsJoystickBeingTouched())
            return;

        // Read accelerometer from the new Input System
        Vector3 accel = Accelerometer.current.acceleration.ReadValue();

        // Apply calibration offset
        Vector3 adjusted = accel - calibrationOffset;

        // Flat-on-table, Landscape Left mapping:
        //  - Tilt left/right → adjusted.x
        //  - Tilt toward/away → adjusted.y (toward = down → invert Y)
        float x = adjusted.x; // left/right
        float y = adjusted.y; // forward/back (toward user = down)

        Vector2 tilt = new(x, y);

        // Apply smoothing and sensitivity
        tiltSmoothed = Vector2.Lerp(
            tiltSmoothed,
            tilt * tiltSensitivity,
            Time.deltaTime * tiltSmoothing
        );

        // Clamp to unit circle
        Vector2 clamped = Vector2.ClampMagnitude(tiltSmoothed, 1f);

        // Send to the underlying control
        sendValueVector2?.Invoke(this, new object[] { clamped });

        // Tilt controls visual handle unless handle is being touched
        UpdateVisualHandle(clamped);
#endif
    }

    /// <summary>
    /// Check if any active touch is within this joystick’s area.
    /// Prevents tilt override when the user is dragging the stick.
    /// </summary>
    private bool IsJoystickBeingTouched()
    {
        if (Touchscreen.current == null)
            return false;

        RectTransform rt = GetComponent<RectTransform>();
        foreach (var touch in Touchscreen.current.touches)
        {
            if (!touch.press.isPressed)
                continue;
            Vector2 pos = touch.position.ReadValue();
            if (RectTransformUtility.RectangleContainsScreenPoint(rt, pos))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Allows external code to directly drive the stick (optional).
    /// </summary>
    public void SetValue(Vector2 value)
    {
        sendValueVector2?.Invoke(this, new object[] { value });
    }

    /// <summary>
    /// Updates the handle’s on-screen position when tilt input is active.
    /// </summary>
    private void UpdateVisualHandle(Vector2 clampedValue)
    {
        if (handle == null)
            return;

        // OnScreenStick exposes a protected 'movementRange' field.
        // We'll get it via reflection for safety.
        var rangeField = typeof(OnScreenStick).GetField(
            "movementRange",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        float range = rangeField != null ? (float)rangeField.GetValue(this) : 100f;

        // Move handle relative to stick center
        handle.anchoredPosition = clampedValue * range;
    }

    /// <summary>
    /// Re-calibrates the neutral tilt position.
    /// </summary>
    public void CalibrateNeutral()
    {
#if UNITY_IOS && !UNITY_EDITOR
        calibrationOffset =
            Accelerometer.current != null
                ? Accelerometer.current.acceleration.ReadValue()
                : Input.acceleration;
        Debug.Log($"[PublicOnScreenStick] Calibrated neutral tilt: {calibrationOffset}");
#endif
    }
}

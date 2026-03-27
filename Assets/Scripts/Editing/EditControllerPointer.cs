using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

public class EditControllerPointer : MonoBehaviour
{
    [SerializeField]
    private float cursorSpeed = 900f;

    [SerializeField]
    private float stickDeadzone = 0.2f;

    [SerializeField]
    private float cursorSize = 10f;

    [SerializeField]
    private float cursorThickness = 2f;

    [SerializeField]
    private float outlineThickness = 6f;

    private EditGM _gmRef;
    private Texture2D _cursorTexture;

    private void Start()
    {
        _gmRef = EditGM.instance;
        PointerSource.EnsureInstance();
    }

    private void OnDestroy()
    {
        if (_cursorTexture != null)
            Destroy(_cursorTexture);
    }

    private void Update()
    {
        if (_gmRef == null || PointerSource.Instance == null)
            return;

        Gamepad pad = Gamepad.current;
        if (pad == null)
            return;

        bool worldPrimaryPressed = pad.buttonSouth.wasPressedThisFrame;
        bool worldSavePressed = pad.startButton.wasPressedThisFrame;
        // Xbox uses a shoulder chord for Test because the Share button is not reliably exposed.
        bool shoulderChordPressed = WasShoulderChordPressed(pad);
        // PlayStation gets a dedicated Test shortcut via the touchpad press.
        bool worldTestPressed = shoulderChordPressed || WasPlayStationTouchpadPressed(pad);
        bool rotateLeftPressed = pad.leftShoulder.wasPressedThisFrame && !shoulderChordPressed;
        bool rotateRightPressed = pad.rightShoulder.wasPressedThisFrame && !shoulderChordPressed;
        bool toggleModePressed = pad.leftStickButton.wasPressedThisFrame;
        bool setAnchorPressed = pad.rightStickButton.wasPressedThisFrame;

        if (HandleUICancel(pad))
            return;

        if (_gmRef.IsControllerUICaptureActive())
        {
            if (worldPrimaryPressed)
                PointerSource.Instance.ConsumeVirtualPrimary();

            if (ShouldClaimModalSelection(pad, worldPrimaryPressed))
                _gmRef.EnsureControllerUISelection();

            return;
        }

        Vector2 stick = pad.rightStick.ReadValue();
        bool canClaimWorldInput =
            !_gmRef.paletteMode
            && !_gmRef.inputMode
            && (_gmRef.quitDialogPanel == null || !_gmRef.quitDialogPanel.activeInHierarchy);

        if (canClaimWorldInput && stick.sqrMagnitude > stickDeadzone * stickDeadzone)
        {
            PointerSource.Instance.MoveVirtual(stick * cursorSpeed * Time.unscaledDeltaTime);
            _gmRef.ClearUISelectionForWorldInput();
        }

        if (!_gmRef.IsControllerWorldInputAllowed())
            return;

        if (worldPrimaryPressed)
        {
            PointerSource.Instance.PressVirtualPrimary();
            if (_gmRef.TryClickHudAtPointer())
                return;
        }

        if (worldSavePressed)
            _gmRef.OpenSaveDialogForNavigation();

        if (worldTestPressed)
            _gmRef.TestLevel();

        if (toggleModePressed)
        {
            _gmRef.HandleControllerToggleCreateEditMode();
        }

        if (setAnchorPressed)
        {
            PointerSource.Instance.PressVirtualSecondary();
        }

        if (rotateLeftPressed)
        {
            _gmRef.HandleControllerRotateTile(true);
        }

        if (rotateRightPressed)
        {
            _gmRef.HandleControllerRotateTile(false);
        }
    }

    private void OnGUI()
    {
        if (
            _gmRef == null
            || PointerSource.Instance == null
            || !PointerSource.Instance.IsVirtualActive
            || !_gmRef.IsControllerWorldInputAllowed()
        )
        {
            return;
        }

        EnsureCursorTexture();
        Vector2 screenPosition = PointerSource.Instance.ScreenPosition;
        float x = screenPosition.x;
        float y = Screen.height - screenPosition.y;

        GUI.color = new Color(0f, 0f, 0f, 0.95f);
        DrawCross(x, y, cursorSize + 2f, outlineThickness, _cursorTexture);

        GUI.color = new Color(1f, 1f, 1f, 0.95f);
        DrawCross(x, y, cursorSize, cursorThickness, _cursorTexture);
        GUI.color = Color.white;
    }

    private bool HandleUICancel(Gamepad pad)
    {
        if (!pad.buttonEast.wasPressedThisFrame)
            return false;

        if (!_gmRef.IsControllerUICaptureActive())
            return false;

        if (PointerSource.Instance != null)
            PointerSource.Instance.ConsumeVirtualPrimary();

        GameObject current =
            EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (current == null)
            return false;

        ExecuteEvents.Execute(
            current,
            new BaseEventData(EventSystem.current),
            ExecuteEvents.cancelHandler
        );
        return true;
    }

    private static bool WasPlayStationTouchpadPressed(Gamepad pad)
    {
        return pad is DualShockGamepad dualShock
            && dualShock.touchpadButton != null
            && dualShock.touchpadButton.wasPressedThisFrame;
    }

    private bool ShouldClaimModalSelection(Gamepad pad, bool primaryPressed)
    {
        if (pad == null || _gmRef == null || ModalAlreadyHasSelection())
            return false;

        return HasControllerModalIntent(pad, primaryPressed);
    }

    private bool ModalAlreadyHasSelection()
    {
        if (_gmRef == null || EventSystem.current == null)
            return false;

        GameObject modalRoot = _gmRef.GetPreferredControllerUIRoot();
        GameObject current = EventSystem.current.currentSelectedGameObject;
        return current != null
            && modalRoot != null
            && current.transform.IsChildOf(modalRoot.transform);
    }

    private bool HasControllerModalIntent(Gamepad pad, bool primaryPressed)
    {
        return primaryPressed
            || pad.buttonEast.wasPressedThisFrame
            || pad.startButton.wasPressedThisFrame
            || pad.selectButton.wasPressedThisFrame
            || pad.dpad.up.wasPressedThisFrame
            || pad.dpad.down.wasPressedThisFrame
            || pad.dpad.left.wasPressedThisFrame
            || pad.dpad.right.wasPressedThisFrame
            || pad.leftStick.ReadValue().sqrMagnitude > stickDeadzone * stickDeadzone;
    }

    private static bool WasShoulderChordPressed(Gamepad pad)
    {
        bool leftPressedThisFrame = pad.leftShoulder.wasPressedThisFrame;
        bool rightPressedThisFrame = pad.rightShoulder.wasPressedThisFrame;

        // Treat either shoulder as the "second half" of the chord if the other is already held.
        return (leftPressedThisFrame && pad.rightShoulder.isPressed)
            || (rightPressedThisFrame && pad.leftShoulder.isPressed);
    }

    private void EnsureCursorTexture()
    {
        if (_cursorTexture != null)
            return;

        _cursorTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        _cursorTexture.SetPixel(0, 0, Color.white);
        _cursorTexture.Apply();
    }

    private static void DrawCross(float x, float y, float size, float thickness, Texture2D texture)
    {
        GUI.DrawTexture(new Rect(x - thickness * 0.5f, y - size, thickness, size * 2f), texture);
        GUI.DrawTexture(new Rect(x - size, y - thickness * 0.5f, size * 2f, thickness), texture);
    }
}

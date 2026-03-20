using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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
        bool worldSecondaryPressed = pad.buttonNorth.wasPressedThisFrame;
        bool worldCancelPressed = pad.buttonEast.wasPressedThisFrame;
        bool worldDeletePressed = pad.buttonWest.wasPressedThisFrame;
        bool rotateLeftPressed = pad.leftShoulder.wasPressedThisFrame;
        bool rotateRightPressed = pad.rightShoulder.wasPressedThisFrame;

        if (HandleUiCancel(pad))
            return;

        if (_gmRef.IsControllerUiCaptureActive())
        {
            if (worldPrimaryPressed)
                PointerSource.Instance.ConsumeVirtualPrimary();
            if (worldSecondaryPressed)
                PointerSource.Instance.ConsumeVirtualSecondary();
            _gmRef.EnsureControllerUiSelection();
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

        if (worldSecondaryPressed)
        {
            PointerSource.Instance.PressVirtualSecondary();
        }

        if (worldCancelPressed)
        {
            _gmRef.HandleControllerCancelWorld();
        }

        if (worldDeletePressed)
        {
            _gmRef.HandleControllerDeleteWorld();
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
        if (_gmRef == null
            || PointerSource.Instance == null
            || !PointerSource.Instance.IsVirtualActive
            || !_gmRef.IsControllerWorldInputAllowed())
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

    private bool HandleUiCancel(Gamepad pad)
    {
        if (!pad.buttonEast.wasPressedThisFrame)
            return false;

        if (!_gmRef.IsControllerUiCaptureActive())
            return false;

        if (PointerSource.Instance != null)
            PointerSource.Instance.ConsumeVirtualPrimary();

        GameObject current = EventSystem.current != null
            ? EventSystem.current.currentSelectedGameObject
            : null;
        if (current == null)
            return false;

        ExecuteEvents.Execute(
            current,
            new BaseEventData(EventSystem.current),
            ExecuteEvents.cancelHandler
        );
        return true;
    }

    private void EnsureCursorTexture()
    {
        if (_cursorTexture != null)
            return;

        _cursorTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        _cursorTexture.SetPixel(0, 0, Color.white);
        _cursorTexture.Apply();
    }

    private static void DrawCross(
        float x,
        float y,
        float size,
        float thickness,
        Texture2D texture
    )
    {
        GUI.DrawTexture(
            new Rect(x - thickness * 0.5f, y - size, thickness, size * 2f),
            texture
        );
        GUI.DrawTexture(
            new Rect(x - size, y - thickness * 0.5f, size * 2f, thickness),
            texture
        );
    }
}

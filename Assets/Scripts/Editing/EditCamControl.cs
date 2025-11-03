using UnityEngine;
using UnityEngine.InputSystem;

public class EditCamControl : MonoBehaviour
{
    [Header("Camera Settings")]
    public float dragSpeed = 500f;
    public float zoomSpeed = 100f;
    public float minZoomAmount = -25f;
    public float maxZoomAmount = 5f;

    // private variables
    private EditGM _gmRef;
    private Vector2 _moveInput;
    private Vector2 _dragOrigin;
    private float _zoomAmount = 0f;

    // input references
    private InputControls _controls;

    void Start()
    {
        _gmRef = EditGM.instance;
        _controls = InputManager.Instance.Controls;

        // enable Editing map
        var edit = _controls.Edit;

        // WASD / Arrow movement
        edit.MoveCamera.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        edit.MoveCamera.canceled += _ => _moveInput = Vector2.zero;

        // Scroll wheel zoom
        edit.ZoomCamera.performed += ctx =>
        {
            float scroll = ctx.ReadValue<float>();
            _zoomAmount = Mathf.Clamp(
                _zoomAmount + scroll * zoomSpeed * Time.deltaTime,
                minZoomAmount,
                maxZoomAmount
            );
        };

        // initial position: back from active layer
        Vector3 v3 = transform.position;
        v3.z = _gmRef.GetLayerDepth() - 8f;
        transform.position = v3;
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null)
            return;

        // --- Keyboard camera movement ---
        if (!_gmRef.inputMode && _moveInput != Vector2.zero)
        {
            Vector3 v3 = transform.position;
            v3.x += _moveInput.x * 5f * Time.deltaTime;
            v3.y += _moveInput.y * 5f * Time.deltaTime;
            transform.position = v3;
        }

        // --- Middle mouse drag (pan) ---
        if (mouse.middleButton.wasPressedThisFrame)
            _dragOrigin = mouse.position.ReadValue();

        if (mouse.middleButton.isPressed)
        {
            Vector2 currentPos = mouse.position.ReadValue();
            Vector2 delta = currentPos - _dragOrigin;

            Camera cam = Camera.main;
            float camHeight = cam.orthographicSize * 2f;
            float camWidth = camHeight * cam.aspect;

            Vector3 move = new Vector3(
                -delta.x / Screen.width * camWidth,
                -delta.y / Screen.height * camHeight,
                0f
            );

            cam.transform.Translate(move * dragSpeed * Time.deltaTime, Space.World);
            _dragOrigin = currentPos;
        }

        // --- Hardware scroll wheel (for fallback / legacy mice) ---
        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            _zoomAmount = Mathf.Clamp(
                _zoomAmount + scroll * zoomSpeed * Time.deltaTime,
                minZoomAmount,
                maxZoomAmount
            );
        }

        // --- Apply zoom ---
        Vector3 pos = transform.position;
        pos.z = _gmRef.GetLayerDepth() - 8f + _zoomAmount;
        transform.position = pos;
    }
}

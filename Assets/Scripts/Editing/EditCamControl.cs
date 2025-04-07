using UnityEngine;
using InputKeys = EditGM.InputKeys;

public class EditCamControl : MonoBehaviour
{
    public float dragSpeed = 500f;
    public float zoomSpeed = 10f;
    public float minZoomAmount = -25f;
    public float maxZoomAmount = 5f;

    // private variables
    private InputKeys _camInputs;
    private InputKeys _keyMask;
    private EditGM _gmRef;
    private Vector3 dragOrigin;
    private float zoomAmount = 0;

    void Start()
    {
        _gmRef = EditGM.instance;
        _keyMask = (InputKeys.Up | InputKeys.Left | InputKeys.Down | InputKeys.Right);

        // move our camera back
        Vector3 v3 = transform.position;
        //  get active layer depth and set temp position back 8 units from it
        v3.z = _gmRef.GetLayerDepth() - 8f;
        transform.position = v3;
    }

    void Update()
    {
        // mask identifying the keys relevant to the camera control (WASD)
        _camInputs = _gmRef.getInputs;
        _camInputs &= _keyMask;

        // WASD movement - (when not in input mode from level name input)
        if (!_gmRef.inputMode)
        {
            Vector3 v3 = transform.position;
            if ((_camInputs & InputKeys.Up) == InputKeys.Up)
                v3.y += (5.0f * Time.deltaTime);
            if ((_camInputs & InputKeys.Left) == InputKeys.Left)
                v3.x -= (5.0f * Time.deltaTime);
            if ((_camInputs & InputKeys.Down) == InputKeys.Down)
                v3.y -= (5.0f * Time.deltaTime);
            if ((_camInputs & InputKeys.Right) == InputKeys.Right)
                v3.x += (5.0f * Time.deltaTime);

            transform.position = v3;
        }

        // Optional - uncomment to block scroll wheel behaviors when over UI elements
        // if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        // {
        //     Debug.Log("Ignoring Wheel Click inputs - pointer is over UI");
        //     return;
        // }

        // Wheel mouse button pressed: capture origin
        if (Input.GetMouseButtonDown(2))
        {
            dragOrigin = Input.mousePosition;
        }

        // Wheel mouse button held: calculate and move
        if (Input.GetMouseButton(2))
        {
            Vector3 currentPos = Input.mousePosition;
            Vector3 delta = currentPos - dragOrigin;

            Camera cam = Camera.main;
            float camHeight = cam.orthographicSize * 2f;
            float camWidth = camHeight * cam.aspect;

            // Convert pixel delta to world delta
            Vector3 move = new Vector3(
                -delta.x / Screen.width * camWidth,
                -delta.y / Screen.height * camHeight,
                0f
            );

            cam.transform.Translate(move * dragSpeed * Time.deltaTime, Space.World);
            dragOrigin = currentPos;
        }

        // Wheel Scroll - Zoom Zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            zoomAmount = Mathf.Clamp(zoomAmount + scroll * zoomSpeed, minZoomAmount, maxZoomAmount);
        }

        // Set the camera back and use the zoom amount
        Vector3 finalCameraPosition = transform.position;
        //  get active layer depth and set temp position back 8 units from it
        finalCameraPosition.z = _gmRef.GetLayerDepth() - 8f + zoomAmount;
        transform.position = finalCameraPosition;
    }
}

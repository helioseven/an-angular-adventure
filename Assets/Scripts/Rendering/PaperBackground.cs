using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class PaperBackground : MonoBehaviour
{
    private enum PlacementMode
    {
        CameraBackground,
        WorldSpace,
    }

    [SerializeField]
    private PlacementMode _placement = PlacementMode.WorldSpace;

    [SerializeField]
    private Shader _shader;

    [SerializeField]
    private Material _material;

    [SerializeField, Min(0.1f)]
    private float _distanceFromCamera = 50f;

    [SerializeField]
    private bool _autoSize = true;

    [SerializeField]
    private Vector2 _worldSize = new Vector2(50f, 50f);

    [SerializeField]
    private Vector3 _worldPosition = Vector3.zero;

    [SerializeField]
    private Vector3 _worldEuler = Vector3.zero;

    [SerializeField]
    private bool _followActiveLayer = true;

    [SerializeField]
    private float _layerDepthOffset = 0f;

    [SerializeField]
    private bool _fitToBoundaries = true;

    [SerializeField]
    private float _boundaryPadding = 0f;

    private GameObject _quad;
    private MeshRenderer _renderer;
    private MeshFilter _filter;

    private void OnEnable()
    {
        EnsureSetup();
        UpdatePlacement();
    }

    private void OnDisable()
    {
        Cleanup();
    }

    private void OnValidate()
    {
        if (!enabled)
        {
            return;
        }

        if (_quad != null)
        {
            if (_placement == PlacementMode.CameraBackground)
            {
                UpdateCameraQuad();
            }
            else
            {
                UpdateWorldQuad();
            }
        }
    }

    private void Update()
    {
        UpdatePlacement();
    }

    private void UpdatePlacement()
    {
        UpdateParent();

        if (_placement == PlacementMode.CameraBackground)
        {
            UpdateCameraQuad();
            return;
        }

        UpdateWorldQuad();
    }

    private void EnsureSetup()
    {
        if (_shader == null)
        {
            _shader = Shader.Find("Unlit/PaperBackground");
        }

        if (_material == null && _shader != null)
        {
            _material = new Material(_shader) { hideFlags = HideFlags.DontSave };
        }

        if (_quad == null)
        {
            _quad = new GameObject("PaperBackgroundQuad");
            _quad.hideFlags = HideFlags.DontSave;
            _filter = _quad.AddComponent<MeshFilter>();
            _renderer = _quad.AddComponent<MeshRenderer>();
            _filter.sharedMesh = BuildQuadMesh();
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
        }

        if (_renderer != null && _material != null)
        {
            _renderer.sharedMaterial = _material;
        }
    }

    private void UpdateParent()
    {
        if (_quad == null)
        {
            return;
        }

        Transform desiredParent = _placement == PlacementMode.CameraBackground ? transform : null;
        if (_quad.transform.parent != desiredParent)
        {
            _quad.transform.SetParent(desiredParent, false);
        }
    }

    private void UpdateCameraQuad()
    {
        if (_quad == null)
        {
            return;
        }

        Camera cam = GetComponent<Camera>();
        if (cam == null)
        {
            return;
        }

        float distance = _autoSize ? cam.farClipPlane * 0.95f : _distanceFromCamera;
        distance = Mathf.Clamp(distance, cam.nearClipPlane + 0.01f, cam.farClipPlane - 0.01f);

        float height;
        float width;
        if (cam.orthographic)
        {
            height = cam.orthographicSize * 2f;
            width = height * cam.aspect;
        }
        else
        {
            height = 2f * distance * Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f);
            width = height * cam.aspect;
        }

        _quad.transform.localPosition = new Vector3(0f, 0f, distance);
        _quad.transform.localRotation = Quaternion.identity;
        _quad.transform.localScale = new Vector3(width, height, 1f);
    }

    private void UpdateWorldQuad()
    {
        if (_quad == null)
        {
            return;
        }

        Vector3 position = _worldPosition;
        Vector2 size = _worldSize;
        if (_fitToBoundaries && TryGetBoundaryRect(out Vector2 min, out Vector2 max))
        {
            size = (max - min) + Vector2.one * (_boundaryPadding * 2f);
            Vector2 center = (min + max) * 0.5f;
            position.x = center.x;
            position.y = center.y;
        }

        if (_followActiveLayer && TryGetActiveLayerDepth(out float depth))
        {
            position.z = depth + _layerDepthOffset;
        }

        _quad.transform.position = position;
        _quad.transform.rotation = Quaternion.Euler(_worldEuler);
        _quad.transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    private bool TryGetActiveLayerDepth(out float depth)
    {
        if (PlayGM.instance == null || PlayGM.instance.tileMap == null)
        {
            depth = 0f;
            return false;
        }

        Transform tileMap = PlayGM.instance.tileMap.transform;
        int layerIndex = PlayGM.instance.activeLayer;
        if (layerIndex < 0 || layerIndex >= tileMap.childCount)
        {
            depth = 0f;
            return false;
        }

        depth = tileMap.GetChild(layerIndex).position.z;
        return true;
    }

    private bool TryGetBoundaryRect(out Vector2 min, out Vector2 max)
    {
        min = Vector2.zero;
        max = Vector2.zero;

        if (PlayGM.instance == null)
        {
            return false;
        }

        Boundary left = PlayGM.instance.boundaryLeft;
        Boundary right = PlayGM.instance.boundaryRight;
        Boundary up = PlayGM.instance.boundaryUp;
        Boundary down = PlayGM.instance.boundaryDown;

        if (left == null || right == null || up == null || down == null)
        {
            return false;
        }

        min = new Vector2(left.transform.position.x, down.transform.position.y);
        max = new Vector2(right.transform.position.x, up.transform.position.y);
        return true;
    }

    private Mesh BuildQuadMesh()
    {
        Mesh mesh = new Mesh { name = "PaperBackgroundQuad" };

        Vector3[] verts =
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f),
        };

        Vector2[] uvs =
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
        };

        int[] tris = { 0, 2, 1, 0, 3, 2 };

        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void Cleanup()
    {
        if (_quad != null)
        {
            DestroyImmediate(_quad);
            _quad = null;
        }

        if (_material != null && _material.hideFlags == HideFlags.DontSave)
        {
            DestroyImmediate(_material);
            _material = null;
        }
    }
}

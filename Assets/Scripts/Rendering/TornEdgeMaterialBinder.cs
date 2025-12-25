using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TornEdgeMaterialBinder : MonoBehaviour
{
    [SerializeField] private Boundary _boundary;
    [SerializeField] private bool _followActiveLayer = true;
    [SerializeField] private float _layerDepthOffset = -0.5f;
    [SerializeField] private Color _baseColor = new Color(0.95f, 0.92f, 0.86f, 1f);
    [SerializeField, Range(0f, 1f)] private float _alpha = 1f;
    [SerializeField, Range(0f, 0.5f)] private float _edgeWidth = 0.08f;
    [SerializeField, Range(-0.5f, 0.5f)] private float _edgeOffset = 0.05f;
    [SerializeField] private float _noiseScale = 1f;
    [SerializeField, Min(0.1f)] private float _noiseTileWorldSize = 1f;
    [SerializeField, Range(0f, 0.5f)] private float _noiseStrength = 0.08f;
    [SerializeField, Range(0f, 0.2f)] private float _feather = 0.02f;
    [SerializeField, Range(0f, 0.2f)] private float _grainStrength = 0.05f;
    [SerializeField, Range(0f, 0.4f)] private float _burnWidth = 0.32f;
    [SerializeField, Range(0f, 2f)] private float _burnStrength = 1.6f;
    [SerializeField, Range(0f, 1f)] private float _burnGlow = 0.08f;
    [SerializeField] private int _edgeDirectionOverride = -1;

    private SpriteRenderer _renderer;
    private MaterialPropertyBlock _mpb;
    private static Sprite _fallbackSprite;

    private void OnEnable()
    {
        if (_renderer == null)
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        DisableMeshRenderer();
        EnsureBoundary();
        EnsureSprite();
        Apply();
    }

    private void OnValidate()
    {
        if (!enabled)
        {
            return;
        }

        if (_renderer == null)
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        DisableMeshRenderer();
        EnsureBoundary();
        Apply();
    }

    private void Update()
    {
        if (_followActiveLayer && TryGetActiveLayerDepth(out float depth))
        {
            Vector3 position = transform.position;
            position.z = depth + _layerDepthOffset;
            transform.position = position;
        }
    }

    private void Apply()
    {
        if (_renderer == null)
        {
            return;
        }

        if (_mpb == null)
        {
            _mpb = new MaterialPropertyBlock();
        }

        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor("_BaseColor", _baseColor);
        _mpb.SetFloat("_Alpha", _alpha);
        _mpb.SetFloat("_EdgeWidth", _edgeWidth);
        _mpb.SetFloat("_EdgeOffset", _edgeOffset);
        _mpb.SetFloat("_NoiseScale", _noiseScale);
        _mpb.SetVector("_NoiseTiling", GetNoiseTiling());
        _mpb.SetFloat("_NoiseStrength", _noiseStrength);
        _mpb.SetFloat("_Feather", _feather);
        _mpb.SetFloat("_GrainStrength", _grainStrength);
        _mpb.SetFloat("_BurnWidth", _burnWidth);
        _mpb.SetFloat("_BurnStrength", _burnStrength);
        _mpb.SetFloat("_BurnGlow", _burnGlow);
        _mpb.SetFloat("_EdgeDir", GetEdgeDirection());
        _renderer.SetPropertyBlock(_mpb);
    }

    private Vector2 GetNoiseTiling()
    {
        if (_renderer == null)
        {
            return Vector2.one;
        }

        Bounds bounds = _renderer.bounds;
        float width = bounds.size.x;
        float height = bounds.size.y;
        float tileSize = Mathf.Max(_noiseTileWorldSize, 0.001f);
        int edgeDir = GetEdgeDirection();

        if (edgeDir == 0 || edgeDir == 1)
        {
            float tileCount = Mathf.Max(1f, width / tileSize);
            return new Vector2(tileCount, 1f);
        }

        float verticalCount = Mathf.Max(1f, height / tileSize);
        return new Vector2(1f, verticalCount);
    }

    private void EnsureSprite()
    {
        if (_renderer == null || _renderer.sprite != null)
        {
            return;
        }

        if (_fallbackSprite == null)
        {
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            tex.hideFlags = HideFlags.DontSave;
            _fallbackSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            _fallbackSprite.hideFlags = HideFlags.DontSave;
        }

        _renderer.sprite = _fallbackSprite;
    }

    private void DisableMeshRenderer()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.enabled)
        {
            meshRenderer.enabled = false;
        }
    }

    private void EnsureBoundary()
    {
        if (_boundary == null)
        {
            _boundary = GetComponent<Boundary>();
        }
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

    private int GetEdgeDirection()
    {
        if (_edgeDirectionOverride >= 0 && _edgeDirectionOverride <= 3)
        {
            return _edgeDirectionOverride;
        }

        Boundary boundary = _boundary != null ? _boundary : GetComponent<Boundary>();
        if (boundary == null)
        {
            return 0;
        }

        if (boundary.isVertical)
        {
            return boundary.isPositive ? 0 : 1; // top / bottom
        }

        return boundary.isPositive ? 3 : 2; // right / left
    }
}

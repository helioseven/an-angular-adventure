using System.Collections.Generic;
using UnityEngine;

public class MenuUIPhysicsProxy : MonoBehaviour
{
    [Header("Source UI")]
    public Transform ignoreRoot;
    public List<Transform> ignoreRoots = new List<Transform>();

    [Header("World")]
    public Camera worldCamera;
    public float physicsPlaneZ = 0f;
    public Transform proxyRoot;
    public int proxyLayer = 5;
    public bool disableSourceColliders = true;
    public bool startActive = true;
    public bool keepSourceCollidersDisabled = false;

    private readonly List<Collider2D> _disabledSources = new List<Collider2D>();
    private readonly Dictionary<string, ProxyGroup> _groups = new Dictionary<string, ProxyGroup>();
    private bool _physicsActive = true;

    private enum ProxyType
    {
        Polygon,
        Box,
        Circle,
    }

    private class ProxyEntry
    {
        public Collider2D source;
        public Collider2D proxy;
        public ProxyType type;
    }

    private class ProxyGroup
    {
        public string name;
        public RectTransform root;
        public RectTransform visibilityRect;
        public Transform container;
        public readonly List<ProxyEntry> proxies = new List<ProxyEntry>();
        public bool active;
        public bool built;
    }

    void Awake()
    {
        _physicsActive = startActive;
        if (!worldCamera)
        {
            worldCamera = Camera.main;
        }

        EnsureProxyRoot();

        proxyRoot.gameObject.SetActive(_physicsActive);
    }

    void LateUpdate()
    {
        if (!_physicsActive)
        {
            return;
        }

        UpdateProxies();
    }

    public void RegisterGroup(string name, RectTransform root)
    {
        if (string.IsNullOrEmpty(name) || root == null)
        {
            return;
        }

        EnsureProxyRoot();

        if (!_groups.TryGetValue(name, out ProxyGroup group))
        {
            group = new ProxyGroup { name = name };
            _groups.Add(name, group);
        }

        group.root = root;
        if (group.container == null)
        {
            GameObject container = new GameObject($"{name}Proxies");
            container.transform.SetParent(proxyRoot, false);
            group.container = container.transform;
            group.container.gameObject.SetActive(false);
        }

        if (_physicsActive)
        {
            BuildGroup(group);
        }
    }

    public void SetGroupVisibilityRect(string name, RectTransform visibilityRect)
    {
        if (!_groups.TryGetValue(name, out ProxyGroup group))
        {
            return;
        }

        group.visibilityRect = visibilityRect;
    }

    private void EnsureProxyRoot()
    {
        if (proxyRoot)
        {
            return;
        }

        GameObject existing = GameObject.Find("MenuUIPhysicsProxyRoot");
        if (existing)
        {
            proxyRoot = existing.transform;
        }
        else
        {
            GameObject root = new GameObject("MenuUIPhysicsProxyRoot");
            root.transform.SetParent(null, false);
            root.transform.position = new Vector3(0f, 0f, physicsPlaneZ);
            proxyRoot = root.transform;
        }

        foreach (ProxyGroup group in _groups.Values)
        {
            if (group.container != null && group.container.parent != proxyRoot)
            {
                group.container.SetParent(proxyRoot, false);
            }
        }
    }

    public Transform GetGroupContainer(string name)
    {
        if (_groups.TryGetValue(name, out ProxyGroup group))
        {
            return group.container;
        }

        return null;
    }

    public void SetGroupActive(string name, bool active)
    {
        if (!_groups.TryGetValue(name, out ProxyGroup group))
        {
            return;
        }

        group.active = active;
        if (group.container != null)
        {
            group.container.gameObject.SetActive(active && _physicsActive);
        }

        if (active && _physicsActive)
        {
            ClearGroup(group);
            BuildGroup(group);
        }
    }

    public void RebuildGroup(string name)
    {
        if (!_groups.TryGetValue(name, out ProxyGroup group))
        {
            return;
        }

        ClearGroup(group);

        if (_physicsActive && group.active)
        {
            BuildGroup(group);
        }
    }

    public void SetPhysicsActive(bool active)
    {
        if (_physicsActive == active)
        {
            return;
        }

        _physicsActive = active;

        if (_physicsActive)
        {
            if (proxyRoot)
            {
                proxyRoot.gameObject.SetActive(true);
            }

            foreach (ProxyGroup group in _groups.Values)
            {
                if (group.container != null)
                {
                    group.container.gameObject.SetActive(group.active);
                }

                if (group.active && !group.built)
                {
                    BuildGroup(group);
                }
            }
        }
        else
        {
            if (proxyRoot)
            {
                proxyRoot.gameObject.SetActive(false);
            }
        }
    }

    private void ClearGroup(ProxyGroup group)
    {
        if (group == null)
        {
            return;
        }

        if (group.container != null)
        {
            for (int i = group.container.childCount - 1; i >= 0; i--)
            {
                Transform child = group.container.GetChild(i);
                Destroy(child.gameObject);
            }
        }

        for (int i = group.proxies.Count - 1; i >= 0; i--)
        {
            ProxyEntry entry = group.proxies[i];
            if (entry?.source != null && disableSourceColliders && !keepSourceCollidersDisabled)
            {
                entry.source.enabled = true;
                _disabledSources.Remove(entry.source);
            }

            if (entry?.proxy != null)
            {
                Destroy(entry.proxy.gameObject);
            }
        }

        group.proxies.Clear();
        group.built = false;
    }

    private void BuildGroup(ProxyGroup group)
    {
        if (group == null || group.root == null)
        {
            return;
        }

        group.proxies.Clear();
        Collider2D[] colliders = group.root.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D source = colliders[i];
            if (!source)
            {
                continue;
            }

            if (!source.enabled)
            {
                if (!disableSourceColliders || !_disabledSources.Contains(source))
                {
                    continue;
                }
            }

            if (!source.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (IsIgnored(source.transform))
            {
                continue;
            }

            ProxyType type;
            if (source is PolygonCollider2D)
            {
                type = ProxyType.Polygon;
            }
            else if (source is BoxCollider2D)
            {
                type = ProxyType.Box;
            }
            else if (source is CircleCollider2D)
            {
                type = ProxyType.Circle;
            }
            else
            {
                continue;
            }

            GameObject proxyGo = new GameObject($"Proxy_{source.gameObject.name}");
            proxyGo.layer = proxyLayer;
            proxyGo.transform.SetParent(group.container, false);
            proxyGo.transform.position = new Vector3(0f, 0f, physicsPlaneZ);

            Rigidbody2D body = proxyGo.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            body.simulated = true;

            Collider2D proxy;
            switch (type)
            {
                case ProxyType.Box:
                    proxy = proxyGo.AddComponent<BoxCollider2D>();
                    break;
                case ProxyType.Circle:
                    proxy = proxyGo.AddComponent<CircleCollider2D>();
                    break;
                default:
                    proxy = proxyGo.AddComponent<PolygonCollider2D>();
                    break;
            }

            proxy.isTrigger = source.isTrigger;
            proxy.sharedMaterial = source.sharedMaterial;

            RectTransform rect = source.transform as RectTransform;
            if (group.visibilityRect != null && rect != null)
            {
                if (!DoScreenRectsOverlap(rect, group.visibilityRect))
                {
                    Destroy(proxyGo);
                    continue;
                }
            }

            group.proxies.Add(
                new ProxyEntry
                {
                    source = source,
                    proxy = proxy,
                    type = type,
                }
            );

            if (disableSourceColliders)
            {
                source.enabled = false;
                _disabledSources.Add(source);
            }
        }

        group.built = true;
    }

    private void UpdateProxies()
    {
        if (!worldCamera || _groups.Count == 0)
        {
            return;
        }

        float zDistance = physicsPlaneZ - worldCamera.transform.position.z;

        foreach (ProxyGroup group in _groups.Values)
        {
            if (!group.active || group.proxies.Count == 0)
            {
                continue;
            }

            for (int i = group.proxies.Count - 1; i >= 0; i--)
            {
                ProxyEntry entry = group.proxies[i];
                if (entry == null || entry.source == null || entry.proxy == null)
                {
                    group.proxies.RemoveAt(i);
                    continue;
                }

                if (!entry.source.gameObject.activeInHierarchy)
                {
                    entry.proxy.enabled = false;
                    continue;
                }
                if (!entry.proxy.enabled)
                {
                    entry.proxy.enabled = true;
                }

                RectTransform rect = entry.source.transform as RectTransform;
                if (rect == null)
                {
                    continue;
                }

                switch (entry.type)
                {
                    case ProxyType.Polygon:
                        UpdatePolygonProxy(
                            (PolygonCollider2D)entry.source,
                            (PolygonCollider2D)entry.proxy,
                            rect,
                            zDistance
                        );
                        break;
                    case ProxyType.Box:
                        UpdateBoxProxy(
                            (BoxCollider2D)entry.source,
                            (BoxCollider2D)entry.proxy,
                            rect,
                            zDistance
                        );
                        break;
                    case ProxyType.Circle:
                        UpdateCircleProxy(
                            (CircleCollider2D)entry.source,
                            (CircleCollider2D)entry.proxy,
                            rect,
                            zDistance
                        );
                        break;
                }
            }
        }
    }

    private bool IsIgnored(Transform target)
    {
        if (ignoreRoot && target.IsChildOf(ignoreRoot))
        {
            return true;
        }

        if (ignoreRoots != null)
        {
            for (int i = 0; i < ignoreRoots.Count; i++)
            {
                Transform root = ignoreRoots[i];
                if (root && target.IsChildOf(root))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void UpdatePolygonProxy(
        PolygonCollider2D source,
        PolygonCollider2D proxy,
        RectTransform rect,
        float zDistance
    )
    {
        Vector3 worldCenter = rect.TransformPoint(Vector3.zero);
        proxy.transform.position = worldCenter;
        proxy.transform.rotation = rect.rotation;

        int pathCount = source.pathCount;
        proxy.pathCount = pathCount;

        for (int p = 0; p < pathCount; p++)
        {
            Vector2[] srcPath = source.GetPath(p);
            Vector2[] proxyPath = new Vector2[srcPath.Length];

            for (int j = 0; j < srcPath.Length; j++)
            {
                Vector3 uiWorld = rect.TransformPoint(srcPath[j]);
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, uiWorld);
                Vector3 world = worldCamera.ScreenToWorldPoint(
                    new Vector3(screen.x, screen.y, zDistance)
                );
                Vector3 local = proxy.transform.InverseTransformPoint(world);
                proxyPath[j] = new Vector2(local.x, local.y);
            }

            proxy.SetPath(p, proxyPath);
        }
    }

    private void UpdateBoxProxy(
        BoxCollider2D source,
        BoxCollider2D proxy,
        RectTransform rect,
        float zDistance
    )
    {
        Vector3 centerWorld = rect.TransformPoint(source.offset);
        Vector3 worldCenter = ScreenToWorld(centerWorld, zDistance);

        Vector3 rightWorld = rect.TransformVector(Vector3.right);
        Vector3 upWorld = rect.TransformVector(Vector3.up);
        Vector3 worldRight = ScreenToWorld(centerWorld + rightWorld, zDistance) - worldCenter;
        Vector3 worldUp = ScreenToWorld(centerWorld + upWorld, zDistance) - worldCenter;

        float worldUnitsPerUiX = worldRight.magnitude;
        float worldUnitsPerUiY = worldUp.magnitude;

        Vector2 size = source.size;
        float width = Mathf.Abs(size.x * worldUnitsPerUiX);
        float height = Mathf.Abs(size.y * worldUnitsPerUiY);

        proxy.transform.position = worldCenter;
        proxy.transform.rotation = rect.rotation;
        proxy.offset = Vector2.zero;
        proxy.size = new Vector2(width, height);
    }

    private void UpdateCircleProxy(
        CircleCollider2D source,
        CircleCollider2D proxy,
        RectTransform rect,
        float zDistance
    )
    {
        Vector3 centerWorld = rect.TransformPoint(source.offset);
        Vector3 worldCenter = ScreenToWorld(centerWorld, zDistance);

        Vector3 edgeWorld = rect.TransformPoint(source.offset + Vector2.right * source.radius);
        Vector3 worldEdge = ScreenToWorld(edgeWorld, zDistance);

        proxy.transform.position = worldCenter;
        proxy.offset = Vector2.zero;
        proxy.radius = Vector2.Distance(
            new Vector2(worldCenter.x, worldCenter.y),
            new Vector2(worldEdge.x, worldEdge.y)
        );
    }

    private bool DoScreenRectsOverlap(RectTransform a, RectTransform b)
    {
        if (a == null || b == null)
        {
            return false;
        }

        Rect rectA = GetScreenRect(a);
        Rect rectB = GetScreenRect(b);

        return rectA.Overlaps(rectB);
    }

    private Rect GetScreenRect(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, corners[i]);
            if (screen.x < minX)
                minX = screen.x;
            if (screen.x > maxX)
                maxX = screen.x;
            if (screen.y < minY)
                minY = screen.y;
            if (screen.y > maxY)
                maxY = screen.y;
        }

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private Vector3 ScreenToWorld(Vector3 uiWorld, float zDistance)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, uiWorld);
        Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, zDistance));
        world.z = physicsPlaneZ;
        return world;
    }
}

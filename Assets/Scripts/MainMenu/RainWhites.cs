using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RainWhites : MonoBehaviour
{
    [Header("Spawn")]
    public RectTransform spawnArea;
    public GameObject tileTemplate;
    public float minSpawnInterval = 0.1f;
    public float maxSpawnInterval = 0.35f;
    public float spawnBuffer = 50f;
    public int maxAlive = 40;
    public float maxRandomZRotation = 0f;
    public float fallSpeedVariance = 0f;
    public bool spawnFromScreenTop = true;
    public bool forceSpawnXCenter = false;

    [Header("Motion")]
    public float fallSpeed = 200f;
    public float swayDistance = 30f;
    public float swayFrequency = 0.4f;
    public float velocitySmoothing = 10f;
    public float despawnBuffer = 150f;

    [Header("World Physics")]
    public bool useWorldSpacePhysics = true;
    public Camera worldCamera;
    public float physicsPlaneZ = 0f;
    public Transform physicsRoot;
    public bool bakeColliderRotation = false;
    public bool lockVisualRotation = false;
    public bool lockPhysicsRotation = false;
    public bool useCanvasScaleForPhysics = false;

    [Header("Click Burst")]
    public int burstShardCount = 6;
    public float burstDuration = 0.35f;
    public float burstDistance = 180f;
    public float burstSpreadAngle = 50f;
    public float burstSizeScale = 0.15f;
    public float burstKickSpeed = 520f;
    public float burstKickImpulse = 14f;
    public float burstKickTorque = 12f;
    public LayerMask clickLayers = ~0;

    private readonly List<RainTile> _alive = new List<RainTile>();
    private Coroutine _spawnRoutine;
    private Canvas _canvas;
    private RectTransform _rootRect;
    private float _physicsScale = 1f;
    private bool _didInitialSpawnDelay;
    private float _lastScaleFactor = -1f;
    private int _lastScreenWidth = -1;
    private int _lastScreenHeight = -1;
    private float _pendingResizeTimer = -1f;
    public float resizeDebounceSeconds = 0.15f;

#if UNITY_EDITOR
    [Header("Debug")]
    public bool drawCenterGizmo = false;
    public Color centerGizmoColor = new Color(1f, 0.3f, 0.1f, 0.9f);
#endif

    [Header("Visual Center")]
    public bool assumeEquilateralTriangle = true;

    [Tooltip(
        "Applied as a fraction of rect height. -0.1666 means centroid for a point-up triangle."
    )]
    public float triangleCenterYOffset = -0.2f;

    private class RainTile
    {
        public Transform transform;
        public RectTransform rect;
        public Rigidbody2D body;
        public PolygonCollider2D collider;
        public Image image;
        public Quaternion lastRotation;
        public float fallSpeed;
        public float swayDistance;
        public float swayFrequency;
        public float swayPhase;
        public float spawnX;
    }

    void Awake()
    {
        if (!spawnArea)
        {
            spawnArea = GetComponent<RectTransform>();
        }

        _canvas = GetComponentInParent<Canvas>();

        if (!tileTemplate && transform.childCount > 0)
        {
            Transform named = transform.Find("White Triangle");
            tileTemplate = named ? named.gameObject : transform.GetChild(0).gameObject;
        }

        if (tileTemplate)
        {
            tileTemplate.SetActive(false);
        }

        if (!worldCamera)
        {
            worldCamera = Camera.main;
        }

        if (!physicsRoot && useWorldSpacePhysics)
        {
            GameObject root = new GameObject("MenuPhysicsBodies");
            root.transform.position = new Vector3(0f, 0f, physicsPlaneZ);
            physicsRoot = root.transform;
        }

        _rootRect = spawnArea ? spawnArea : transform as RectTransform;

        if (useWorldSpacePhysics && worldCamera)
        {
            CanvasScaler scaler = _canvas ? _canvas.GetComponentInParent<CanvasScaler>() : null;
            float scaleFactor = scaler ? scaler.scaleFactor : 1f;
            float worldUnitsPerScreenPixel =
                (2f * worldCamera.orthographicSize) / Mathf.Max(1f, Screen.height);
            _physicsScale = worldUnitsPerScreenPixel * scaleFactor;

            if (_canvas)
            {
                RectTransform canvasRect = _canvas.transform as RectTransform;
                if (canvasRect)
                {
                    _rootRect = canvasRect;
                }
            }
        }
    }

    void OnEnable()
    {
        _spawnRoutine = StartCoroutine(SpawnLoop());
    }

    void OnDisable()
    {
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }
    }

    void Update()
    {
        if (_alive.Count == 0)
        {
            return;
        }

        float bottomY = GetBottomY();
        float t = Time.time;

        for (int i = _alive.Count - 1; i >= 0; i--)
        {
            RainTile tile = _alive[i];
            if (!tile.transform)
            {
                if (tile.body)
                {
                    Destroy(tile.body.gameObject);
                }
                _alive.RemoveAt(i);
                continue;
            }

            if (tile.rect)
            {
                if (!tile.body || !useWorldSpacePhysics)
                {
                    Vector2 rectPos = tile.rect.anchoredPosition;
                    rectPos.y -= tile.fallSpeed * Time.deltaTime;
                    rectPos.x =
                        tile.spawnX
                        + Mathf.Sin(t * tile.swayFrequency + tile.swayPhase) * tile.swayDistance;
                    tile.rect.anchoredPosition = rectPos;
                }

                Vector2 rectPosCheck = tile.rect.anchoredPosition;
                if (rectPosCheck.y < bottomY - despawnBuffer)
                {
                    DestroyTile(tile);
                    _alive.RemoveAt(i);
                }
            }
            else
            {
                if (!tile.body || !useWorldSpacePhysics)
                {
                    Vector3 localPos = tile.transform.localPosition;
                    localPos.y -= tile.fallSpeed * Time.deltaTime;
                    localPos.x =
                        tile.spawnX
                        + Mathf.Sin(t * tile.swayFrequency + tile.swayPhase) * tile.swayDistance;
                    tile.transform.localPosition = localPos;
                }

                Vector3 localPosCheck = tile.transform.localPosition;
                if (localPosCheck.y < bottomY - despawnBuffer)
                {
                    DestroyTile(tile);
                    _alive.RemoveAt(i);
                }
            }
        }

        HandlePointerClick();
    }

    void FixedUpdate()
    {
        if (_alive.Count == 0)
        {
            return;
        }

        float t = Time.time;
        float smoothT = 1f - Mathf.Exp(-velocitySmoothing * Time.fixedDeltaTime);

        for (int i = _alive.Count - 1; i >= 0; i--)
        {
            RainTile tile = _alive[i];
            if (!tile.transform)
            {
                continue;
            }

            if (tile.body)
            {
                float phase = t * tile.swayFrequency + tile.swayPhase;
                float scale = useWorldSpacePhysics ? _physicsScale : 1f;
                float targetVx = Mathf.Cos(phase) * tile.swayDistance * tile.swayFrequency * scale;
                Vector2 target = new Vector2(targetVx, -tile.fallSpeed * scale);
                tile.body.linearVelocity = Vector2.Lerp(tile.body.linearVelocity, target, smoothT);
            }
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (!_didInitialSpawnDelay)
            {
                _didInitialSpawnDelay = true;
                yield return null;
            }

            if (_alive.Count < maxAlive)
            {
                SpawnOne();
            }

            float wait = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(wait);
        }
    }

    private void SpawnOne()
    {
        if (!tileTemplate)
        {
            return;
        }

        UpdatePhysicsScale();

        Transform parent = spawnArea ? spawnArea : transform;
        GameObject go = Instantiate(tileTemplate, parent);
        go.SetActive(true);

        float x = forceSpawnXCenter ? 0f : Random.Range(GetLeftX(), GetRightX());
        float y = GetTopY() + spawnBuffer;

        RectTransform rect = go.GetComponent<RectTransform>();
        Rigidbody2D body = go.GetComponent<Rigidbody2D>();
        PolygonCollider2D collider = go.GetComponent<PolygonCollider2D>();
        if (rect)
        {
            rect.anchoredPosition = new Vector2(x, y);
        }
        else
        {
            go.transform.localPosition = new Vector3(x, y, 0f);
        }

        float zRot = Random.Range(0f, maxRandomZRotation);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, zRot);

        if (body)
        {
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            float scale = useWorldSpacePhysics ? _physicsScale : 1f;
            body.linearVelocity = new Vector2(0f, -fallSpeed * scale);
        }

        float speed = fallSpeed * Random.Range(1f - fallSpeedVariance, 1f + fallSpeedVariance);
        float swayDist = swayDistance * Random.Range(0.7f, 1.3f);
        float swayFreq = swayFrequency * Random.Range(0.8f, 1.2f);
        float swayPhase = Random.Range(0f, Mathf.PI * 2f);

        if (useWorldSpacePhysics)
        {
            if (body)
            {
                body.simulated = false;
            }

            if (collider)
            {
                collider.enabled = false;
            }

            if (rect && _rootRect && rect.parent != _rootRect)
            {
                Vector3 world = rect.TransformPoint(rect.rect.center);
                Vector3 rootLocal = _rootRect.InverseTransformPoint(world);
                rect.SetParent(_rootRect, false);
                rect.anchoredPosition = new Vector2(rootLocal.x, rootLocal.y);
            }

            body = CreatePhysicsBody(
                go,
                rect,
                ignoreRectRotation: !bakeColliderRotation,
                out PolygonCollider2D physicsCollider
            );
            if (body)
            {
                if (!bakeColliderRotation)
                {
                    ApplyInitialRotation(body, rect, go.transform.rotation);
                }
            }
            if (physicsCollider)
            {
                collider = physicsCollider;
            }

            if (body && lockPhysicsRotation)
            {
                body.constraints |= RigidbodyConstraints2D.FreezeRotation;
            }
        }

        _alive.Add(
            new RainTile
            {
                transform = go.transform,
                rect = rect,
                body = body,
                collider = collider,
                image = go.GetComponent<Image>(),
                lastRotation = rect ? rect.rotation : Quaternion.identity,
                fallSpeed = speed,
                swayDistance = swayDist,
                swayFrequency = swayFreq,
                swayPhase = swayPhase,
                spawnX = x,
            }
        );
    }

    private float GetLeftX()
    {
        if (!spawnArea)
        {
            return -Screen.width * 0.5f;
        }

        return spawnArea.rect.xMin;
    }

    private float GetRightX()
    {
        if (!spawnArea)
        {
            return Screen.width * 0.5f;
        }

        return spawnArea.rect.xMax;
    }

    private float GetTopY()
    {
        if (spawnFromScreenTop && _rootRect != null)
        {
            return _rootRect.rect.yMax;
        }

        if (!spawnArea)
        {
            return Screen.height * 0.5f;
        }

        return spawnArea.rect.yMax;
    }

    private float GetBottomY()
    {
        if (!spawnArea)
        {
            return -Screen.height * 0.5f;
        }

        return spawnArea.rect.yMin;
    }

    private void UpdatePhysicsScale()
    {
        if (!useWorldSpacePhysics || !worldCamera)
        {
            _physicsScale = 1f;
            return;
        }

        if (_rootRect != null)
        {
            float zDistance = physicsPlaneZ - worldCamera.transform.position.z;
            Vector2 localA = _rootRect.rect.center;
            Vector2 localB = localA + Vector2.up;
            Vector3 worldA = UIToWorldPoint(_rootRect, localA, zDistance);
            Vector3 worldB = UIToWorldPoint(_rootRect, localB, zDistance);
            _physicsScale = Vector3.Distance(worldA, worldB);
            return;
        }

        CanvasScaler scaler = _canvas ? _canvas.GetComponentInParent<CanvasScaler>() : null;
        float scaleFactor = scaler ? scaler.scaleFactor : 1f;
        if (!useCanvasScaleForPhysics)
        {
            scaleFactor = 1f;
        }
        float worldUnitsPerScreenPixel =
            (2f * worldCamera.orthographicSize) / Mathf.Max(1f, Screen.height);
        _physicsScale = worldUnitsPerScreenPixel * scaleFactor;
    }

    private Vector3 UIToWorldPoint(RectTransform rect, Vector2 localPoint, float zDistance)
    {
        Vector3 uiWorld = rect.TransformPoint(localPoint);
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, uiWorld);
        Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, zDistance));
        world.z = physicsPlaneZ;
        return world;
    }

    private bool HasScaleChanged()
    {
        CanvasScaler scaler = _canvas ? _canvas.GetComponentInParent<CanvasScaler>() : null;
        float scaleFactor = scaler ? scaler.scaleFactor : 1f;

        if (_lastScaleFactor < 0f)
        {
            _lastScaleFactor = scaleFactor;
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            return false;
        }

        if (
            Mathf.Abs(scaleFactor - _lastScaleFactor) > 0.0001f
            || _lastScreenWidth != Screen.width
            || _lastScreenHeight != Screen.height
        )
        {
            _lastScaleFactor = scaleFactor;
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            return true;
        }

        return false;
    }

    private void RebuildPhysicsBodies()
    {
        UpdatePhysicsScale();

        for (int i = 0; i < _alive.Count; i++)
        {
            RainTile tile = _alive[i];
            if (tile == null || tile.rect == null)
            {
                continue;
            }

            Quaternion desiredRotation = tile.body
                ? tile.body.transform.rotation
                : tile.lastRotation;
            tile.lastRotation = desiredRotation;

            if (tile.body)
            {
                Destroy(tile.body.gameObject);
                tile.body = null;
            }

            tile.body = CreatePhysicsBody(
                tile.rect.gameObject,
                tile.rect,
                ignoreRectRotation: !bakeColliderRotation,
                out PolygonCollider2D physicsCollider
            );

            if (physicsCollider)
            {
                tile.collider = physicsCollider;
            }

            if (tile.body && !bakeColliderRotation)
            {
                ApplyInitialRotation(tile.body, tile.rect, desiredRotation);
            }
        }

        StartCoroutine(RestoreVisualRotationsEndOfFrame());
    }

    private IEnumerator RestoreVisualRotationsEndOfFrame()
    {
        yield return new WaitForEndOfFrame();

        for (int i = 0; i < _alive.Count; i++)
        {
            RainTile tile = _alive[i];
            if (tile == null || tile.rect == null)
            {
                continue;
            }

            if (lockVisualRotation)
            {
                tile.rect.localRotation = Quaternion.identity;
            }
            else
            {
                tile.rect.rotation = tile.lastRotation;
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!drawCenterGizmo)
        {
            return;
        }

        Gizmos.color = centerGizmoColor;

        for (int i = 0; i < _alive.Count; i++)
        {
            RainTile tile = _alive[i];
            if (tile == null)
            {
                continue;
            }

            if (tile.rect != null)
            {
                Vector3 center = GetVisualCenterWorld(tile);
                Gizmos.DrawWireSphere(center, 0.08f);
                Gizmos.DrawLine(center + Vector3.left * 0.2f, center + Vector3.right * 0.2f);
                Gizmos.DrawLine(center + Vector3.up * 0.2f, center + Vector3.down * 0.2f);
            }
        }
    }
#endif

    private Vector3 GetVisualCenterWorld(RainTile tile)
    {
        if (tile == null || tile.rect == null)
        {
            return Vector3.zero;
        }

        Vector2 localCenter = GetVisualCenterLocal(tile);
        return tile.rect.TransformPoint(localCenter);
    }

    private Vector2 GetVisualCenterLocal(RainTile tile)
    {
        if (tile == null || tile.rect == null)
        {
            return Vector2.zero;
        }

        if (assumeEquilateralTriangle)
        {
            Vector2 center = tile.rect.rect.center;
            center.y += tile.rect.rect.height * triangleCenterYOffset;
            return center;
        }

        if (tile.image == null || tile.image.sprite == null)
        {
            return tile.rect.rect.center;
        }

        Sprite sprite = tile.image.sprite;
        Vector2 spriteSize = sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return tile.rect.rect.center;
        }

        Vector2 rectSize = tile.rect.rect.size;
        Vector2 scale = new Vector2(rectSize.x / spriteSize.x, rectSize.y / spriteSize.y);
        Vector2 spriteCenter = sprite.bounds.center;
        return new Vector2(spriteCenter.x * scale.x, spriteCenter.y * scale.y);
    }

    private Rigidbody2D CreatePhysicsBody(
        GameObject uiTile,
        RectTransform rect,
        bool ignoreRectRotation,
        out PolygonCollider2D physicsCollider
    )
    {
        physicsCollider = null;
        if (!worldCamera || rect == null)
        {
            return null;
        }

        Quaternion originalRotation = rect.rotation;
        if (ignoreRectRotation)
        {
            rect.rotation = Quaternion.identity;
        }

        GameObject bodyGo = new GameObject($"{uiTile.name}_Physics");
        bodyGo.layer = gameObject.layer;
        bodyGo.transform.SetParent(physicsRoot, false);

        float zDistance = physicsPlaneZ - worldCamera.transform.position.z;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, rect.position);
        Vector3 worldPos = worldCamera.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, zDistance)
        );
        worldPos.z = physicsPlaneZ;
        bodyGo.transform.position = worldPos;

        Rigidbody2D body = bodyGo.AddComponent<Rigidbody2D>();
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.sleepMode = RigidbodySleepMode2D.NeverSleep;
        body.gravityScale = 0f;

        PolygonCollider2D sourceCollider = uiTile.GetComponent<PolygonCollider2D>();
        if (sourceCollider)
        {
            PolygonCollider2D collider = bodyGo.AddComponent<PolygonCollider2D>();
            collider.isTrigger = sourceCollider.isTrigger;
            physicsCollider = collider;

            RectTransform sourceRect = sourceCollider.transform as RectTransform;
            if (sourceRect != null)
            {
                int pathCount = sourceCollider.pathCount;
                collider.pathCount = pathCount;
                for (int p = 0; p < pathCount; p++)
                {
                    Vector2[] srcPath = sourceCollider.GetPath(p);
                    Vector2[] dstPath = new Vector2[srcPath.Length];
                    for (int i = 0; i < srcPath.Length; i++)
                    {
                        Vector3 uiWorld = sourceRect.TransformPoint(srcPath[i]);
                        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, uiWorld);
                        Vector3 world = worldCamera.ScreenToWorldPoint(
                            new Vector3(screen.x, screen.y, zDistance)
                        );
                        dstPath[i] = new Vector2(world.x - worldPos.x, world.y - worldPos.y);
                    }

                    collider.SetPath(p, dstPath);
                }
            }
        }

        if (ignoreRectRotation)
        {
            rect.rotation = originalRotation;
        }

        return body;
    }

    private void DestroyTile(RainTile tile)
    {
        if (tile == null)
        {
            return;
        }

        if (tile.body)
        {
            Destroy(tile.body.gameObject);
        }

        if (tile.rect)
        {
            Destroy(tile.rect.gameObject);
        }
        else if (tile.transform)
        {
            Destroy(tile.transform.gameObject);
        }
    }

    void LateUpdate()
    {
        if (!useWorldSpacePhysics || !worldCamera)
        {
            return;
        }

        float zDistance = physicsPlaneZ - worldCamera.transform.position.z;

        for (int i = _alive.Count - 1; i >= 0; i--)
        {
            RainTile tile = _alive[i];
            if (tile == null || tile.rect == null || tile.body == null)
            {
                continue;
            }

            Vector3 worldPos = tile.body.transform.position;
            Vector3 screen = worldCamera.WorldToScreenPoint(
                new Vector3(worldPos.x, worldPos.y, physicsPlaneZ)
            );
            if (_rootRect != null)
            {
                if (
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _rootRect,
                        screen,
                        _canvas ? _canvas.worldCamera : null,
                        out Vector2 local
                    )
                )
                {
                    tile.rect.anchoredPosition = local;
                }
            }

            if (lockVisualRotation)
            {
                tile.rect.localRotation = Quaternion.identity;
            }
            else
            {
                if (!bakeColliderRotation)
                {
                    tile.rect.rotation = tile.body.transform.rotation;
                }
            }

            tile.lastRotation = tile.rect.rotation;
        }

        if (HasScaleChanged())
        {
            _pendingResizeTimer = resizeDebounceSeconds;
        }

        if (_pendingResizeTimer >= 0f)
        {
            _pendingResizeTimer -= Time.unscaledDeltaTime;
            if (_pendingResizeTimer <= 0f)
            {
                _pendingResizeTimer = -1f;
                RebuildPhysicsBodies();
            }
        }
    }

    private void HandlePointerClick()
    {
        if (!IsPrimaryClickDown(out Vector2 screenPos))
        {
            return;
        }

        if (IsPointerOverInteractiveUI(screenPos))
        {
            return;
        }

        if (!TryGetWorldPoint(screenPos, out Vector3 worldPoint))
        {
            return;
        }

        RainTile hitTile = GetTopmostTileAtPoint(worldPoint);
        if (hitTile == null || hitTile.rect == null)
        {
            return;
        }

        SpawnBurst(hitTile, screenPos);
    }

    private void ApplyInitialRotation(Rigidbody2D body, RectTransform rect, Quaternion rotation)
    {
        if (!body)
        {
            return;
        }

        float z = rotation.eulerAngles.z;
        body.SetRotation(z);

        if (rect)
        {
            rect.rotation = rotation;
        }
    }

    private bool IsPrimaryClickDown(out Vector2 screenPos)
    {
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null)
        {
            UnityEngine.InputSystem.Controls.TouchControl touch = Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                screenPos = touch.position.ReadValue();
                return true;
            }
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPos = Mouse.current.position.ReadValue();
            return true;
        }
#else
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                screenPos = touch.position;
                return true;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            screenPos = Input.mousePosition;
            return true;
        }
#endif

        screenPos = default;
        return false;
    }

    private bool IsPointerOverInteractiveUI(Vector2 screenPos)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointer = new PointerEventData(EventSystem.current)
        {
            position = screenPos,
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);
        if (results.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < results.Count; i++)
        {
            GameObject go = results[i].gameObject;
            if (!go)
            {
                continue;
            }

            if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(go) != null)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetWorldPoint(Vector2 screenPos, out Vector3 worldPoint)
    {
        if (useWorldSpacePhysics && worldCamera)
        {
            float zDistance = physicsPlaneZ - worldCamera.transform.position.z;
            Vector3 wp = worldCamera.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, zDistance)
            );
            wp.z = physicsPlaneZ;
            worldPoint = wp;
            return true;
        }

        Camera eventCamera = _canvas ? _canvas.worldCamera : null;
        RectTransform refRect = spawnArea ? spawnArea : transform as RectTransform;
        if (refRect != null)
        {
            return RectTransformUtility.ScreenPointToWorldPointInRectangle(
                refRect,
                screenPos,
                eventCamera,
                out worldPoint
            );
        }

        if (Camera.main)
        {
            Vector3 wp = Camera.main.ScreenToWorldPoint(screenPos);
            wp.z = 0f;
            worldPoint = wp;
            return true;
        }

        worldPoint = default;
        return false;
    }

    private RainTile GetTopmostTileAtPoint(Vector3 worldPoint)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint, clickLayers);
        if (hits == null || hits.Length == 0)
        {
            return null;
        }

        RainTile best = null;
        int bestSibling = int.MinValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (!hit)
            {
                continue;
            }

            for (int j = 0; j < _alive.Count; j++)
            {
                RainTile tile = _alive[j];
                if (tile == null || tile.transform == null || tile.collider != hit)
                {
                    continue;
                }

                int sibling = tile.transform.GetSiblingIndex();
                if (sibling > bestSibling)
                {
                    bestSibling = sibling;
                    best = tile;
                }
            }
        }

        return best;
    }

    private void SpawnBurst(RainTile tile, Vector2 screenPos)
    {
        RectTransform parentRect = tile.rect.parent as RectTransform;
        if (parentRect == null)
        {
            return;
        }

        Camera eventCamera = _canvas ? _canvas.worldCamera : null;

        if (
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                screenPos,
                eventCamera,
                out Vector2 originLocal
            )
        )
        {
            return;
        }

        Vector3 centerWorld = GetVisualCenterWorld(tile);
        Vector2 centerLocal = parentRect.InverseTransformPoint(centerWorld);

        Vector2 dirLocal = originLocal - centerLocal;
        if (dirLocal.sqrMagnitude < 0.001f)
        {
            dirLocal = Vector2.up;
        }

        dirLocal = -dirLocal.normalized;

        float torqueSign = 0f;
        float torqueStrength = 1f;
        if (TryGetWorldPoint(screenPos, out Vector3 hitWorld))
        {
            torqueSign = hitWorld.x < centerWorld.x ? -1f : 1f;
            float distance = Mathf.Abs(hitWorld.x - centerWorld.x);
            float radius = Mathf.Max(0.001f, tile.rect.rect.width * 0.5f);
            torqueStrength = Mathf.Clamp01(distance / radius);
        }

        ApplyKick(tile, dirLocal, torqueSign, torqueStrength);

        Sprite sprite = tile.image ? tile.image.sprite : null;
        Color baseColor = tile.image ? tile.image.color : Color.white;

        StartCoroutine(BurstRoutine(parentRect, originLocal, dirLocal, sprite, baseColor));
    }

    private void ApplyKick(RainTile tile, Vector2 dirLocal, float torqueSign, float torqueStrength)
    {
        if (tile.body == null)
        {
            return;
        }

        Vector2 dir = dirLocal.normalized;
        tile.body.linearVelocity = Vector2.zero;

        if (burstKickSpeed > 0f)
        {
            float scale = useWorldSpacePhysics ? _physicsScale : 1f;
            tile.body.linearVelocity = dir * burstKickSpeed * scale;
        }

        if (burstKickImpulse > 0f)
        {
            float scale = useWorldSpacePhysics ? _physicsScale : 1f;
            tile.body.AddForce(dir * burstKickImpulse * scale, ForceMode2D.Impulse);
        }

        if (burstKickTorque != 0f)
        {
            float scale = useWorldSpacePhysics ? _physicsScale : 1f;
            float torque = burstKickTorque * scale * torqueStrength;
            if (torqueSign != 0f)
            {
                torque *= torqueSign;
            }
            tile.body.AddTorque(torque, ForceMode2D.Impulse);
        }
    }

    private IEnumerator BurstRoutine(
        RectTransform parent,
        Vector2 originLocal,
        Vector2 dirLocal,
        Sprite sprite,
        Color baseColor
    )
    {
        if (burstShardCount <= 0 || burstDuration <= 0f)
        {
            yield break;
        }

        List<Image> shards = new List<Image>(burstShardCount);
        List<Vector2> targets = new List<Vector2>(burstShardCount);
        List<float> sizes = new List<float>(burstShardCount);

        float sizeBase = Mathf.Max(8f, burstDistance * burstSizeScale);

        for (int i = 0; i < burstShardCount; i++)
        {
            GameObject shard = new GameObject(
                "WhiteTileBurstShard",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            RectTransform rect = shard.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = originLocal;
            rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            float size = sizeBase * Random.Range(0.7f, 1.15f);
            rect.sizeDelta = new Vector2(size, size);

            Image img = shard.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            img.color = baseColor;

            float angle = Random.Range(-burstSpreadAngle * 0.5f, burstSpreadAngle * 0.5f);
            Vector2 dir = Rotate(dirLocal, angle);
            float distance = burstDistance * Random.Range(0.7f, 1.1f);
            Vector2 target = originLocal + dir * distance;

            shards.Add(img);
            targets.Add(target);
            sizes.Add(size);
        }

        float elapsed = 0f;
        while (elapsed < burstDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / burstDuration);
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            float alpha = Mathf.Lerp(1f, 0f, t);

            for (int i = 0; i < shards.Count; i++)
            {
                Image img = shards[i];
                if (!img)
                {
                    continue;
                }

                RectTransform rect = img.rectTransform;
                rect.anchoredPosition = Vector2.Lerp(originLocal, targets[i], ease);
                float scale = Mathf.Lerp(1f, 0.6f, t);
                rect.localScale = Vector3.one * scale;

                Color c = img.color;
                c.a = baseColor.a * alpha;
                img.color = c;
            }

            yield return null;
        }

        for (int i = 0; i < shards.Count; i++)
        {
            if (shards[i])
            {
                Destroy(shards[i].gameObject);
            }
        }
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}

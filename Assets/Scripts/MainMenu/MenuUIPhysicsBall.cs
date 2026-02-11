using UnityEngine;
using UnityEngine.UI;

public class MenuUIPhysicsBall : MonoBehaviour
{
    public RectTransform uiRect;
    public Camera worldCamera;
    public float physicsPlaneZ = 0f;
    public Transform physicsRoot;
    public bool disableUIPhysics = true;
    public float gravityScaleMultiplier = 0.25f;
    public float linearDampingMultiplier = 1f;
    public bool startActive = true;

    [Header("Respawn")]
    public RectTransform respawnArea;
    public bool respawnAtCenter = true;
    public float respawnX = 0f;
    public float respawnYOffset = 50f;
    public float killYThreshold = -10f;

    private Canvas _canvas;
    private RectTransform _rootRect;
    private Rigidbody2D _uiBody;
    private CircleCollider2D _uiCollider;
    private Rigidbody2D _body;
    private CircleCollider2D _collider;
    private float _lastScaleFactor = -1f;
    private int _lastScreenWidth = -1;
    private int _lastScreenHeight = -1;
    private bool _physicsActive;

    void Awake()
    {
        _physicsActive = startActive;
        if (!uiRect)
        {
            uiRect = GetComponent<RectTransform>();
        }

        _canvas = GetComponentInParent<Canvas>();
        _rootRect =
            uiRect && uiRect.parent
                ? uiRect.parent as RectTransform
                : (_canvas ? _canvas.transform as RectTransform : uiRect);

        if (!worldCamera)
        {
            worldCamera = Camera.main;
        }

        EnsurePhysicsRoot();

        _uiBody = GetComponent<Rigidbody2D>();
        _uiCollider = GetComponent<CircleCollider2D>();

        if (disableUIPhysics)
        {
            if (_uiBody)
            {
                _uiBody.simulated = false;
            }

            if (_uiCollider)
            {
                _uiCollider.enabled = false;
            }
        }

        if (_physicsActive)
        {
            CreatePhysicsBody();
        }
    }

    void LateUpdate()
    {
        if (!_physicsActive || !_body || !_rootRect)
        {
            return;
        }

        if (_body.position.y < killYThreshold)
        {
            RespawnAtTop();
        }

        Quaternion rotationOffset =
            uiRect.localRotation * Quaternion.Inverse(_body.transform.rotation);

        if (HasScaleChanged())
        {
            RebuildPhysicsBody();
        }

        Vector3 worldPos = _body.transform.position;
        Vector3 screen = worldCamera.WorldToScreenPoint(
            new Vector3(worldPos.x, worldPos.y, physicsPlaneZ)
        );

        if (
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootRect,
                screen,
                _canvas ? _canvas.worldCamera : null,
                out Vector2 local
            )
        )
        {
            uiRect.anchoredPosition = local;
        }

        uiRect.localRotation = _body.transform.rotation * rotationOffset;
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
            if (_body == null)
            {
                CreatePhysicsBody();
            }
        }
        else
        {
            if (_body)
            {
                Destroy(_body.gameObject);
                _body = null;
                _collider = null;
            }
        }
    }

    public void SetPhysicsRoot(Transform newRoot, bool rebuild = true)
    {
        if (newRoot == physicsRoot)
        {
            return;
        }

        physicsRoot = newRoot;

        if (rebuild && _physicsActive)
        {
            RebuildPhysicsBody();
        }
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

    private void RebuildPhysicsBody()
    {
        if (!_body)
        {
            return;
        }

        Vector2 velocity = _body.linearVelocity;
        float angularVelocity = _body.angularVelocity;

        Destroy(_body.gameObject);
        _body = null;
        _collider = null;

        CreatePhysicsBody();

        if (_body)
        {
            _body.linearVelocity = velocity;
            _body.angularVelocity = angularVelocity;
        }
    }

    private void CreatePhysicsBody()
    {
        if (!worldCamera || !uiRect)
        {
            return;
        }

        if (_body != null)
        {
            return;
        }

        EnsurePhysicsRoot();
        if (!physicsRoot)
        {
            return;
        }

        string bodyName = $"{gameObject.name}_Physics";
        Transform existingBody = physicsRoot.Find(bodyName);
        if (existingBody)
        {
            Destroy(existingBody.gameObject);
        }

        GameObject bodyGo = new GameObject(bodyName);
        bodyGo.layer = gameObject.layer;
        bodyGo.transform.SetParent(physicsRoot, false);

        float zDistance = physicsPlaneZ - worldCamera.transform.position.z;

        Vector3 centerWorld = uiRect.TransformPoint(uiRect.rect.center);
        Vector3 worldCenter = ScreenToWorld(centerWorld, zDistance);
        bodyGo.transform.position = worldCenter;

        _body = bodyGo.AddComponent<Rigidbody2D>();
        _body.bodyType = _uiBody ? _uiBody.bodyType : RigidbodyType2D.Dynamic;
        _body.interpolation = _uiBody
            ? _uiBody.interpolation
            : RigidbodyInterpolation2D.Interpolate;
        _body.sleepMode = _uiBody ? _uiBody.sleepMode : RigidbodySleepMode2D.NeverSleep;
        float baseGravity = _uiBody ? _uiBody.gravityScale : 1f;
        _body.gravityScale = baseGravity * gravityScaleMultiplier;
        _body.mass = _uiBody ? _uiBody.mass : 1f;
        float baseDamping = _uiBody ? _uiBody.linearDamping : 0f;
        _body.linearDamping = baseDamping * linearDampingMultiplier;
        _body.angularDamping = _uiBody ? _uiBody.angularDamping : 0.05f;
        _body.collisionDetectionMode = _uiBody
            ? _uiBody.collisionDetectionMode
            : CollisionDetectionMode2D.Discrete;
        _body.constraints = _uiBody ? _uiBody.constraints : RigidbodyConstraints2D.None;
        _body.sharedMaterial = _uiBody ? _uiBody.sharedMaterial : null;
        _body.useFullKinematicContacts = _uiBody && _uiBody.useFullKinematicContacts;

        _collider = bodyGo.AddComponent<CircleCollider2D>();
        if (_uiCollider && _uiCollider.sharedMaterial)
        {
            _collider.sharedMaterial = _uiCollider.sharedMaterial;
        }

        if (_uiCollider)
        {
            Vector3 uiCenter = uiRect.TransformPoint(
                _uiCollider.offset + (Vector2)uiRect.rect.center
            );
            Vector3 uiEdge = uiRect.TransformPoint(
                _uiCollider.offset
                    + (Vector2)uiRect.rect.center
                    + Vector2.right * _uiCollider.radius
            );

            Vector3 worldCenter2 = ScreenToWorld(uiCenter, zDistance);
            Vector3 worldEdge = ScreenToWorld(uiEdge, zDistance);

            _collider.radius = Vector2.Distance(
                new Vector2(worldCenter2.x, worldCenter2.y),
                new Vector2(worldEdge.x, worldEdge.y)
            );
            _body.transform.position = worldCenter2;
        }
        else
        {
            Vector3 uiEdge = uiRect.TransformPoint(
                uiRect.rect.center + Vector2.right * (uiRect.rect.width * 0.5f)
            );
            Vector3 worldEdge = ScreenToWorld(uiEdge, zDistance);
            _collider.radius = Vector2.Distance(
                new Vector2(worldCenter.x, worldCenter.y),
                new Vector2(worldEdge.x, worldEdge.y)
            );
        }
    }

    private void RespawnAtTop()
    {
        if (_body == null || worldCamera == null)
            return;

        RectTransform area = respawnArea != null ? respawnArea : _rootRect;
        if (area == null)
            return;

        float x = respawnAtCenter ? 0f : respawnX;
        float y = area.rect.yMax + respawnYOffset;

        Vector3 uiWorld = area.TransformPoint(new Vector3(x, y, 0f));
        float zDistance = physicsPlaneZ - worldCamera.transform.position.z;
        Vector3 worldPos = ScreenToWorld(uiWorld, zDistance);

        _body.position = new Vector2(worldPos.x, worldPos.y);
        _body.linearVelocity = Vector2.zero;
        _body.angularVelocity = 0f;
        _body.rotation = 0f;
    }

    private Vector3 ScreenToWorld(Vector3 uiWorld, float zDistance)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, uiWorld);
        Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, zDistance));
        world.z = physicsPlaneZ;
        return world;
    }

    private void EnsurePhysicsRoot()
    {
        if (physicsRoot)
        {
            return;
        }

        GameObject existing = GameObject.Find("MenuPhysicsBodies");
        if (existing)
        {
            physicsRoot = existing.transform;
        }
        else
        {
            GameObject root = new GameObject("MenuPhysicsBodies");
            root.transform.position = new Vector3(0f, 0f, physicsPlaneZ);
            physicsRoot = root.transform;
        }
    }
}

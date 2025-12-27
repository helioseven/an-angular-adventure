using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RainWhites : MonoBehaviour
{
    [Header("Spawn")]
    public RectTransform spawnArea;
    public GameObject tileTemplate;
    public float minSpawnInterval = 0.1f;
    public float maxSpawnInterval = 0.35f;
    public float spawnBuffer = 50f;
    public int maxAlive = 40;
    public float maxRandomZRotation = 360f;
    public float fallSpeedVariance = 0.2f;

    [Header("Motion")]
    public float fallSpeed = 200f;
    public float swayDistance = 30f;
    public float swayFrequency = 0.4f;
    public float velocitySmoothing = 10f;
    public float despawnBuffer = 150f;

    private readonly List<RainTile> _alive = new List<RainTile>();
    private Coroutine _spawnRoutine;

    private class RainTile
    {
        public Transform transform;
        public RectTransform rect;
        public Rigidbody2D body;
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

        if (!tileTemplate && transform.childCount > 0)
        {
            Transform named = transform.Find("White Triangle");
            tileTemplate = named ? named.gameObject : transform.GetChild(0).gameObject;
        }

        if (tileTemplate)
        {
            tileTemplate.SetActive(false);
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
                _alive.RemoveAt(i);
                continue;
            }

            if (tile.rect)
            {
                if (!tile.body)
                {
                    Vector2 rectPos = tile.rect.anchoredPosition;
                    rectPos.y -= tile.fallSpeed * Time.deltaTime;
                    rectPos.x = tile.spawnX + Mathf.Sin(t * tile.swayFrequency + tile.swayPhase) * tile.swayDistance;
                    tile.rect.anchoredPosition = rectPos;
                }

                Vector2 rectPosCheck = tile.rect.anchoredPosition;
                if (rectPosCheck.y < bottomY - despawnBuffer)
                {
                    Destroy(tile.rect.gameObject);
                    _alive.RemoveAt(i);
                }
            }
            else
            {
                if (!tile.body)
                {
                    Vector3 localPos = tile.transform.localPosition;
                    localPos.y -= tile.fallSpeed * Time.deltaTime;
                    localPos.x = tile.spawnX + Mathf.Sin(t * tile.swayFrequency + tile.swayPhase) * tile.swayDistance;
                    tile.transform.localPosition = localPos;
                }

                Vector3 localPosCheck = tile.transform.localPosition;
                if (localPosCheck.y < bottomY - despawnBuffer)
                {
                    Destroy(tile.transform.gameObject);
                    _alive.RemoveAt(i);
                }
            }
        }
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
                float targetVx = Mathf.Cos(phase) * tile.swayDistance * tile.swayFrequency;
                Vector2 target = new Vector2(targetVx, -tile.fallSpeed);
                tile.body.linearVelocity = Vector2.Lerp(tile.body.linearVelocity, target, smoothT);
            }
        }
    }
    private IEnumerator SpawnLoop()
    {
        while (true)
        {
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

        Transform parent = spawnArea ? spawnArea : transform;
        GameObject go = Instantiate(tileTemplate, parent);
        go.SetActive(true);

        float x = Random.Range(GetLeftX(), GetRightX());
        float y = GetTopY() + spawnBuffer;

        RectTransform rect = go.GetComponent<RectTransform>();
        Rigidbody2D body = go.GetComponent<Rigidbody2D>();
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
            body.linearVelocity = new Vector2(0f, -fallSpeed);
        }

        float speed = fallSpeed * Random.Range(1f - fallSpeedVariance, 1f + fallSpeedVariance);
        float swayDist = swayDistance * Random.Range(0.7f, 1.3f);
        float swayFreq = swayFrequency * Random.Range(0.8f, 1.2f);
        float swayPhase = Random.Range(0f, Mathf.PI * 2f);

        _alive.Add(new RainTile
        {
            transform = go.transform,
            rect = rect,
            body = body,
            fallSpeed = speed,
            swayDistance = swayDist,
            swayFrequency = swayFreq,
            swayPhase = swayPhase,
            spawnX = x
        });
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
}

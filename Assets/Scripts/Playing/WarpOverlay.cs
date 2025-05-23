using UnityEngine;

public class WarpOverlay : MonoBehaviour
{
    public float pulseSpeed = 2.5f;
    public float pulseScale = 0.02f;
    public float spinSpeed = 120f; // degrees per second
    private Vector3 baseScale;
    private SpriteRenderer sr;

    void Start()
    {
        baseScale = transform.localScale;
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // pulse slightly in size
        float t = Mathf.Sin(Time.time * pulseSpeed) * pulseScale + 1f;
        transform.localScale = baseScale * t;

        // spin
        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
    }
}

using UnityEngine;
using circleXsquares;

public class Victory : MonoBehaviour
{
    // public variables
    public VictoryData data;
    public float pullRadius;
    public float pullForce;

    // private constants
    private readonly string[] INT_TO_NAME = {
        "Zero",
        "One",
        "Two",
        "Three",
        "Four",
        "Five",
        "Six",
        "Seven",
        "Eight",
        "Nine"
    };

    // private references
    private PlayGM play_gm;

    void Awake()
    {
        play_gm = PlayGM.instance;
    }

    void Update()
    {
        transform.Rotate(Vector3.forward * Time.deltaTime * 100);
    }

    void FixedUpdate()
    {
        int mask = 1 << LayerMask.NameToLayer(INT_TO_NAME[data.layer]);
        Collider2D[] c2ds = Physics2D.OverlapCircleAll(transform.position, pullRadius, mask);
        foreach (Collider2D c in c2ds) {
            if (c.gameObject.CompareTag("Player")) {
                play_gm.RegisterVictory(this);

                // calculate direction from target to victory center
                Vector2 forceDirection = transform.position - c.transform.position;

                // apply force on target towards victory center
                Rigidbody2D rb2d = c.GetComponent<Rigidbody2D>();
                rb2d.AddForce(forceDirection.normalized * pullForce * Time.fixedDeltaTime);
            }
        }
    }

    /* Override Functions */

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player")) {
            play_gm.RegisterVictory(this);
            play_gm.soundManager.Play("victory");
        }
    }
}

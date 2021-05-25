using UnityEngine;
using circleXsquares;

public class Victory : MonoBehaviour
{

    public float pullRadius;
    public float pullForce;

    private PlayGM play_gm;

    public VictoryData data;

    private readonly string[] INT_TO_NAME = { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine" };

    void Awake()
    {
        play_gm = PlayGM.instance;
    }

    void Update()
    {
        transform.Rotate(Vector3.forward * Time.deltaTime * 100);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            play_gm.RegisterVictory(this);

            play_gm.soundManager.Play("victory");
        }
    }

    public void FixedUpdate()
    {
        foreach (Collider2D c in Physics2D.OverlapCircleAll(transform.position, pullRadius, 1 << LayerMask.NameToLayer(INT_TO_NAME[data.layer])))
        {
            if (c.gameObject.CompareTag("Player"))
            {
                play_gm.RegisterVictory(this);

                // calculate direction from target to victory center
                Vector2 forceDirection = transform.position - c.transform.position;

                // apply force on target towards victory center
                Rigidbody2D rb2d = c.GetComponent<Rigidbody2D>();
                rb2d.AddForce(forceDirection.normalized * pullForce * Time.fixedDeltaTime);
            }
        }
    }
}

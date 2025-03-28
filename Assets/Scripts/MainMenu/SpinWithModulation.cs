using UnityEngine;

public class SpinWithModulation : MonoBehaviour
{
    public float baseSpeed = 0f;
    public float modulationAmount = 420f;
    public float modulationSpeed = 0.007f;
    public bool useSine = false;

    void Update()
    {
        float modulatedSpeed;

        if (useSine)
        {
            // Oscillates over time with a sine wave
            modulatedSpeed = baseSpeed + Mathf.Sin(Time.time * modulationSpeed) * modulationAmount;
        }
        else
        {
            // Uses Perlin noise for more irregular motion
            float noise = Mathf.PerlinNoise(Time.time * modulationSpeed, 0f);
            modulatedSpeed = baseSpeed + (noise - 0.5f) * 2f * modulationAmount;
        }

        transform.Rotate(0f, 0f, modulatedSpeed * Time.deltaTime);
    }
}

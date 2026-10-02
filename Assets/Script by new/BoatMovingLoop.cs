using UnityEngine;

public class BoatMovingLoop : MonoBehaviour
{
    [Header("Movement (Left to Right)")]
    public float moveSpeed = 2f;         // Speed of movement
    public float distanceToTravel = 30f; // Distance before looping back to start

    [Header("Wave Bobbing and Tilting")]
    public float waveSpeed = 2f;         // Wave frequency/speed
    public float waveHeight = 0.15f;     // Vertical bobbing height
    public float tiltAngle = 3f;         // Tilting angle range

    private float initialX;
    private float initialY;

    void Start()
    {
        // Cache the initial starting position from the scene
        initialX = transform.position.x;
        initialY = transform.position.y;
    }

    void Update()
    {
        // 1. Move to the right
        float currentX = transform.position.x + (moveSpeed * Time.deltaTime);

        // Reset to initial position once distance threshold is reached
        if (currentX > initialX + distanceToTravel)
        {
            currentX = initialX;
        }

        // 2. Calculate vertical wave movement
        float newY = initialY + (Mathf.Sin(Time.time * waveSpeed) * waveHeight);

        // Update position
        transform.position = new Vector3(currentX, newY, transform.position.z);

        // 3. Apply tilt rotation
        float angle = Mathf.Sin(Time.time * (waveSpeed * 0.8f)) * tiltAngle;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
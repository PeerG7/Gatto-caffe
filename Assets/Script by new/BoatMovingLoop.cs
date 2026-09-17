using UnityEngine;

public class BoatMovingLoop : MonoBehaviour
{
    [Header("การแล่นข้ามจอ (ซ้ายไปขวา)")]
    public float moveSpeed = 2f;         // ความเร็วในการแล่น
    public float distanceToTravel = 30f; // ระยะทางที่ให้แล่นไปทางขวาก่อนจะวนกลับมาที่เดิม

    [Header("การกระเพื่อมและโคลงเคลง")]
    public float waveSpeed = 2f;         // ความเร็วคลื่น
    public float waveHeight = 0.15f;     // ความสูงการลอยขึ้น-ลง
    public float tiltAngle = 3f;         // มุมเอียงโคลงไปมา

    private float initialX;
    private float initialY;

    void Start()
    {
        // บันทึกตำแหน่งจุดเริ่มต้นตามที่คุณวางไว้ใน Scene ทันที
        initialX = transform.position.x;
        initialY = transform.position.y;
    }

    void Update()
    {
        // 1. ขยับไปทางขวาเรื่อยๆ
        float currentX = transform.position.x + (moveSpeed * Time.deltaTime);

        // ถ้าแล่นไปไกลเกินระยะทางที่กำหนด ให้วนกลับมาจุดตั้งต้นเดิม
        if (currentX > initialX + distanceToTravel)
        {
            currentX = initialX;
        }

        // 2. คำนวณคลื่นน้ำ
        float newY = initialY + (Mathf.Sin(Time.time * waveSpeed) * waveHeight);

        // อัปเดตพิกัด
        transform.position = new Vector3(currentX, newY, transform.position.z);

        // 3. เอียงเรือ
        float angle = Mathf.Sin(Time.time * (waveSpeed * 0.8f)) * tiltAngle;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
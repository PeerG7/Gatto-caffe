using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuTransitionHandler : MonoBehaviour
{
    [Header("Transition Prefabs (Assign 3 different animation prefabs here)")]
    public GameObject[] transitionPrefabs; // ใส่ Prefab แอนิเมชัน 3 แบบตรงนี้

    [Tooltip("Canvas หลักหรือตำแหน่งที่จะให้ Object ไป spawn ทับหน้าจอ (ถ้าว่างจะ spawn ที่ตำแหน่ง Canvas หรือ World Center)")]
    public Transform spawnParent;

    [Header("Loading Settings")]
    [Tooltip("เวลารอให้แอนิเมชันเล่นจนเสร็จก่อนจะโหลดฉากจริง")]
    public float transitionDuration = 1.0f;

    /// <summary>
    /// ฟังก์ชันนี้ผูกกับ OnClick ของปุ่ม Continue หรือ New Game
    /// </summary>
    /// <param name="sceneName">ชื่อฉากที่จะโหลด</param>
    public void PlayTransitionAndLoadScene(string sceneName)
    {
        StartCoroutine(TransitionRoutine(sceneName));
    }

    IEnumerator TransitionRoutine(string sceneName)
    {
        // 1. สุ่มเลือก Prefab แอนิเมชัน 1 ใน 3 ตัว
        if (transitionPrefabs != null && transitionPrefabs.Length > 0)
        {
            int randomIndex = Random.Range(0, transitionPrefabs.Length);
            GameObject selectedPrefab = transitionPrefabs[randomIndex];

            if (selectedPrefab != null)
            {
                // Spawn GameObject ขึ้นมาบนจอ
                GameObject spawnedEffect = Instantiate(selectedPrefab, spawnParent);

                // (Optional) ถ้าแอนิเมชันเป็น UI อาจจะต้องแน่ใจว่ามันถูก Render ทับบนสุด 
                // สามารถเซ็ต RectTransform ให้เต็มจอได้ถ้าจำเป็น
            }
        }

        // 2. หน่วงเวลาให้แอนิเมชันเล่น (ปรับความยาวตามความเหมาะสม)
        yield return new WaitForSecondsRealtime(transitionDuration);

        // 3. โหลดฉากเกมจริง
        SceneManager.LoadScene(sceneName);
    }
}
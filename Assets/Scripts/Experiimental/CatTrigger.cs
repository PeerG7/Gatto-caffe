using UnityEngine;

public class CatTrigger : MonoBehaviour
{
    public CatProfile myProfile;

    // ฟังก์ชันหลักที่ใช้สั่งโหลดฉาก
    public void Interact()
    {
        if (SceneLoader.Instance != null)
        {
            Debug.Log("กำลังโหลดฉากสำหรับแมว: " + myProfile.catName);
            SceneLoader.Instance.LoadRelationshipScene(myProfile);
        }
    }

    // ยังเก็บไว้เผื่ออยากคลิกด้วยเมาส์
    public void OnMouseDown()
    {
        Interact();
    }
}
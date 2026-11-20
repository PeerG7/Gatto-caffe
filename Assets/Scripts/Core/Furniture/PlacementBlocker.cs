using UnityEngine;

/// <summary>
/// วางบน GameObject "โซนห้ามวาง" (ต้องมี Collider2D + Is Trigger และอยู่ใน Layer ที่ติ๊กไว้ใน
/// FurnitureManager.unplaceableLayer / FurniturePlacementManager.interactableLayer)
/// ใช้กำหนดว่าโซนนี้ห้ามวางเฟอร์นิเจอร์ชิ้นไหนบ้าง โดยเทียบกับ FurnitureObject.furnitureID
///
/// ผู้เล่น/แมวเดินผ่านได้ตามปกติ เพราะเป็น Trigger และไม่มี NavMeshObstacle
/// </summary>
[DisallowMultipleComponent]
public class PlacementBlocker : MonoBehaviour
{
    public enum Mode
    {
        [Tooltip("ห้ามเฉพาะ ID ที่อยู่ในลิสต์ (ชิ้นอื่นวางได้)")]
        BlockListed,
        [Tooltip("ห้ามทุกชิ้น ยกเว้น ID ที่อยู่ในลิสต์ (วางได้เฉพาะชิ้นในลิสต์)")]
        BlockAllExceptListed
    }

    [Header("Rule")]
    public Mode mode = Mode.BlockListed;

    [Tooltip("furnitureID ของเฟอร์นิเจอร์ เช่น \"TableSet 1 Seat\"\nถ้าปล่อยว่าง = ห้ามทุกชิ้น")]
    public string[] furnitureIDs;

    [Header("Editor Gizmo")]
    public bool showGizmo = true;
    public Color gizmoColor = new Color(1f, 0f, 0f, 0.25f);

    /// <summary>true = โซนนี้ห้ามวางเฟอร์นิเจอร์ที่มี ID นี้</summary>
    public bool Blocks(string furnitureID)
    {
        // ลิสต์ว่าง = ห้ามทุกชิ้น (ไม่ว่าจะเลือกโหมดไหน)
        if (furnitureIDs == null || furnitureIDs.Length == 0) return true;

        bool listed = false;
        if (!string.IsNullOrEmpty(furnitureID))
        {
            for (int i = 0; i < furnitureIDs.Length; i++)
            {
                if (furnitureIDs[i] == furnitureID) { listed = true; break; }
            }
        }

        return mode == Mode.BlockListed ? listed : !listed;
    }

    /// <summary>
    /// ดึง ID ของเฟอร์นิเจอร์จาก GameObject/Prefab
    /// ใช้ FurnitureObject.furnitureID เป็นหลัก ถ้าไม่มีให้ใช้ชื่อ GameObject (ตัดคำว่า (Clone) ออก)
    /// </summary>
    public static string GetFurnitureID(GameObject obj)
    {
        if (obj == null) return null;

        FurnitureObject furn = obj.GetComponent<FurnitureObject>();
        if (furn != null && !string.IsNullOrEmpty(furn.furnitureID)) return furn.furnitureID;

        return obj.name.Replace("(Clone)", "").Trim();
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo) return;

        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        Bounds b = col.bounds;
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(b.center, b.size);
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawWireCube(b.center, b.size);
    }
}
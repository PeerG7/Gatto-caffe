using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =====================================================================
// ใส่ script นี้บน CatEntryPrefab root
// แล้วลาก child แต่ละตัวมาผูกใน Inspector โดยตรง
// ไม่ต้องสนใจชื่อ child อีกต่อไป
// =====================================================================
public class CatEntryUI : MonoBehaviour
{
    public Image portrait;
    public TMP_Text nameText;
    public TMP_Text labelText;
    public Image relBarFill; // ลาก Fill image มาใส่ตรงนี้
}
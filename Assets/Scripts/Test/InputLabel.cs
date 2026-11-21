using TMPro;
using UnityEngine;

// =====================================================================
// InputLabel — แปะบน TMP_Text เลือก Action แล้วข้อความจะเป็นชื่อปุ่มที่ถูกต้องตามอุปกรณ์
//
//   Action = Interact, Format = "[{0}] คุย"
//     เมาส์/คีย์บอร์ด → "[E] คุย"
//     Xbox            → "[A] คุย"
//     PlayStation     → "[Cross] คุย"
//     Nintendo        → "[B] คุย"
//
//  {0} = ชื่อปุ่ม  (ถ้าไม่ใส่ {0} ข้อความจะถูกใช้ตามที่พิมพ์ไว้)
//  ไม่ต้องมีรูปไอคอนก็ใช้ได้ — ถ้ามีรูปให้ใช้ InputIconSwitcher แทน/ควบคู่กัน
// =====================================================================
[DisallowMultipleComponent]
public class InputLabel : MonoBehaviour
{
    public TMP_Text text;
    public GameAction action = GameAction.Interact;
    [Tooltip("{0} = ชื่อปุ่ม เช่น \"[{0}] คุย\"")]
    public string format = "{0}";

    void Awake()
    {
        if (text == null) text = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        InputDeviceMonitor.Changed += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        InputDeviceMonitor.Changed -= Refresh;
    }

    public void Refresh()
    {
        if (text == null) return;

        string label = InputGlyphs.Label(action);
        try { text.text = string.Format(format, label); }
        catch (System.FormatException) { text.text = label; }   // กัน { } แปลก ๆ ใน format ทำให้ error
    }
}

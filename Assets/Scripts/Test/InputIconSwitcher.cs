using UnityEngine;
using UnityEngine.UI;

// =====================================================================
// InputIconSwitcher — แปะบน Image (UI) หรือ SpriteRenderer (ฟองลอยเหนือ station)
// แล้วลากรูปไอคอนของแต่ละอุปกรณ์ใส่ ระบบจะสลับรูปให้เองแบบ Realtime
//
//   เมาส์/คีย์บอร์ด → รูปในช่อง Keyboard Mouse
//   จอย             → รูปตามยี่ห้อ (Xbox / PlayStation / Nintendo)
//                     ช่องที่ว่างจะใช้รูปของ Xbox แทน
//
// เสริม: ใส่ GameObject ในช่อง Show On Keyboard Mouse / Show On Gamepad
//        เพื่อเปิด-ปิดกลุ่มคำแนะนำทั้งกลุ่มตามอุปกรณ์ (เช่น แผงปุ่มลัดของ Furniture)
// =====================================================================
[DisallowMultipleComponent]
public class InputIconSwitcher : MonoBehaviour
{
    [Header("Target (ว่าง = หา Image หรือ SpriteRenderer บน GameObject นี้เอง)")]
    public Image targetImage;
    public SpriteRenderer targetSprite;

    [Header("Sprites")]
    public Sprite keyboardMouse;
    public Sprite xbox;
    [Tooltip("ว่าง = ใช้รูป Xbox")]
    public Sprite playStation;
    [Tooltip("ว่าง = ใช้รูป Xbox")]
    public Sprite nintendo;

    [Header("Optional: เปิด/ปิดทั้งกลุ่มตามอุปกรณ์")]
    public GameObject[] showOnKeyboardMouse;
    public GameObject[] showOnGamepad;

    void Awake()
    {
        if (targetImage == null && targetSprite == null)
        {
            targetImage = GetComponent<Image>();
            if (targetImage == null) targetSprite = GetComponent<SpriteRenderer>();
        }
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
        bool pad = InputDeviceMonitor.UsingGamepad;

        Sprite pick;
        if (!pad) pick = keyboardMouse;
        else
        {
            switch (InputDeviceMonitor.Style)
            {
                case PadStyle.PlayStation: pick = playStation != null ? playStation : xbox; break;
                case PadStyle.Nintendo:    pick = nintendo != null ? nintendo : xbox; break;
                default:                   pick = xbox; break;
            }
        }

        // ช่องที่ยังไม่ได้ลากรูป → ไม่แตะรูปเดิม
        if (pick != null)
        {
            if (targetImage != null) targetImage.sprite = pick;
            if (targetSprite != null) targetSprite.sprite = pick;
        }

        SetGroup(showOnKeyboardMouse, !pad);
        SetGroup(showOnGamepad, pad);
    }

    static void SetGroup(GameObject[] group, bool active)
    {
        if (group == null) return;
        foreach (GameObject go in group)
            if (go != null && go.activeSelf != active) go.SetActive(active);
    }
}

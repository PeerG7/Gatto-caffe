using UnityEngine;
using UnityEngine.UI;

// =====================================================================
// InputIconSwitcher — แปะบน Image (UI) หรือ SpriteRenderer (ฟองลอยเหนือแมว / station)
// แล้วลากรูปไอคอนของแต่ละอุปกรณ์ใส่ ระบบจะสลับรูปให้เองแบบ Realtime
//
//   เมาส์/คีย์บอร์ด → รูปในช่อง Keyboard Mouse
//   จอย             → รูปตามยี่ห้อ (Xbox / PlayStation / Nintendo)
//                     ช่องที่ว่างจะใช้รูปของ Xbox แทน
//
// เวอร์ชันนี้ปรับปรุง:
//   • Reapply Every Frame: ตั้งรูปซ้ำใน LateUpdate ทุกเฟรม (LateUpdate รันหลัง Animator และสคริปต์อื่น)
//   • Force Switch: ย้ำสถานะเปิด-ปิดกลุ่ม Show On Keyboard Mouse / Show On Gamepad ทุกเฟรมใน LateUpdate
//   • หา Image / SpriteRenderer ในลูกด้วย ถ้า Component นี้ไม่ได้อยู่บนตัวที่วาดรูปโดยตรง
//   • เตือนใน Console ถ้าช่องรูปของอุปกรณ์ปัจจุบันว่าง (เดิมเงียบ ๆ แล้วรูปไม่เปลี่ยน)
// =====================================================================
[DisallowMultipleComponent]
public class InputIconSwitcher : MonoBehaviour
{
    [Header("Target (ว่าง = หา Image หรือ SpriteRenderer บน GameObject นี้ หรือในลูก)")]
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

    [Header("Behaviour")]
    [Tooltip("ตั้งรูปซ้ำทุกเฟรมใน LateUpdate กันสคริปต์/Animator อื่นตั้งรูปกลับเป็นรูปเดิม (เช่นตอนแมวมานั่ง)")]
    public bool reapplyEveryFrame = true;
    [Tooltip("บังคับเปิด-ปิดกลุ่ม Show On Keyboard Mouse / Show On Gamepad ตามสถานะปัจจุบันทุกเฟรมใน LateUpdate")]
    public bool forceSwitch = true;
    [Tooltip("เตือนใน Console ถ้าช่องรูปของอุปกรณ์ปัจจุบันยังว่าง")]
    public bool warnIfMissing = true;

    void Awake()
    {
        if (targetImage == null && targetSprite == null)
        {
            targetImage = GetComponent<Image>();
            if (targetImage == null) targetSprite = GetComponent<SpriteRenderer>();

            // ไม่ได้อยู่บนตัวที่วาดรูปโดยตรง → หาในลูก (รวมลูกที่ยังปิดอยู่)
            if (targetImage == null && targetSprite == null)
            {
                targetImage = GetComponentInChildren<Image>(true);
                if (targetImage == null) targetSprite = GetComponentInChildren<SpriteRenderer>(true);
            }
        }

        if (targetImage == null && targetSprite == null && warnIfMissing)
            Debug.LogWarning("[InputIconSwitcher] ไม่พบ Image หรือ SpriteRenderer บน '" + name + "' (หรือในลูก) — ลากใส่ช่อง Target เอง", this);
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

    void LateUpdate()
    {
        if (reapplyEveryFrame) ApplySprite(false);
        if (forceSwitch) ApplyGroups();
    }

    public void Refresh()
    {
        ApplySprite(true);
        ApplyGroups();
    }

    void ApplyGroups()
    {
        bool pad = InputDeviceMonitor.UsingGamepad;
        SetGroup(showOnKeyboardMouse, !pad);
        SetGroup(showOnGamepad, pad);
    }

    Sprite Pick()
    {
        if (!InputDeviceMonitor.UsingGamepad) return keyboardMouse;

        switch (InputDeviceMonitor.Style)
        {
            case PadStyle.PlayStation: return playStation != null ? playStation : xbox;
            case PadStyle.Nintendo: return nintendo != null ? nintendo : xbox;
            default: return xbox;
        }
    }

    void ApplySprite(bool warn)
    {
        Sprite pick = Pick();

        if (pick == null)
        {
            // ช่องรูปยังว่าง → ไม่แตะรูปเดิม (แต่เตือนครั้งเดียวตอนอุปกรณ์เปลี่ยน)
            if (warn && warnIfMissing)
                Debug.LogWarning("[InputIconSwitcher] '" + name + "' ยังไม่ได้ลากรูปสำหรับอุปกรณ์ " +
                                 InputDeviceMonitor.Current + " (" + InputDeviceMonitor.Style + ") — รูปจึงไม่เปลี่ยน", this);
            return;
        }

        if (targetImage != null && targetImage.sprite != pick) targetImage.sprite = pick;
        if (targetSprite != null && targetSprite.sprite != pick) targetSprite.sprite = pick;
    }

    static void SetGroup(GameObject[] group, bool active)
    {
        if (group == null) return;
        foreach (GameObject go in group)
            if (go != null && go.activeSelf != active) go.SetActive(active);
    }
}
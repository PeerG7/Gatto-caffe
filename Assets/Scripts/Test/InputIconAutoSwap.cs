using UnityEngine;
using UnityEngine.UI;

// =====================================================================
// InputIconAutoSwap — ตาข่ายกันตก: สแกน SpriteRenderer / Image ที่เปิดอยู่ในฉาก
// ถ้ารูปของมันอยู่ในตาราง InputIconMap จะสลับเป็นรูปของอุปกรณ์ปัจจุบันให้เอง
//
//  • ครอบคลุมไอคอนทุกอัน: station, แมวที่ spawn มา, ฟองเหนือเบาะ ฯลฯ โดยไม่ต้องแปะ Component ทีละตัว
//  • ต้องมีไฟล์  Assets/Resources/InputIconMap.asset  (ไม่มีไฟล์ = ไม่ทำอะไรเลย)
//  • สร้างตัวเองตอนเริ่มเกม ไม่ต้องวางใน Scene
//  • วัตถุที่แปะ InputIconSwitcher อยู่แล้วจะถูกข้าม (ให้ตัวนั้นจัดการเอง)
//  • ข้อจำกัด: ไอคอนที่ Animator คีย์รูป (Sprite) เองทุกเฟรม จะถูก Animator ตั้งกลับ → ให้ใช้ InputIconSwitcher
//    แบบแยกลูกคีย์บอร์ด/จอยแทน
// =====================================================================
public class InputIconAutoSwap : MonoBehaviour
{
    [Tooltip("สแกนทุกกี่วินาที (ไอคอนที่เพิ่งเกิดจะเป็นรูปเก่าได้ไม่เกินช่วงนี้)")]
    public float scanInterval = 0.1f;

    private static InputIconAutoSwap instance;
    private InputIconMap map;
    private float nextScan;
    private bool dirty = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null) return;

        InputIconMap m = Resources.Load<InputIconMap>("InputIconMap");
        if (m == null) return;   // ยังไม่ได้สร้างตาราง → ไม่ทำอะไร

        GameObject go = new GameObject("InputIconAutoSwap");
        go.AddComponent<InputIconAutoSwap>().map = m;
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        if (transform.parent == null) DontDestroyOnLoad(gameObject);
    }

    void OnEnable() { InputDeviceMonitor.Changed += MarkDirty; }
    void OnDisable() { InputDeviceMonitor.Changed -= MarkDirty; }
    void OnDestroy() { if (instance == this) instance = null; }

    void MarkDirty() { dirty = true; }

    void LateUpdate()
    {
        if (map == null) return;
        if (!dirty && Time.unscaledTime < nextScan) return;

        dirty = false;
        nextScan = Time.unscaledTime + scanInterval;
        Scan();
    }

    void Scan()
    {
        InputScheme scheme = InputDeviceMonitor.Current;
        PadStyle style = InputDeviceMonitor.Style;

        foreach (SpriteRenderer r in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            Sprite t = map.Resolve(r.sprite, scheme, style);
            if (t != null && r.GetComponent<InputIconSwitcher>() == null) r.sprite = t;
        }

        foreach (Image img in FindObjectsByType<Image>(FindObjectsSortMode.None))
        {
            Sprite t = map.Resolve(img.sprite, scheme, style);
            if (t != null && img.GetComponent<InputIconSwitcher>() == null) img.sprite = t;
        }
    }
}
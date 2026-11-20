using TMPro;
using UnityEngine;

// =====================================================================
// CafeRatingHUD — โชว์ "Rating" ของร้านบน HUD (อย่างเดียว ไม่มีรายละเอียด)
//
//  วางบน GameObject ใน HUD (Canvas ที่เปิดอยู่ตลอดวัน — ห้ามอยู่ใน Summary Canvas)
//  แล้วลาก TMP เข้าช่อง Rating Text
//  อัปเดตเฉพาะตอนค่า Rating เปลี่ยนจริง (จบวัน / ขึ้นวันใหม่) ไม่ได้ขยับตามแมวทีละตัว
//
//  รายละเอียด (Served / Missed / Interact) อยู่ที่หน้าสรุปวัน → UISummaryController
// =====================================================================
public class CafeRatingHUD : MonoBehaviour
{
    public TextMeshProUGUI ratingText;

    [Tooltip("{0} = Rating ปัจจุบัน, {1} = Rating สูงสุด")]
    public string ratingFormat = "Rating {0:0.0} / {1:0}";

    private bool _subscribed;

    void OnEnable()
    {
        TrySubscribe();
        Refresh();
    }

    void Start()
    {
        // เผื่อ CafeRatingManager ยังไม่พร้อมตอน OnEnable (ลำดับ Awake/OnEnable ของ Scene)
        TrySubscribe();
        Refresh();
    }

    void OnDisable()
    {
        if (_subscribed && CafeRatingManager.Instance != null)
            CafeRatingManager.Instance.OnRatingValueChanged -= Refresh;
        _subscribed = false;
    }

    void TrySubscribe()
    {
        if (_subscribed || CafeRatingManager.Instance == null) return;
        CafeRatingManager.Instance.OnRatingValueChanged += Refresh;
        _subscribed = true;
    }

    public void Refresh()
    {
        CafeRatingManager m = CafeRatingManager.Instance;
        if (m == null || ratingText == null) return;

        ratingText.text = string.Format(ratingFormat, m.Rating, m.maxRating);
    }
}
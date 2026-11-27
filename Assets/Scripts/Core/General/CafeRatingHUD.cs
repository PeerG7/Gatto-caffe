using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// =====================================================================
// CafeRatingHUD — โชว์ Rating ของร้านบน HUD เป็นดาว
//
//  วางบน GameObject ใน HUD (Canvas ที่เปิดอยู่ตลอดวัน — ห้ามอยู่ใน Summary Canvas)
//  - แบบรูปดาว: ลาก Image ดาวเรียงซ้าย→ขวา ใส่ช่อง Star Images
//  - แบบข้อความ (ไม่บังคับ): ลาก TMP ใส่ช่อง Rating Text
//
//  อัปเดตเฉพาะตอนค่า Rating เปลี่ยนจริง (จบวัน / ขึ้นวันใหม่)
//  รายละเอียด (Served / Missed / Interact) อยู่ที่หน้าสรุปวัน → UISummaryController
// =====================================================================
public class CafeRatingHUD : MonoBehaviour
{
    [Header("แบบรูปดาว (แนะนำ)")]
    [Tooltip("ลาก Image ดาวเรียงจากซ้ายไปขวา — โค้ดจะตั้ง Image Type = Filled (Horizontal, Left) ให้เอง")]
    public Image[] starImages;

    [Tooltip("ดาวสีเทา/ว่างด้านหลังแต่ละดวง (ไม่บังคับ)")]
    public Image[] starBackgrounds;

    [Header("แบบข้อความ (ถ้าไม่ใช้รูป)")]
    public TextMeshProUGUI ratingText;
    public string filledStar = "★";
    public string emptyStar = "☆";
    public bool showNumberToo = false;

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
        if (m == null) return;

        float rating = m.Rating;
        int max = Mathf.RoundToInt(m.maxRating);

        // --- แบบรูปดาว: ดวงที่ i เต็มตาม (rating - i) รองรับครึ่งดาว/เศษดาว ---
        if (starImages != null)
        {
            for (int i = 0; i < starImages.Length; i++)
            {
                Image img = starImages[i];
                if (img == null) continue;

                // บังคับเป็น Filled เสมอ (ถ้าเป็น Simple ดาวจะเต็มทุกดวง)
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Horizontal;
                img.fillOrigin = (int)Image.OriginHorizontal.Left;

                img.fillAmount = Mathf.Clamp01(rating - i);
                img.gameObject.SetActive(i < max);

                if (starBackgrounds != null && i < starBackgrounds.Length && starBackgrounds[i] != null)
                    starBackgrounds[i].gameObject.SetActive(i < max);
            }
        }

        // --- แบบข้อความ ---
        if (ratingText != null)
        {
            ratingText.text = ToStarString(rating, max, filledStar, emptyStar)
                            + (showNumberToo ? $"  {rating:0.0}" : "");
        }
    }

    // เรียกใช้จากที่อื่นได้ เช่น หน้าสรุปวัน (UISummaryController)
    public static string ToStarString(float rating, int max, string filled = "★", string empty = "☆")
    {
        int full = Mathf.Clamp(Mathf.RoundToInt(rating), 0, max); // ปัดเป็นดาวเต็ม
        var sb = new StringBuilder();
        for (int i = 0; i < full; i++) sb.Append(filled);
        for (int i = full; i < max; i++) sb.Append(empty);
        return sb.ToString();
    }
}
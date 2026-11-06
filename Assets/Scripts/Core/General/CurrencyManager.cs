using UnityEngine;
using TMPro;
using System.Collections;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance;

    [Header("Economy Settings")]
    public int currentMoney = 0;

    [Header("UI References")]
    public TextMeshProUGUI moneyText;

    [Header("Effects")]
    [SerializeField] private CoinFlipAnimator coinFlipAnimator;
    [SerializeField] private CoinPunchAnimator moneyTextPunchAnimator;
    [SerializeField] private MoneyGainPopup moneyGainPopup;

    // ตัวแปรสำหรับควบคุมการวิ่งของตัวเลข
    private Coroutine rollMoneyCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        UpdateCurrencyUIInstant(currentMoney);
    }

    public void AddMoney(int amount)
    {
        int oldMoney = currentMoney;
        currentMoney += amount;

        // สั่งให้ตัวเลขวิ่งจากยอดเก่าไปยอดใหม่พร้อมเอฟเฟกต์ดีดตัว
        AnimateMoneyChange(oldMoney, currentMoney);

        // Trigger coin flip
        if (coinFlipAnimator != null)
        {
            coinFlipAnimator.PlayCoinFlip();
        }

        // Trigger text punch
        if (moneyTextPunchAnimator != null)
        {
            moneyTextPunchAnimator.PlayCoinPunch();
        }

        // Trigger floating popup
        if (moneyGainPopup != null)
        {
            moneyGainPopup.ShowGain(amount);
        }
    }

    public bool TrySpendMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            int oldMoney = currentMoney;
            currentMoney -= amount;

            // สั่งให้ตัวเลขวิ่งตอนเสียเงินด้วย
            AnimateMoneyChange(oldMoney, currentMoney);
            return true;
        }
        return false;
    }

    // จัดการระบบ Coroutine ตัวเลขวิ่ง
    void AnimateMoneyChange(int startValue, int endValue)
    {
        if (rollMoneyCoroutine != null)
        {
            StopCoroutine(rollMoneyCoroutine);
        }
        rollMoneyCoroutine = StartCoroutine(RollMoneyRoutine(startValue, endValue));
    }

    IEnumerator RollMoneyRoutine(int startValue, int endValue)
    {
        float duration = 1f; // ระยะเวลาในการวิ่งรวม (ปรับเพิ่ม-ลดความเร็วตรงนี้ได้ครับ)
        float elapsed = 0f;

        // เก็บขนาดสเกลเดิมของ Text ไว้เป็นฐาน
        Vector3 originalScale = moneyText != null ? moneyText.transform.localScale : Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 1. คำนวณตัวเลขค่อยๆ ไล่ระดับจากค่าเก่าไปค่าใหม่
            int currentDisplay = Mathf.RoundToInt(Mathf.Lerp(startValue, endValue, t));

            if (moneyText != null)
            {
                moneyText.text = "$" + currentDisplay.ToString();

                // 2. จังหวะดีดตัวเด้งดึ๋ง (Pop/Bounce) ทุกครั้งที่ตัวเลขกำลังรัน
                float punch = Mathf.Sin(t * Mathf.PI * 6f) * 0.15f * (1f - t);
                moneyText.transform.localScale = originalScale * (1f + punch);
            }

            yield return null;
        }

        // ล็อคค่าและขนาดให้กลับมาปกติเป๊ะๆ ตอนจบอนิเมชัน
        if (moneyText != null)
        {
            moneyText.text = "$" + endValue.ToString();
            moneyText.transform.localScale = originalScale;
        }
    }

    // อัปเดตแบบทันทีตอนเริ่มเกม
    void UpdateCurrencyUIInstant(int value)
    {
        if (moneyText != null)
        {
            moneyText.text = "$" + value.ToString();
        }
    }

    // ฟังก์ชันเดิมเผื่อที่อื่นเรียกใช้
    void UpdateCurrencyUI()
    {
        UpdateCurrencyUIInstant(currentMoney);
    }
}
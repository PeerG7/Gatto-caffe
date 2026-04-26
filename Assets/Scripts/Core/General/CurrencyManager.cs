using UnityEngine;
using TMPro; // อย่าลืมใช้ TextMeshPro สำหรับ UI

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance;

    [Header("Economy Settings")]
    public int currentMoney = 0;

    [Header("UI References")]
    public TextMeshProUGUI moneyText; // ลาก Text ที่แสดงเลขเงินมาใส่ใน Inspector

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateCurrencyUI();
    }

    // ฟังก์ชันที่ CustomerTable.cs จะเรียกใช้
    public void AddMoney(int amount)
    {
        currentMoney += amount;
        UpdateCurrencyUI();
    }

    // ฟังก์ชันที่ FurnitureObject.cs จะเรียกใช้เพื่อหักเงิน
    public bool TrySpendMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            UpdateCurrencyUI();
            return true;
        }
        return false;
    }

    // อัปเดตตัวเลขบนหน้าจอ
    void UpdateCurrencyUI()
    {
        if (moneyText != null)
        {
            moneyText.text = "$" + currentMoney.ToString();
        }
    }
}
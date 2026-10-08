using UnityEngine;
using TMPro;

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
        UpdateCurrencyUI();
    }

    public void AddMoney(int amount)
    {
        currentMoney += amount;
        UpdateCurrencyUI();

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
            currentMoney -= amount;
            UpdateCurrencyUI();
            return true;
        }
        return false;
    }

    void UpdateCurrencyUI()
    {
        if (moneyText != null)
        {
            moneyText.text = "$" + currentMoney.ToString();
        }
    }
}
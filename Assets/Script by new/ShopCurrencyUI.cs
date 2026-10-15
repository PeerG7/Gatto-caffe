using UnityEngine;
using TMPro;

public class ShopCurrencyUI : MonoBehaviour
{
    private TextMeshProUGUI shopMoneyText;

    void Awake()
    {
        shopMoneyText = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        UpdateShopMoney();
    }

    void Update()
    {
        UpdateShopMoney();
    }

    public void UpdateShopMoney()
    {
        if (CurrencyManager.Instance != null && shopMoneyText != null)
        {
            shopMoneyText.text = "$" + CurrencyManager.Instance.currentMoney.ToString();
        }
    }
}
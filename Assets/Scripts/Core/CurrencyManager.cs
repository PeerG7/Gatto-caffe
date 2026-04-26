using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance;
    public int currentMoney = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddMoney(int amount)
    {
        currentMoney += amount;
        Debug.Log($"[Economy] Earned: {amount}. Total: {currentMoney}");
    }

    public bool TrySpendMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            Debug.Log($"[Economy] Spent: {amount}. Remaining: {currentMoney}");
            return true;
        }
        Debug.Log("[Economy] Not enough money!");
        return false;
    }
}
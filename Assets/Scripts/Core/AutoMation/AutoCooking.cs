using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class AutoCooking : MonoBehaviour
{
    [Header("Shop & Price Settings")]
    public int botPrice = 800;

    [Header("Automation Settings")]
    public bool isActivated = false;
    public float checkInterval = 1.0f;
    private float timer = 0f;

    [Header("Cooking Animation")]
    [Tooltip("จุดที่ไอคอนอาหารเกิด (ถ้าไม่ใส่จะใช้ตำแหน่งของ Bot)")]
    public Transform cookPoint;
    [Tooltip("เวลาที่ไอคอนค่อยๆ ปรากฏ (วินาที)")]
    public float cookDuration = 1.2f;
    [Tooltip("เวลาที่ใช้โยนอาหารไปที่ Tray (วินาที)")]
    public float tossDuration = 0.6f;
    [Tooltip("ความสูงของวงโค้ง (หน่วยเดียวกับโลก ถ้าเป็น UI ให้ใส่เป็นพิกเซล เช่น 150)")]
    public float arcHeight = 1.5f;
    [Tooltip("ขนาดไอคอนตอนโตเต็มที่")]
    public float iconScale = 1f;
    [Tooltip("จำนวนรอบที่หมุนระหว่างโยน (0 = ไม่หมุน)")]
    public float spinTurns = 1f;
    [Tooltip("Sorting Order ของไอคอน (กรณีเป็น World Sprite)")]
    public int sortingOrder = 50;

    private bool isCooking = false;
    private GameObject currentIcon;

    void Start()
    {
        UpdateVisualState();
    }

    void OnDisable()
    {
        // กันไอคอนค้างถ้าบอทถูกปิดกลางทาง
        if (currentIcon != null) Destroy(currentIcon);
        currentIcon = null;
        isCooking = false;
    }

    void Update()
    {
        if (!isActivated) return;
        if (isCooking) return; // กำลังทำจานอยู่ ไม่เริ่มจานใหม่ซ้อน

        bool isPaused = DayNightManager.Instance != null && DayNightManager.Instance.isPaused;
        bool isNotWork = DayNightManager.Instance != null && !DayNightManager.Instance.isWorkTime;
        if (isPaused || isNotWork) return;

        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            TryAutoCookExactDemand();
        }
    }

    void TryAutoCookExactDemand()
    {
        if (TrayManager.instance == null || !TrayManager.instance.HasEmptySlot()) return;

        CustomerTable[] allTables = FindObjectsOfType<CustomerTable>();
        if (allTables == null || allTables.Length == 0) return;

        List<RecipeSO> waitingOrders = new List<RecipeSO>();
        foreach (var table in allTables)
        {
            if (table != null && table.sittingNPCs != null)
            {
                foreach (var npc in table.sittingNPCs)
                {
                    if (npc != null && !npc.hasBeenServed && npc.requestedRecipe != null)
                        waitingOrders.Add(npc.requestedRecipe);
                }
            }
        }
        if (waitingOrders.Count == 0) return;

        int occupiedTrayCount = 0;
        foreach (var slot in TrayManager.instance.traySlots)
        {
            if (slot != null && slot.HasFood()) occupiedTrayCount++;
        }

        if (waitingOrders.Count > occupiedTrayCount)
        {
            RecipeSO recipeToCook = waitingOrders[0];
            if (recipeToCook != null)
                StartCoroutine(CookAndTossRoutine(recipeToCook));
        }
    }

    // ---------------------------------------------------------------
    //  Animation
    // ---------------------------------------------------------------
    IEnumerator CookAndTossRoutine(RecipeSO recipe)
    {
        isCooking = true;

        Transform origin = cookPoint != null ? cookPoint : transform;

        // หาช่องว่างเป้าหมายตั้งแต่ตอนเริ่มโยน (ค่อยหาอีกทีตอนโยนจริง)
        Transform firstTarget = FindEmptySlotTransform();
        bool uiMode = firstTarget != null && firstTarget is RectTransform;

        currentIcon = CreateIcon(recipe.finalDishSprite, uiMode, origin);
        Transform icon = currentIcon.transform;
        Graphic uiGraphic = currentIcon.GetComponent<Graphic>();
        SpriteRenderer sr = currentIcon.GetComponent<SpriteRenderer>();

        Vector3 startPos = GetStartPosition(origin, uiMode);
        icon.position = startPos;
        icon.localScale = Vector3.zero;
        SetAlpha(sr, uiGraphic, 0f);

        // ---------- 1) ค่อยๆ สร้างอาหารที่ตัวบอท ----------
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, cookDuration);
            float k = Mathf.Clamp01(t);

            float scale = EaseOutBack(k) * iconScale;
            // เด้งเบาๆ ตอนกำลังทำ ให้ดูมีชีวิต
            float wobble = 1f + Mathf.Sin(k * Mathf.PI * 6f) * 0.04f * (1f - k);
            icon.localScale = Vector3.one * scale * wobble;
            SetAlpha(sr, uiGraphic, Mathf.Clamp01(k * 2f));

            // ลอยขึ้นเล็กน้อย
            icon.position = startPos + Vector3.up * (arcHeight * 0.1f * k);
            yield return null;
        }
        icon.localScale = Vector3.one * iconScale;
        SetAlpha(sr, uiGraphic, 1f);

        // ---------- 2) หาช่อง Tray ที่ว่าง ณ ตอนนี้ ----------
        Transform target = FindEmptySlotTransform();
        if (target == null)
        {
            // Tray เต็มระหว่างทำ -> ยกเลิกจานนี้
            Destroy(currentIcon);
            currentIcon = null;
            isCooking = false;
            yield break;
        }

        // ---------- 3) โยนเป็นวงโค้งไปที่ Tray ----------
        Vector3 from = icon.position;
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, tossDuration);
            float k = Mathf.Clamp01(t);
            float e = k * k * (3f - 2f * k); // smoothstep

            // เป้าหมายอาจขยับได้ (เช่น UI) จึงอ่านตำแหน่งใหม่ทุกเฟรม
            Vector3 to = target.position;
            Vector3 pos = Vector3.Lerp(from, to, e);
            pos += Vector3.up * Mathf.Sin(k * Mathf.PI) * arcHeight;
            icon.position = pos;

            // หมุน + ย่อลงนิดหน่อยตอนใกล้ถึง
            icon.rotation = Quaternion.Euler(0f, 0f, -360f * spinTurns * e);
            float s = Mathf.Lerp(iconScale, iconScale * 0.85f, e);
            icon.localScale = Vector3.one * s;
            yield return null;
        }

        // ---------- 4) ถึงแล้ว ส่งเข้า Tray จริง ----------
        Destroy(currentIcon);
        currentIcon = null;

        bool success = TrayManager.instance != null &&
                       TrayManager.instance.ReceiveFood(recipe.recipeName, recipe.finalDishSprite);
        if (success)
            Debug.Log($"[AutoCooking Bot] 🍳 ทำเมนู [{recipe.recipeName}] สำเร็จ และโยนลง Tray แล้ว");

        isCooking = false;
    }

    Transform FindEmptySlotTransform()
    {
        if (TrayManager.instance == null) return null;
        foreach (var slot in TrayManager.instance.traySlots)
        {
            if (slot != null && !slot.HasFood())
                return slot.transform;
        }
        return null;
    }

    GameObject CreateIcon(Sprite sprite, bool uiMode, Transform origin)
    {
        GameObject go = new GameObject("CookingIcon");

        if (uiMode)
        {
            // วางไว้ใต้ Canvas เดียวกับ Tray
            Transform slotT = FindEmptySlotTransform();
            Canvas canvas = slotT != null ? slotT.GetComponentInParent<Canvas>() : null;
            Canvas root = canvas != null ? canvas.rootCanvas : null;
            if (root != null) go.transform.SetParent(root.transform, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100f, 100f);
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            img.preserveAspect = true;
            go.transform.SetAsLastSibling(); // อยู่บนสุด
        }
        else
        {
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
        }
        return go;
    }

    Vector3 GetStartPosition(Transform origin, bool uiMode)
    {
        if (!uiMode || origin is RectTransform) return origin.position;

        // บอทเป็น World object แต่ Tray เป็น UI -> แปลงเป็นพิกัดหน้าจอ
        Camera cam = Camera.main;
        return cam != null ? cam.WorldToScreenPoint(origin.position) : origin.position;
    }

    void SetAlpha(SpriteRenderer sr, Graphic g, float a)
    {
        if (sr != null) { Color c = sr.color; c.a = a; sr.color = c; }
        if (g != null) { Color c = g.color; c.a = a; g.color = c; }
    }

    float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    // ---------------------------------------------------------------
    //  Shop
    // ---------------------------------------------------------------
    public void TryBuyAutoCookBot()
    {
        if (isActivated) return;

        if (CurrencyManager.Instance != null)
        {
            if (CurrencyManager.Instance.TrySpendMoney(botPrice))
            {
                ActivateBot();
                if (UINotificationManager.Instance != null)
                    UINotificationManager.Instance.ShowNotification("Buy AutoCooking Success");
            }
            else
            {
                if (UINotificationManager.Instance != null)
                    UINotificationManager.Instance.ShowNotification("เงินไม่พอ! ต้องการ " + botPrice + " $");
            }
        }
    }

    public void ActivateBot()
    {
        isActivated = true;
        UpdateVisualState();
        Debug.Log("[AutoCooking Bot] 🍳 เปิดใช้งานบอททำอาหาร AutoCooking แล้ว!");
    }

    void UpdateVisualState()
    {
        gameObject.SetActive(isActivated);
    }
}
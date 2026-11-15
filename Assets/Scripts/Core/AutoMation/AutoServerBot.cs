using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

// =====================================================================
// AutoServerBot (Drone Edition) — โดรนส่งอาหารอัตโนมัติ (ไม่ใช้ NavMesh แล้ว)
//
// การทำงาน:
//   1. ทุก checkInterval วินาที หาโต๊ะที่มีแมวรออาหารอยู่ และมีอาหารใน Tray ที่ตรงกับที่แมวสั่ง
//   2. โดรนโผล่มาเหนือโต๊ะ (Fade In) แล้วเลื่อนแกน Y ลงมาจอดลอยเหนือโต๊ะ
//   3. เสิร์ฟอาหาร (หยิบจาก Tray ตอนเสิร์ฟสำเร็จ — ถ้าแมวหายไป/ผู้เล่นเอาอาหารไปก่อน จะไม่เสียอาหาร)
//   4. บินกลับขึ้นไปพร้อม Fade Out แล้วซ่อนตัว รอรอบต่อไป
//
// Setup:
//   - วางสคริปต์นี้บน GameObject ของโดรน แล้วใส่รูปโดรนเป็น SpriteRenderer (บนตัวเองหรือ Child ก็ได้)
//   - (ไม่บังคับ) ลาก SpriteRenderer รูปอาหารที่เป็น Child ของโดรนมาใส่ช่อง Food Renderer
//   - ควรลบ NavMeshAgent และ PlayerInventory ที่เคยติดอยู่บนบอทตัวนี้ออก (ไม่ใช้แล้ว)
//   - API เดิม (botPrice, isActivated, TryBuyAutoServerBot, ActivateBot) ยังใช้งานได้เหมือนเดิม
// =====================================================================
public class AutoServerBot : MonoBehaviour
{
    [Header("Shop & Price Settings")]
    public int botPrice = 800;
    public bool isActivated = false;

    [Header("Bot Settings")]
    [Tooltip("ทุกกี่วินาทีที่โดรนจะมองหาอาหารใน Tray ที่ตรงกับโต๊ะ")]
    public float checkInterval = 1.0f;
    [Tooltip("เวลาพักหลังส่งเสร็จ ก่อนรับงานถัดไป")]
    public float cooldownBetweenDeliveries = 0.5f;

    [Header("Drone Visual")]
    [Tooltip("(ไม่บังคับ) SpriteRenderer รูปอาหารที่โดรนหิ้วมา — ต้องเป็น Child ของ GameObject นี้ / ไม่ใส่ = ไม่แสดงรูปอาหาร")]
    public SpriteRenderer foodRenderer;
    [Tooltip("จุดที่โดรนลอยค้างตอนเสิร์ฟ เทียบกับตำแหน่งโต๊ะ (X, Y)")]
    public Vector2 hoverOffset = new Vector2(0f, 0.6f);
    [Tooltip("ระยะที่โดรนเริ่มบินลงมา/บินกลับขึ้นไป (แกน Y)")]
    public float flyHeight = 3f;
    [Tooltip("เวลาบินลงมา + Fade In (วินาที)")]
    public float flyInDuration = 0.8f;
    [Tooltip("เวลาลอยค้างก่อนเสิร์ฟ (วินาที)")]
    public float serveDelay = 0.4f;
    [Tooltip("เวลาค้างหลังเสิร์ฟ ก่อนบินกลับ (วินาที)")]
    public float leaveDelay = 0.25f;
    [Tooltip("เวลาบินกลับขึ้นไป + Fade Out (วินาที)")]
    public float flyOutDuration = 0.7f;
    [Tooltip("ความสูง/ความเร็วของการโยกขึ้นลงตอนลอยค้าง")]
    public float bobHeight = 0.05f;
    public float bobSpeed = 6f;

    [Header("Sorting (กันโดรนไปอยู่หลังโต๊ะ/ตัวแมว)")]
    [Tooltip("เปิด = บวก Order in Layer ให้ทุก SpriteRenderer ของโดรนเพื่อให้อยู่หน้าสุด")]
    public bool forceOnTop = true;
    [Tooltip("ไม่ว่าง = เปลี่ยน Sorting Layer ของโดรนเป็นชื่อนี้ / เว้นว่าง = ใช้ Layer เดิม")]
    public string sortingLayerName = "";
    public int sortingOrderOffset = 500;

    private readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
    private readonly List<Color> baseColors = new List<Color>();
    private bool foodShown = false;
    private float homeZ;
    private Vector3 lastTablePos;

    private static bool IsPaused =>
        DayNightManager.Instance != null && DayNightManager.Instance.isPaused;

    private bool CanWorkNow =>
        isActivated && !IsPaused &&
        (DayNightManager.Instance == null || DayNightManager.Instance.isWorkTime);

    // ------------------------------------------------------------------
    //  Lifecycle
    // ------------------------------------------------------------------
    void Awake()
    {
        homeZ = transform.position.z;

        // ไม่ใช้ NavMesh แล้ว — ถ้ายังติด Agent อยู่ ต้องปิด ไม่งั้นมันจะดึงตำแหน่งโดรนกลับ
        NavMeshAgent oldAgent = GetComponent<NavMeshAgent>();
        if (oldAgent != null)
        {
            oldAgent.enabled = false;
            Debug.LogWarning("[AutoServerBot] พบ NavMeshAgent บนโดรน — ปิดให้อัตโนมัติแล้ว แนะนำให้ลบ Component นี้ออก", this);
        }

        if (GetComponent<PlayerInventory>() != null)
        {
            Debug.LogWarning("[AutoServerBot] พบ PlayerInventory บนโดรน — ไม่ได้ใช้แล้ว และอาจทำให้สคริปต์อื่นที่ FindObjectOfType<PlayerInventory>() หยิบผิดตัว แนะนำให้ลบออก", this);
        }

        CollectRenderers();
    }

    void OnEnable()
    {
        HideDrone();
        StartCoroutine(BrainLoop());
    }

    void OnDisable()
    {
        HideDrone();
    }

    void CollectRenderers()
    {
        renderers.Clear();
        baseColors.Clear();

        renderers.AddRange(GetComponentsInChildren<SpriteRenderer>(true));
        if (foodRenderer != null && !renderers.Contains(foodRenderer))
            renderers.Add(foodRenderer);

        foreach (SpriteRenderer r in renderers)
        {
            baseColors.Add(r.color);

            if (!forceOnTop) continue;
            if (!string.IsNullOrEmpty(sortingLayerName)) r.sortingLayerName = sortingLayerName;
            r.sortingOrder += sortingOrderOffset; // บวกเพิ่ม เพื่อคงลำดับซ้อนกันภายในตัวโดรนเดิมไว้
        }
    }

    // ------------------------------------------------------------------
    //  Brain: หางาน -> ส่งของ -> พัก
    // ------------------------------------------------------------------
    IEnumerator BrainLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Mathf.Max(0.1f, checkInterval));

            if (!CanWorkNow) continue;
            if (!TryFindJob(out CustomerTable table, out TraySlot slot)) continue;

            yield return StartCoroutine(DeliverRoutine(table, slot));

            if (cooldownBetweenDeliveries > 0f)
                yield return new WaitForSeconds(cooldownBetweenDeliveries);
        }
    }

    // หาโต๊ะที่มีแมวรออาหาร + มีอาหารใน Tray ตรงกับที่แมวสั่ง (ใช้เงื่อนไขเดียวกับตอนเสิร์ฟจริง)
    bool TryFindJob(out CustomerTable foundTable, out TraySlot foundSlot)
    {
        foundTable = null;
        foundSlot = null;

        if (TrayManager.instance == null || TrayManager.instance.traySlots == null) return false;

        CustomerTable[] allTables = FindObjectsByType<CustomerTable>(FindObjectsSortMode.None);
        foreach (CustomerTable table in allTables)
        {
            if (table == null || !table.HasAnyNPCWaiting()) continue;

            foreach (TraySlot slot in TrayManager.instance.traySlots)
            {
                if (slot == null || !slot.HasFood()) continue;

                if (table.CanServeItem(slot.GetFoodItemName()))
                {
                    foundTable = table;
                    foundSlot = slot;
                    return true;
                }
            }
        }
        return false;
    }

    // ------------------------------------------------------------------
    //  Delivery: Fade In + บินลง -> เสิร์ฟ -> บินขึ้น + Fade Out
    // ------------------------------------------------------------------
    IEnumerator DeliverRoutine(CustomerTable table, TraySlot slot)
    {
        string itemName = slot.GetFoodItemName();
        Sprite itemSprite = slot.GetFoodSprite();

        lastTablePos = table.transform.position;
        SetFood(itemSprite);

        // เริ่มที่ด้านบนเหนือจุดลอย แบบโปร่งใส
        Vector3 hoverStart = HoverPoint(table);
        SetPosition(hoverStart + Vector3.up * flyHeight);
        SetAlpha(0f);
        SetVisible(true);

        // 1) บินลงมา (แกน Y) พร้อม Fade In
        yield return Tween(flyInDuration, t =>
        {
            Vector3 target = HoverPoint(table);
            SetPosition(Vector3.LerpUnclamped(target + Vector3.up * flyHeight, target, EaseOutCubic(t)));
            SetAlpha(Mathf.SmoothStep(0f, 1f, t));
        });

        // 2) ลอยค้างโยกเบา ๆ
        float hoverTime = Mathf.Max(0.01f, serveDelay);
        yield return Tween(hoverTime, t =>
        {
            Vector3 p = HoverPoint(table);
            p.y += Mathf.Sin(t * hoverTime * bobSpeed) * bobHeight;
            SetPosition(p);
        });

        // 3) เสิร์ฟ — เช็กอีกรอบก่อน เผื่อแมวหาย/ผู้เล่นหยิบอาหารไปก่อน (ถ้าไม่ผ่านก็ไม่เสียอาหาร)
        bool served = false;
        if (table != null && slot != null && slot.HasFood() &&
            NameEquals(slot.GetFoodItemName(), itemName) && table.HasAnyNPCWaiting())
        {
            if (table.TryServeFoodDirect(itemName))
            {
                slot.ClearSlot();
                served = true;
            }
        }

        SetFood(null);
        Debug.Log(served
            ? $"[AutoServerBot] โดรนเสิร์ฟ '{itemName}' สำเร็จ"
            : $"[AutoServerBot] โดรนเสิร์ฟ '{itemName}' ไม่สำเร็จ (แมวออกไปแล้ว/อาหารถูกเอาไปก่อน) — บินกลับโดยไม่เสียอาหาร");

        // 4) ค้างนิดหนึ่งก่อนบินกลับ
        if (leaveDelay > 0f)
            yield return Tween(leaveDelay, t => SetPosition(HoverPoint(table)));

        // 5) บินกลับขึ้นไปพร้อม Fade Out
        yield return Tween(flyOutDuration, t =>
        {
            Vector3 from = HoverPoint(table);
            SetPosition(Vector3.LerpUnclamped(from, from + Vector3.up * flyHeight, EaseInCubic(t)));
            SetAlpha(1f - Mathf.SmoothStep(0f, 1f, t));
        });

        HideDrone();
    }

    // จุดลอยของโดรนเหนือโต๊ะ (ถ้าโต๊ะถูกเก็บระหว่างทาง ใช้ตำแหน่งสุดท้ายที่รู้)
    Vector3 HoverPoint(CustomerTable table)
    {
        if (table != null) lastTablePos = table.transform.position;
        return new Vector3(lastTablePos.x + hoverOffset.x, lastTablePos.y + hoverOffset.y, homeZ);
    }

    // Tween แบบหยุดนิ่งตอนเกมพอส (step รับค่า t 0..1)
    IEnumerator Tween(float duration, System.Action<float> step)
    {
        duration = Mathf.Max(0.01f, duration);
        float t = 0f;
        while (t < 1f)
        {
            if (IsPaused) { yield return null; continue; }

            t = Mathf.Min(1f, t + Time.deltaTime / duration);
            step(t);
            yield return null;
        }
    }

    static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    static float EaseInCubic(float t) => t * t * t;

    static bool NameEquals(string a, string b)
    {
        if (a == null || b == null) return false;
        return a.Trim().Equals(b.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }

    // ------------------------------------------------------------------
    //  Visual helpers
    // ------------------------------------------------------------------
    void SetPosition(Vector3 pos)
    {
        transform.position = pos;
    }

    void SetAlpha(float a)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] == null) continue;
            Color c = baseColors[i];
            c.a = baseColors[i].a * a;
            renderers[i].color = c;
        }
    }

    void SetVisible(bool visible)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            SpriteRenderer r = renderers[i];
            if (r == null) continue;
            r.enabled = (r == foodRenderer) ? (visible && foodShown) : visible;
        }
    }

    void SetFood(Sprite sprite)
    {
        foodShown = sprite != null;
        if (foodRenderer == null) return;

        foodRenderer.sprite = sprite;
        // ถ้าโดรนกำลังแสดงอยู่ ให้รูปอาหารตามสถานะใหม่ด้วย
        if (!foodShown) foodRenderer.enabled = false;
        else if (IsDroneVisible()) foodRenderer.enabled = true;
    }

    bool IsDroneVisible()
    {
        foreach (SpriteRenderer r in renderers)
            if (r != null && r != foodRenderer) return r.enabled;
        return false;
    }

    void HideDrone()
    {
        foodShown = false;
        if (foodRenderer != null) foodRenderer.sprite = null;
        SetAlpha(0f);
        SetVisible(false);
    }

    // ------------------------------------------------------------------
    //  Shop API (คงของเดิมไว้ — ปุ่มใน UI ไม่ต้องแก้)
    // ------------------------------------------------------------------
    public void TryBuyAutoServerBot()
    {
        if (isActivated) return;

        if (CurrencyManager.Instance != null)
        {
            if (CurrencyManager.Instance.TrySpendMoney(botPrice))
            {
                ActivateBot();
                if (UINotificationManager.Instance != null)
                    UINotificationManager.Instance.ShowNotification("Buy AutoServerBot Success");
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

        // เผื่อใน Scene ปิด GameObject นี้ไว้ตั้งแต่แรก (แบบบอทเก่า) — เปิดเพื่อให้ลูปทำงาน ตัวโดรนจะยังซ่อนอยู่จนกว่าจะมีงาน
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        Debug.Log("[AutoServerBot] เปิดใช้งานโดรนเสิร์ฟอาหารแล้ว!");
    }
}
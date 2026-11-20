using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

// =====================================================================
// AutoServerBot (Drone Edition) — โดรนส่งอาหารอัตโนมัติ (ไม่ใช้ NavMesh แล้ว)
//
// การทำงาน:
//   1. ทุก checkInterval วินาที หาโต๊ะที่มีแมวรออาหารอยู่ และมีอาหารใน Tray ที่ตรงกับที่แมวสั่ง
//   2. โดรนโผล่เหนือถาด (Fade In) บินลงไปที่ถาด หยิบอาหาร แล้วลอยขึ้นพร้อมอาหารและหายไป
//   3. โดรนโผล่เหนือโต๊ะที่จะเสิร์ฟ (Fade In) แล้วบินลงมาพร้อมอาหาร
//   4. เสิร์ฟอาหาร
//   5. บินกลับขึ้นไปพร้อม Fade Out แล้วซ่อนตัว รอรอบต่อไป
//
// Setup:
//   - วางสคริปต์นี้บน GameObject ของโดรน แล้วใส่รูปโดรนเป็น SpriteRenderer (บนตัวเองหรือ Child ก็ได้)
//   - (ไม่บังคับ) ลาก SpriteRenderer รูปอาหารที่เป็น Child ของโดรนมาใส่ช่อง Food Renderer
//     ✅ ใหม่: ถ้าไม่ลาก สคริปต์จะสร้างรูปอาหารให้เองอัตโนมัติ (ห้อยใต้ตัวโดรน)
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
    [Tooltip("(ไม่บังคับ) SpriteRenderer รูปอาหารที่โดรนหิ้วมา — ต้องเป็น Child ของ GameObject นี้ / ไม่ใส่ = สคริปต์สร้างให้เอง")]
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

    [Header("Tray Pickup (รับอาหารจากถาด)")]
    [Tooltip("จุดที่โดรนลอยค้างตอนหยิบอาหาร เทียบกับตำแหน่งช่องถาด (X, Y)")]
    public Vector2 trayHoverOffset = new Vector2(0f, 0.5f);
    [Tooltip("เวลาลอยค้างที่ถาดก่อนหยิบอาหาร (วินาที)")]
    public float pickupDelay = 0.3f;
    [Tooltip("เปิด = อาหารหายจากถาดทันทีที่โดรนหยิบ (ดูเหมือนหยิบจริง) | ปิด = อาหารยังอยู่ในถาดจนกว่าจะเสิร์ฟสำเร็จ (ไม่เสียอาหารถ้าแมวหายไป แต่รูปอาหารจะซ้ำกับที่โดรนถือ)")]
    public bool clearTrayAtPickup = true;

    // ✅ ใหม่: ตั้งค่ารูปอาหารที่สคริปต์สร้างให้เอง ─────────────────
    [Header("Auto Food Sprite (ใหม่)")]
    [Tooltip("ใช้ตอนสคริปต์สร้างรูปอาหารให้เอง (ช่อง Food Renderer ว่าง) — ตำแหน่งเทียบกับตัวโดรน")]
    public Vector2 foodLocalOffset = new Vector2(0f, -0.6f);
    [Tooltip("ขนาดรูปอาหารที่สร้างให้เอง")]
    public float foodScale = 0.6f;

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
    private Vector3 lastTrayPos;

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

        // ✅ ใหม่: ถ้าไม่ได้ลาก Food Renderer ไว้ ให้สร้างรูปอาหารเป็นลูกของโดรนให้เอง (ห้อยใต้ตัวโดรน)
        if (foodRenderer == null)
        {
            SpriteRenderer droneSr = GetComponentInChildren<SpriteRenderer>(true);

            GameObject foodObj = new GameObject("FoodSprite (auto)");
            foodObj.transform.SetParent(transform, false);
            foodObj.transform.localPosition = new Vector3(foodLocalOffset.x, foodLocalOffset.y, 0f);
            foodObj.transform.localScale = Vector3.one * foodScale;

            foodRenderer = foodObj.AddComponent<SpriteRenderer>();
            foodRenderer.enabled = false;

            if (droneSr != null)
            {
                // ให้รูปอาหารอยู่หน้าตัวโดรนเล็กน้อย
                foodRenderer.sortingLayerID = droneSr.sortingLayerID;
                foodRenderer.sortingOrder = droneSr.sortingOrder + 1;
            }
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
    //  Delivery:
    //  บินลงถาด -> หยิบอาหาร -> ลอยขึ้นหาย -> โผล่เหนือโต๊ะ -> บินลง -> เสิร์ฟ -> บินขึ้นหาย
    // ------------------------------------------------------------------
    IEnumerator DeliverRoutine(CustomerTable table, TraySlot slot)
    {
        string itemName = slot.GetFoodItemName();
        Sprite itemSprite = slot.GetFoodSprite();

        lastTablePos = table.transform.position;
        SetFood(null); // ตอนบินไปถาด มือเปล่า

        // ── ช่วงที่ 1: ไปรับอาหารที่ถาด ──────────────────────────────
        System.Func<Vector3> trayPoint = () => TrayPoint(slot);

        SetPosition(trayPoint() + Vector3.up * flyHeight);
        SetAlpha(0f);
        SetVisible(true);

        // 1) บินลงมาที่ถาดพร้อม Fade In
        yield return FlyIn(trayPoint);

        // 2) ลอยค้างที่ถาดแป๊บหนึ่ง
        yield return Hover(trayPoint, pickupDelay);

        // 3) เช็กก่อนหยิบ — เผื่อผู้เล่นหยิบอาหารไปก่อน / แมวหายไปแล้ว
        bool stillValid = slot != null && slot.HasFood() &&
                          NameEquals(slot.GetFoodItemName(), itemName) &&
                          table != null && table.HasAnyNPCWaiting();

        if (!stillValid)
        {
            Debug.Log($"[AutoServerBot] งานของ '{itemName}' ถูกยกเลิกก่อนหยิบ (อาหารถูกเอาไปแล้ว/แมวออกไปแล้ว) — โดรนกลับโดยไม่ได้หยิบ");
            yield return FlyOut(trayPoint);
            HideDrone();
            yield break;
        }

        // 4) หยิบอาหาร: รูปอาหารโผล่ที่โดรน (และอาหารหายจากถาดถ้าเปิด clearTrayAtPickup)
        SetFood(itemSprite);
        // ✅ ใหม่: log ไว้เช็กว่าได้รูปอาหารมาไหม (ลบทิ้งได้เมื่อใช้งานปกติแล้ว)
        Debug.Log($"[AutoServerBot] pickup: sprite={(itemSprite != null ? itemSprite.name : "NULL")}, foodRenderer={(foodRenderer != null ? foodRenderer.name : "NULL")}");
        if (clearTrayAtPickup) slot.ClearSlot();

        // 5) ลอยขึ้นพร้อมอาหาร + Fade Out (หายไป)
        yield return FlyOut(trayPoint);

        // ── ช่วงที่ 2: ไปเสิร์ฟที่โต๊ะ ────────────────────────────────
        System.Func<Vector3> tablePoint = () => HoverPoint(table);

        // 6) "วาร์ป" ไปโผล่เหนือโต๊ะ (ซ่อนก่อนย้ายตำแหน่ง กันกะพริบ) — รูปอาหารยังถืออยู่
        SetVisible(false);
        SetAlpha(0f);
        SetPosition(tablePoint() + Vector3.up * flyHeight);
        SetVisible(true);

        // 7) บินลงมาที่โต๊ะพร้อมอาหาร + Fade In
        yield return FlyIn(tablePoint);

        // 8) ลอยค้างก่อนเสิร์ฟ
        yield return Hover(tablePoint, serveDelay);

        // 9) เสิร์ฟ — เช็กอีกรอบเผื่อแมวหายไปประหว่างทาง
        bool served = false;
        if (table != null && table.HasAnyNPCWaiting() && table.TryServeFoodDirect(itemName))
        {
            served = true;

            // ถ้าไม่ได้เคลียร์ตอนหยิบ ให้เคลียร์ตอนเสิร์ฟสำเร็จ (เหมือนพฤติกรรมเดิม)
            if (!clearTrayAtPickup && slot != null && slot.HasFood() &&
                NameEquals(slot.GetFoodItemName(), itemName))
            {
                slot.ClearSlot();
            }
        }

        SetFood(null);
        Debug.Log(served
            ? $"[AutoServerBot] โดรนเสิร์ฟ '{itemName}' สำเร็จ"
            : (clearTrayAtPickup
                ? $"[AutoServerBot] โดรนเสิร์ฟ '{itemName}' ไม่สำเร็จ (แมวออกไปแล้ว) — อาหารถูกหยิบจากถาดไปแล้วจึงสูญไป"
                : $"[AutoServerBot] โดรนเสิร์ฟ '{itemName}' ไม่สำเร็จ (แมวออกไปแล้ว) — อาหารยังอยู่ในถาด"));

        // 10) ค้างนิดหนึ่งก่อนบินกลับ
        if (leaveDelay > 0f)
            yield return Hover(tablePoint, leaveDelay, bob: false);

        // 11) บินกลับขึ้นไปพร้อม Fade Out
        yield return FlyOut(tablePoint);

        HideDrone();
    }

    // ── ท่าบินพื้นฐาน (ใช้ทั้งที่ถาดและที่โต๊ะ) ──────────────────────

    // บินลงมาจากด้านบนจนถึงจุดหมาย พร้อม Fade In
    IEnumerator FlyIn(System.Func<Vector3> point)
    {
        yield return Tween(flyInDuration, t =>
        {
            Vector3 target = point();
            SetPosition(Vector3.LerpUnclamped(target + Vector3.up * flyHeight, target, EaseOutCubic(t)));
            SetAlpha(Mathf.SmoothStep(0f, 1f, t));
        });
    }

    // ลอยค้างที่จุดหมาย (โยกเบา ๆ ถ้าเปิด bob)
    IEnumerator Hover(System.Func<Vector3> point, float duration, bool bob = true)
    {
        float hoverTime = Mathf.Max(0.01f, duration);
        yield return Tween(hoverTime, t =>
        {
            Vector3 p = point();
            if (bob) p.y += Mathf.Sin(t * hoverTime * bobSpeed) * bobHeight;
            SetPosition(p);
        });
    }

    // ลอยขึ้นไปด้านบน พร้อม Fade Out
    IEnumerator FlyOut(System.Func<Vector3> point)
    {
        yield return Tween(flyOutDuration, t =>
        {
            Vector3 from = point();
            SetPosition(Vector3.LerpUnclamped(from, from + Vector3.up * flyHeight, EaseInCubic(t)));
            SetAlpha(1f - Mathf.SmoothStep(0f, 1f, t));
        });
    }

    // จุดลอยของโดรนเหนือโต๊ะ (ถ้าโต๊ะถูกเก็บระหว่างทาง ใช้ตำแหน่งสุดท้ายที่รู้)
    Vector3 HoverPoint(CustomerTable table)
    {
        if (table != null) lastTablePos = table.transform.position;
        return new Vector3(lastTablePos.x + hoverOffset.x, lastTablePos.y + hoverOffset.y, homeZ);
    }

    // จุดลอยของโดรนเหนือช่องถาด (รองรับถาดที่เป็น UI แบบ Screen Space - Overlay ด้วย)
    Vector3 TrayPoint(TraySlot slot)
    {
        if (slot != null)
        {
            Vector3 p = slot.transform.position;

            Canvas canvas = slot.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // Overlay: transform.position เป็นพิกเซลหน้าจอ → แปลงเป็นพิกัดโลกของกล้องหลัก
                Camera cam = Camera.main;
                if (cam != null)
                    p = cam.ScreenToWorldPoint(new Vector3(p.x, p.y, Mathf.Abs(cam.transform.position.z - homeZ)));
            }

            lastTrayPos = p;
        }

        return new Vector3(lastTrayPos.x + trayHoverOffset.x, lastTrayPos.y + trayHoverOffset.y, homeZ);
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
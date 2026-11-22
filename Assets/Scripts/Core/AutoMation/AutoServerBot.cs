using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class AutoServerBot : MonoBehaviour
{
    [Header("Shop & Price Settings")]
    public int botPrice = 200;
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
    [Tooltip("เปิด = อาหารหายจากถาดทันทีที่โดรนหยิบ (ดูเหมือนหยิบจริง) | ปิด = อาหารยังอยู่ในถาดจนกว่าจะเสิร์ฟสำเร็จ")]
    public bool clearTrayAtPickup = true;

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

    void Awake()
    {
        homeZ = transform.position.z;

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
            r.sortingOrder += sortingOrderOffset;
        }
    }

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

    IEnumerator DeliverRoutine(CustomerTable table, TraySlot slot)
    {
        string itemName = slot.GetFoodItemName();
        Sprite itemSprite = slot.GetFoodSprite();

        lastTablePos = table.transform.position;
        SetFood(null);

        System.Func<Vector3> trayPoint = () => TrayPoint(slot);

        SetPosition(trayPoint() + Vector3.up * flyHeight);
        SetAlpha(0f);
        SetVisible(true);

        yield return FlyIn(trayPoint);
        yield return Hover(trayPoint, pickupDelay);

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

        SetFood(itemSprite);
        Debug.Log($"[AutoServerBot] pickup: sprite={(itemSprite != null ? itemSprite.name : "NULL")}, foodRenderer={(foodRenderer != null ? foodRenderer.name : "NULL")}");
        if (clearTrayAtPickup) slot.ClearSlot();

        yield return FlyOut(trayPoint);

        System.Func<Vector3> tablePoint = () => HoverPoint(table);

        SetVisible(false);
        SetAlpha(0f);
        SetPosition(tablePoint() + Vector3.up * flyHeight);
        SetVisible(true);

        yield return FlyIn(tablePoint);
        yield return Hover(tablePoint, serveDelay);

        bool served = false;
        if (table != null && table.HasAnyNPCWaiting() && table.TryServeFoodDirect(itemName))
        {
            served = true;

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

        if (leaveDelay > 0f)
            yield return Hover(tablePoint, leaveDelay, bob: false);

        yield return FlyOut(tablePoint);

        HideDrone();
    }

    IEnumerator FlyIn(System.Func<Vector3> point)
    {
        yield return Tween(flyInDuration, t =>
        {
            Vector3 target = point();
            SetPosition(Vector3.LerpUnclamped(target + Vector3.up * flyHeight, target, EaseOutCubic(t)));
            SetAlpha(Mathf.SmoothStep(0f, 1f, t));
        });
    }

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

    IEnumerator FlyOut(System.Func<Vector3> point)
    {
        yield return Tween(flyOutDuration, t =>
        {
            Vector3 from = point();
            SetPosition(Vector3.LerpUnclamped(from, from + Vector3.up * flyHeight, EaseInCubic(t)));
            SetAlpha(1f - Mathf.SmoothStep(0f, 1f, t));
        });
    }

    Vector3 HoverPoint(CustomerTable table)
    {
        if (table != null) lastTablePos = table.transform.position;
        return new Vector3(lastTablePos.x + hoverOffset.x, lastTablePos.y + hoverOffset.y, homeZ);
    }

    Vector3 TrayPoint(TraySlot slot)
    {
        if (slot != null)
        {
            Vector3 p = slot.transform.position;

            Canvas canvas = slot.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                Camera cam = Camera.main;
                if (cam != null)
                    p = cam.ScreenToWorldPoint(new Vector3(p.x, p.y, Mathf.Abs(cam.transform.position.z - homeZ)));
            }

            lastTrayPos = p;
        }

        return new Vector3(lastTrayPos.x + trayHoverOffset.x, lastTrayPos.y + trayHoverOffset.y, homeZ);
    }

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

    public void TryBuyAutoServerBot()
    {
        if (isActivated) return;

        if (CurrencyManager.Instance != null)
        {
            if (CurrencyManager.Instance.TrySpendMoney(botPrice))
            {
                ActivateBot();
                UINotificationManager.Instance?.ShowNotification("NOTI_BUY_SERVER_BOT_SUCCESS");
            }
            else
            {
                UINotificationManager.Instance?.ShowNotification("NOTI_NOT_ENOUGH_MONEY", botPrice);
            }
        }
    }

    public void ActivateBot()
    {
        isActivated = true;

        if (!gameObject.activeSelf) gameObject.SetActive(true);

        Debug.Log("[AutoServerBot] เปิดใช้งานโดรนเสิร์ฟอาหารแล้ว!");
    }
}
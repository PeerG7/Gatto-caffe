#if ENABLE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;   // InputState
using UnityEngine.InputSystem.UI;
using TMPro;

// =====================================================================
// GamepadCursor — ก้านขวา = เมาส์ (ใช้กดปุ่ม UI / วางเฟอร์นิเจอร์ได้เหมือนเมาส์จริง)
//
//   ก้านขวา        ขยับเคอร์เซอร์
//   RT หรือ กดก้านขวา (R3)   คลิกซ้าย (กดค้าง = ลาก)
//   LT             คลิกขวา
//
// ไม่ต้องตั้งค่าอะไรใน Scene: สคริปต์สร้างตัวเองตอนเริ่มเกม (RuntimeInitializeOnLoadMethod)
// วิธีทำงาน: เขียนตำแหน่ง/ปุ่มลงใน Mouse.current ของ Input System โดยตรง
//   → EventSystem (InputSystemUIInputModule) เห็นเป็นเมาส์ปกติ จึงใช้ได้กับ Canvas ทุกอัน
//     ไม่ว่าจะเปิด/ปิดด้วยสคริปต์ หรือด้วย UnityEvent (OnClick → SetActive) ใน Inspector
//   → สคริปต์เกมที่อ่านเมาส์ต้องใช้ GameInput.PointerPosition / PointerDown แทน Input.mousePosition
//
// เมาส์จริงขยับเมื่อไหร่ เคอร์เซอร์จอยจะหายไปเอง (ตรวจจาก Input แบบเก่า ซึ่งไม่โดนการฉีดค่าของเรา)
// =====================================================================
[DefaultExecutionOrder(-32000)] // ฉีดค่าก่อนสคริปต์อื่นทุกตัวในเฟรมเดียวกัน → wasPressedThisFrame ใช้ได้
public class GamepadCursor : MonoBehaviour
{
    public static GamepadCursor Instance { get; private set; }

    /// <summary>true = กำลังใช้เคอร์เซอร์จอย (ใช้สลับไอคอนปุ่มช่วยเหลือบน UI ได้)</summary>
    public static bool IsPadMode { get; private set; }

    [Header("Movement")]
    [Tooltip("ความเร็วเคอร์เซอร์ (พิกเซล/วินาที ที่จอสูง 1080 — ปรับตามความสูงจอให้อัตโนมัติ)")]
    public float speed = 1000f;
    public float deadzone = 0.15f;
    [Tooltip("> 1 = เอียงก้านนิดเดียวช้าลง ควบคุมละเอียดขึ้น")]
    public float sensitivityCurve = 1.6f;

    [Header("Look")]
    [Tooltip("ว่าง = วาดลูกศรให้อัตโนมัติ")]
    public Sprite cursorSprite;
    [Tooltip("ขนาดเคอร์เซอร์ (พิกเซลที่จอสูง 1080)")]
    public float cursorSize = 40f;
    public bool hideWhenIdle = true;
    public float idleHideSeconds = 3f;

    [Header("Behaviour")]
    [Tooltip("เปลี่ยน StandaloneInputModule เป็น InputSystemUIInputModule ให้อัตโนมัติ (จำเป็น ไม่งั้น UI มองไม่เห็นเคอร์เซอร์จอย)")]
    public bool autoSwapUIModule = true;
    [Tooltip("หลังคลิกด้วย RT ให้ล้างปุ่มที่ถูกเลือกค้าง กันปุ่ม A ไป 'กดซ้ำ' ปุ่ม HUD ที่ค้างอยู่")]
    public bool deselectAfterClick = true;

    private Canvas canvas;
    private RectTransform cursorRect;
    private Image cursorImage;

    private Vector2 pos;
    private bool leftDown, rightDown;
    private float lastActivity;
    private bool prevCursorVisible = true;
    private EventSystem lastEventSystem;
    private int deselectFrame = -1;
#if ENABLE_LEGACY_INPUT_MANAGER
    private Vector3 lastLegacyMouse;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        new GameObject("GamepadCursor").AddComponent<GamepadCursor>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (transform.parent == null) DontDestroyOnLoad(gameObject);
        BuildCursorVisual();
    }

    void OnDisable() { if (IsPadMode) ExitPadMode(Mouse.current); }

    void OnDestroy() { if (Instance == this) { Instance = null; IsPadMode = false; } }

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        EnsureUIModule();

        Gamepad pad = Gamepad.current;
        Vector2 stick = pad != null ? pad.rightStick.ReadValue() : Vector2.zero;
        bool wantLeft = pad != null && (pad.rightTrigger.isPressed || pad.rightStickButton.isPressed);
        bool wantRight = pad != null && pad.leftTrigger.isPressed;
        bool padTouched = stick.sqrMagnitude > deadzone * deadzone || wantLeft;

        // --- สลับโหมด ---
        if (RealMouseActive(mouse))
        {
            if (IsPadMode) ExitPadMode(mouse);
            return;
        }
        if (!IsPadMode)
        {
            if (!padTouched) return;
            EnterPadMode(mouse);
        }

        // --- ขยับเคอร์เซอร์ ---
        Vector2 delta = Vector2.zero;
        float mag = stick.magnitude;
        if (mag > deadzone)
        {
            float t = Mathf.Pow(Mathf.Clamp01((mag - deadzone) / (1f - deadzone)), sensitivityCurve);
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);   // unscaled: เกมถูก Pause (timeScale 0) ตอนเปิดเมนู
            delta = (stick / mag) * (t * speed * (Screen.height / 1080f) * dt);
        }

        Vector2 newPos = pos + delta;
        newPos.x = Mathf.Clamp(newPos.x, 0f, Screen.width - 1f);
        newPos.y = Mathf.Clamp(newPos.y, 0f, Screen.height - 1f);
        delta = newPos - pos;
        pos = newPos;

        // --- ฉีดเข้า Mouse.current ---
        if ((mouse.position.ReadValue() - pos).sqrMagnitude > 0.0001f) InputState.Change(mouse.position, pos);
        if (delta != Vector2.zero) InputState.Change(mouse.delta, delta);

        if (wantLeft != leftDown)
        {
            InputState.Change(mouse.leftButton, wantLeft ? 1f : 0f);
            leftDown = wantLeft;
            if (!wantLeft && deselectAfterClick) deselectFrame = Time.frameCount + 2;
        }
        if (wantRight != rightDown)
        {
            InputState.Change(mouse.rightButton, wantRight ? 1f : 0f);
            rightDown = wantRight;
        }

        if (deselectFrame >= 0 && Time.frameCount >= deselectFrame)
        {
            deselectFrame = -1;
            DeselectIfNotTextField();
        }

        // --- แสดงผล ---
        if (delta != Vector2.zero || wantLeft || wantRight) lastActivity = Time.unscaledTime;
        bool visible = !hideWhenIdle || (Time.unscaledTime - lastActivity) < idleHideSeconds;
        if (cursorRect.gameObject.activeSelf != visible) cursorRect.gameObject.SetActive(visible);
        cursorRect.anchoredPosition = pos;
    }

    // ------------------------------------------------------------------
    //  Mode
    // ------------------------------------------------------------------
    void EnterPadMode(Mouse mouse)
    {
        IsPadMode = true;
        pos = mouse.position.ReadValue();
        lastActivity = Time.unscaledTime;
        prevCursorVisible = Cursor.visible;
        Cursor.visible = false;
        cursorRect.sizeDelta = Vector2.one * cursorSize * (Screen.height / 1080f);
        cursorRect.anchoredPosition = pos;
        cursorRect.gameObject.SetActive(true);
    }

    void ExitPadMode(Mouse mouse)
    {
        IsPadMode = false;
        if (mouse != null)
        {
            if (leftDown) InputState.Change(mouse.leftButton, 0f);
            if (rightDown) InputState.Change(mouse.rightButton, 0f);
        }
        leftDown = rightDown = false;
        Cursor.visible = prevCursorVisible;
        if (cursorRect != null) cursorRect.gameObject.SetActive(false);
    }

    bool RealMouseActive(Mouse mouse)
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        // Input แบบเก่าอ่านเมาส์จริงตรง ๆ ไม่โดนการฉีดค่าของเรา → ตัดสินได้ชัวร์ว่า "คนขยับเมาส์จริง"
        Vector3 p = Input.mousePosition;
        bool moved = (p - lastLegacyMouse).sqrMagnitude > 4f;
        lastLegacyMouse = p;
        return moved || Input.GetMouseButton(0) || Input.GetMouseButton(1);
#else
        // โหมด New อย่างเดียว: ถ้าตำแหน่งใน Mouse ไม่ตรงกับที่เราเขียนไว้ = เมาส์จริงเขียนทับ
        return IsPadMode && (mouse.position.ReadValue() - pos).sqrMagnitude > 9f;
#endif
    }

    // ------------------------------------------------------------------
    //  EventSystem
    // ------------------------------------------------------------------
    void EnsureUIModule()
    {
        if (!autoSwapUIModule) return;

        EventSystem es = EventSystem.current;
        if (es == null || es == lastEventSystem) return;
        lastEventSystem = es;

        if (es.GetComponent<InputSystemUIInputModule>() != null) return;

        foreach (BaseInputModule m in es.GetComponents<BaseInputModule>())
            m.enabled = false;

        InputSystemUIInputModule module = es.gameObject.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
        Debug.Log("[GamepadCursor] เปลี่ยน EventSystem เป็น InputSystemUIInputModule แล้ว (เพื่อให้ UI เห็นเคอร์เซอร์จอย)");
    }

    void DeselectIfNotTextField()
    {
        EventSystem es = EventSystem.current;
        if (es == null) return;
        GameObject sel = es.currentSelectedGameObject;
        if (sel == null) return;
        if (sel.GetComponent<InputField>() != null || sel.GetComponent<TMP_InputField>() != null) return;
        es.SetSelectedGameObject(null);
    }

    // ------------------------------------------------------------------
    //  Visual (สร้าง Canvas + ลูกศรด้วยโค้ด ไม่ต้องทำ Prefab)
    // ------------------------------------------------------------------
    void BuildCursorVisual()
    {
        GameObject cgo = new GameObject("CursorCanvas", typeof(Canvas));
        cgo.transform.SetParent(transform, false);
        canvas = cgo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;   // อยู่บนสุดเสมอ

        GameObject igo = new GameObject("Cursor", typeof(RectTransform), typeof(Image));
        igo.transform.SetParent(cgo.transform, false);
        cursorImage = igo.GetComponent<Image>();
        cursorImage.raycastTarget = false;      // ห้ามบังการคลิกของตัวเอง
        cursorImage.preserveAspect = true;
        cursorImage.sprite = cursorSprite != null ? cursorSprite : BuildDefaultSprite();

        cursorRect = igo.GetComponent<RectTransform>();
        cursorRect.anchorMin = cursorRect.anchorMax = Vector2.zero;
        cursorRect.pivot = new Vector2(0f, 1f);  // ปลายลูกศรอยู่มุมซ้ายบน
        cursorRect.sizeDelta = Vector2.one * cursorSize;
        igo.SetActive(false);
    }

    static Sprite BuildDefaultSprite()
    {
        const int N = 32;
        Vector2[] poly =
        {
            new Vector2(2f, 30f), new Vector2(2f, 8f), new Vector2(7.5f, 12.5f), new Vector2(11f, 4f),
            new Vector2(15f, 6f), new Vector2(11f, 14f), new Vector2(19f, 14f)
        };

        bool[] inside = new bool[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
                inside[y * N + x] = PointInPolygon(poly, x + 0.5f, y + 0.5f);

        Color32[] px = new Color32[N * N];
        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                if (inside[y * N + x]) { px[y * N + x] = new Color32(255, 255, 255, 255); continue; }

                bool edge = false;
                for (int oy = -1; oy <= 1 && !edge; oy++)
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        int nx = x + ox, ny = y + oy;
                        if (nx >= 0 && ny >= 0 && nx < N && ny < N && inside[ny * N + nx]) { edge = true; break; }
                    }
                px[y * N + x] = edge ? new Color32(0, 0, 0, 255) : new Color32(0, 0, 0, 0);
            }
        }

        Texture2D tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0f, 1f), 100f);
    }

    static bool PointInPolygon(Vector2[] p, float x, float y)
    {
        bool c = false;
        for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
        {
            if (((p[i].y > y) != (p[j].y > y)) &&
                (x < (p[j].x - p[i].x) * (y - p[i].y) / (p[j].y - p[i].y) + p[i].x))
                c = !c;
        }
        return c;
    }
}
#endif
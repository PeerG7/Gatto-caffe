#if ENABLE_INPUT_SYSTEM
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// =====================================================================
// GamepadUINavigator — D-pad / ก้านซ้าย เลือกปุ่ม (Button / Selectable) ในหน้า UI
//
//  ทำงานร่วมกับ EventSystem: การ "เลื่อนไปปุ่มถัดไป" ทำโดย InputSystemUIInputModule อยู่แล้ว
//  (D-pad = Navigate ตามค่าเริ่มต้น) สคริปต์นี้เติมสิ่งที่ EventSystem ไม่ทำให้:
//
//   1) ยังไม่มีปุ่มถูกเลือก + กด D-pad  → เลือกปุ่มที่อยู่ใกล้เคอร์เซอร์ที่สุด (ที่ไม่ถูกบัง)
//   2) เลื่อนแล้วหลุดไปปุ่มของหน้าอื่น (เช่น HUD ด้านหลัง) → เด้งกลับ  (จำกัดให้อยู่ใน Canvas/หน้าเดียวกัน)
//   3) เคอร์เซอร์จอยกระโดดตามไปอยู่บนปุ่มที่เลือก → กด A (คลิกซ้าย) แล้วกดปุ่มนั้นพอดี
//   4) ปุ่มอยู่ใน ScrollRect นอกจอ → เลื่อนให้เห็นอัตโนมัติ
//   5) วาดกรอบสีเหลืองรอบปุ่มที่เลือก (ไม่ต้องไปแก้ Selected Color ของปุ่มทีละตัว)
//
//  สร้างตัวเองตอนเริ่มเกม ไม่ต้องวางใน Scene
//  ปุ่มที่ไม่อยากให้ D-pad เลือก: ตั้ง Navigation = None ที่ปุ่มนั้น
//  หน้าที่อยู่ใน Canvas ใหญ่ร่วมกับ HUD: แปะ UIContextTag บน Panel ของหน้านั้น จะใช้เป็นขอบเขตการเลื่อน
// =====================================================================
public class GamepadUINavigator : MonoBehaviour
{
    public static GamepadUINavigator Instance { get; private set; }

    [Header("Navigation")]
    [Tooltip("D-pad / ก้านซ้ายต้องเอียงเกินค่านี้ถึงนับว่ากด")]
    public float navThreshold = 0.5f;
    [Tooltip("เลื่อนได้เฉพาะในหน้าเดียวกับปุ่มที่เลือกอยู่ (Canvas เดียวกัน หรือใต้ UIContextTag เดียวกัน)")]
    public bool restrictToCurrentPage = true;
    public float scrollPadding = 12f;

    [Header("Selection frame")]
    [Tooltip("วาดกรอบสีเหลืองรอบปุ่มที่เลือกด้วย D-pad (ปิดไว้เป็นค่าเริ่มต้น — เคอร์เซอร์จะกระโดดไปอยู่บนปุ่มที่เลือกอยู่แล้ว)")]
    public bool showSelectionFrame = false;
    public Color frameColor = new Color(1f, 0.85f, 0.2f, 1f);
    [Tooltip("ระยะกรอบห่างจากปุ่ม (พิกเซลที่จอสูง 1080)")]
    public float framePadding = 6f;

    private bool prevNavActive;
    private float lastNavTime = -10f;
    private GameObject lastSelected;

    private readonly List<RaycastResult> hits = new List<RaycastResult>();
    private readonly Vector3[] corners = new Vector3[4];

    private Canvas frameCanvas;
    private RectTransform frameRect;
    private Image frameImage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        new GameObject("GamepadUINavigator").AddComponent<GamepadUINavigator>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (transform.parent == null) DontDestroyOnLoad(gameObject);
        BuildFrame();
    }

    void OnDisable() { SetFrame(null); }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // LateUpdate = หลัง EventSystem ประมวลผลการเลื่อนของเฟรมนี้แล้ว (กันเลือกซ้อนจนข้ามปุ่ม)
    void LateUpdate()
    {
        EventSystem es = EventSystem.current;
        if (es == null || !GameInput.ClickMode || !InputDeviceMonitor.UsingGamepad)
        {
            lastSelected = null;
            prevNavActive = false;
            SetFrame(null);
            return;
        }

        Vector2 nav = GameInput.Navigate;   // D-pad (ถ้ากด) ไม่งั้นก้านซ้าย
        bool navActive = nav.sqrMagnitude > navThreshold * navThreshold;
        bool navEdge = navActive && !prevNavActive;
        prevNavActive = navActive;
        if (navActive) lastNavTime = Time.unscaledTime;

        Selectable sel = Usable(es.currentSelectedGameObject);

        if (navEdge && sel == null)
        {
            // ยังไม่มีปุ่มที่เลือกอยู่ → เลือกปุ่มที่ใกล้เคอร์เซอร์ที่สุด
            Selectable cand = FindNearest(GameInput.PointerPosition);
            if (cand != null)
            {
                es.SetSelectedGameObject(cand.gameObject);
                sel = cand;
                FocusCursorOn(cand);
                lastSelected = cand.gameObject;
            }
        }
        else if (es.currentSelectedGameObject != lastSelected)
        {
            bool navigated = Time.unscaledTime - lastNavTime < 0.3f;   // การเปลี่ยนครั้งนี้มาจาก D-pad/ก้านซ้าย
            if (navigated && sel != null)
            {
                Selectable prev = Usable(lastSelected);
                if (restrictToCurrentPage && prev != null && Scope(prev) != Scope(sel))
                {
                    es.SetSelectedGameObject(prev.gameObject);   // หลุดไปหน้าอื่น → เด้งกลับ
                    sel = prev;
                }
                else
                {
                    FocusCursorOn(sel);
                }
            }
            lastSelected = es.currentSelectedGameObject;
        }

        // แสดงกรอบเมื่อ "ครั้งล่าสุดที่ผู้เล่นสั่ง" คือ D-pad (ถ้าขยับเคอร์เซอร์ด้วยก้านขวาทีหลัง กรอบจะซ่อน)
        bool showFrame = showSelectionFrame && sel != null && GamepadCursor.Instance != null
                         && lastNavTime >= GamepadCursor.Instance.LastStickMoveTime;
        SetFrame(showFrame ? sel : null);
    }

    // ------------------------------------------------------------------
    //  ค้นหา / ตรวจสอบปุ่ม
    // ------------------------------------------------------------------
    static Selectable Usable(GameObject go)
    {
        if (go == null || !go.activeInHierarchy) return null;
        Selectable s = go.GetComponent<Selectable>();
        return (s != null && s.IsInteractable()) ? s : null;
    }

    static bool CanUse(Selectable s)
    {
        return s != null && s.isActiveAndEnabled && s.IsInteractable() && s.navigation.mode != Navigation.Mode.None;
    }

    Selectable FindNearest(Vector2 from)
    {
        Selectable best = null, bestAny = null;
        float bestD = float.MaxValue, bestAnyD = float.MaxValue;

        foreach (Selectable s in Selectable.allSelectablesArray)
        {
            if (!CanUse(s)) continue;
            if (!TryScreenPoint(s, out Vector2 sp)) continue;

            float d = (sp - from).sqrMagnitude;
            if (d < bestAnyD) { bestAnyD = d; bestAny = s; }
            if (d < bestD && IsTopmost(s, sp)) { bestD = d; best = s; }
        }
        return best != null ? best : bestAny;   // ไม่มีตัวที่ "อยู่บนสุด" เลย ก็ใช้ตัวที่ใกล้ที่สุดแทน
    }

    // ปุ่มนี้ไม่ถูกอะไรบัง (เช่น พื้นหลังของหน้าที่เปิดทับอยู่)
    bool IsTopmost(Selectable s, Vector2 screenPos)
    {
        EventSystem es = EventSystem.current;
        if (es == null) return false;

        PointerEventData ped = new PointerEventData(es) { position = screenPos };
        hits.Clear();
        es.RaycastAll(ped, hits);

        for (int i = 0; i < hits.Count; i++)
        {
            GameObject go = hits[i].gameObject;
            if (go == null) continue;
            return go == s.gameObject || go.transform.IsChildOf(s.transform);
        }
        return false;
    }

    // ขอบเขตของ "หน้า": UIContextTag ที่ใกล้ที่สุด ไม่งั้นใช้ Canvas ราก
    static Transform Scope(Selectable s)
    {
        UIContextTag tag = s.GetComponentInParent<UIContextTag>();
        if (tag != null) return tag.transform;

        Canvas c = s.GetComponentInParent<Canvas>();
        return c != null ? c.rootCanvas.transform : null;
    }

    static Camera CanvasCamera(Component c)
    {
        Canvas canvas = c.GetComponentInParent<Canvas>();
        if (canvas == null) return null;

        Canvas root = canvas.rootCanvas;
        if (root.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        return root.worldCamera != null ? root.worldCamera : Camera.main;
    }

    static bool TryScreenPoint(Selectable s, out Vector2 sp)
    {
        sp = default;
        RectTransform rt = s.transform as RectTransform;
        if (rt == null) return false;

        sp = RectTransformUtility.WorldToScreenPoint(CanvasCamera(rt), rt.TransformPoint(rt.rect.center));
        return sp.x >= 0f && sp.y >= 0f && sp.x <= Screen.width && sp.y <= Screen.height;
    }

    // ------------------------------------------------------------------
    //  ย้ายเคอร์เซอร์ / เลื่อน ScrollRect
    // ------------------------------------------------------------------
    void FocusCursorOn(Selectable s)
    {
        ScrollIntoView(s);

        if (GamepadCursor.Instance != null && TryScreenPoint(s, out Vector2 sp))
            GamepadCursor.Instance.MoveTo(sp);
    }

    void ScrollIntoView(Selectable s)
    {
        ScrollRect sr = s.GetComponentInParent<ScrollRect>();
        if (sr == null || sr.content == null) return;

        Canvas.ForceUpdateCanvases();

        RectTransform view = sr.viewport != null ? sr.viewport : (RectTransform)sr.transform;
        Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(view, (RectTransform)s.transform);
        Rect v = view.rect;

        Vector2 delta = Vector2.zero;
        if (sr.horizontal)
        {
            if (b.max.x > v.xMax - scrollPadding) delta.x = (v.xMax - scrollPadding) - b.max.x;
            else if (b.min.x < v.xMin + scrollPadding) delta.x = (v.xMin + scrollPadding) - b.min.x;
        }
        if (sr.vertical)
        {
            if (b.max.y > v.yMax - scrollPadding) delta.y = (v.yMax - scrollPadding) - b.max.y;
            else if (b.min.y < v.yMin + scrollPadding) delta.y = (v.yMin + scrollPadding) - b.min.y;
        }

        if (delta != Vector2.zero)
        {
            sr.content.anchoredPosition += delta;
            Canvas.ForceUpdateCanvases();
        }
    }

    // ------------------------------------------------------------------
    //  กรอบสีเหลืองรอบปุ่มที่เลือก (วาดบน Canvas ของตัวเอง ใต้เคอร์เซอร์)
    // ------------------------------------------------------------------
    void BuildFrame()
    {
        GameObject cgo = new GameObject("SelectionFrameCanvas", typeof(Canvas));
        cgo.transform.SetParent(transform, false);
        frameCanvas = cgo.GetComponent<Canvas>();
        frameCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        frameCanvas.sortingOrder = short.MaxValue - 1;   // ใต้เคอร์เซอร์ (short.MaxValue) แต่เหนือ UI อื่น

        GameObject igo = new GameObject("Frame", typeof(RectTransform), typeof(Image));
        igo.transform.SetParent(cgo.transform, false);
        frameImage = igo.GetComponent<Image>();
        frameImage.raycastTarget = false;
        frameImage.sprite = BuildFrameSprite();
        frameImage.type = Image.Type.Sliced;
        frameImage.fillCenter = false;
        frameImage.color = frameColor;
        frameImage.pixelsPerUnitMultiplier = Mathf.Max(0.1f, 1080f / Mathf.Max(1, Screen.height));   // จอใหญ่ → กรอบหนาขึ้น

        frameRect = igo.GetComponent<RectTransform>();
        frameRect.anchorMin = frameRect.anchorMax = Vector2.zero;
        frameRect.pivot = Vector2.zero;
        igo.SetActive(false);
    }

    static Sprite BuildFrameSprite()
    {
        const int N = 16, B = 4;
        Color32[] px = new Color32[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                bool border = x < B || y < B || x >= N - B || y >= N - B;
                px[y * N + x] = border ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }

        Texture2D tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f, 0,
                             SpriteMeshType.FullRect, new Vector4(B, B, B, B));
    }

    void SetFrame(Selectable s)
    {
        if (frameRect == null) return;

        if (s == null)
        {
            if (frameRect.gameObject.activeSelf) frameRect.gameObject.SetActive(false);
            return;
        }

        RectTransform rt = s.transform as RectTransform;
        if (rt == null) { frameRect.gameObject.SetActive(false); return; }

        rt.GetWorldCorners(corners);
        Camera cam = CanvasCamera(rt);
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < 4; i++)
        {
            Vector2 p = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }

        float pad = framePadding * (Screen.height / 1080f);
        frameImage.color = frameColor;
        frameRect.anchoredPosition = min - Vector2.one * pad;
        frameRect.sizeDelta = (max - min) + Vector2.one * (pad * 2f);
        if (!frameRect.gameObject.activeSelf) frameRect.gameObject.SetActive(true);
    }
}
#endif
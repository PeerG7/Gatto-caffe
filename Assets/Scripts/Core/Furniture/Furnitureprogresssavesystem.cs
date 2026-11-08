using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// เซฟ/โหลดเฟอร์นิเจอร์ทั้งหมด: ของใน Inventory + ของที่วางอยู่ในฉาก (ตำแหน่ง/การหมุน/เลเวล)
///
/// ไม่ต้องวางใน Scene เอง — สร้างตัวเองอัตโนมัติเมื่อโหลดฉากที่มี FurnitureInventory + ComputerStation (ShopScene)
/// (ถ้าอยากวางเองก็ได้ ระบบจะไม่สร้างซ้ำ)
///
/// เซฟเมื่อ DayNightManager.SaveDayProgress() ถูกเรียก (ขึ้นวันใหม่ / กด Title / กด Exit) — เป็น Snapshot ชุดเดียวกับวันและเงิน
/// โหลดอัตโนมัติตอนเข้าฉาก (ถ้ามีเซฟ) — New Game จะลบ Key นี้ทิ้ง
/// </summary>
public class FurnitureProgressSaveSystem : MonoBehaviour
{
    public const string SaveKey = "SavedFurniture"; // ต้องตรงกับที่ MainMenuManager / NewGameIntroDialogue ลบตอน New Game

    public static FurnitureProgressSaveSystem Instance;

    [Header("Item Database (Optional)")]
    [Tooltip("ปกติระบบหา FurnitureItemData เองจากปุ่มซื้อใน Shop และของที่วางอยู่ — ถ้ามีไอเทมที่ไม่ได้อยู่ในนั้น ให้ลากมาใส่ที่นี่")]
    public List<FurnitureItemData> knownItems = new List<FurnitureItemData>();

    // ── Save Data ──────────────────────────────────────────────────
    [Serializable]
    public class Entry
    {
        public string itemKey;
        public string instanceID;
        public int level;
        public bool isUnlocked;
        public bool isUpgraded;
        public bool isBaked;      // true = ของที่วางอยู่ใน Scene ตั้งแต่แรก (ไม่ได้ Spawn ตอนรัน)
        public string bakedKey;
        public float x, y, z;
        public float rotZ;
    }

    [Serializable]
    public class SaveData
    {
        public List<Entry> inventory = new List<Entry>();
        public List<Entry> placed = new List<Entry>();
    }

    private readonly Dictionary<string, FurnitureItemData> _registry = new Dictionary<string, FurnitureItemData>();
    private readonly Dictionary<string, PlacedFurniture> _baked = new Dictionary<string, PlacedFurniture>();
    private readonly HashSet<PlacedFurniture> _bakedSet = new HashSet<PlacedFurniture>();
    private bool _restoreDone = false;

    // ── Auto Bootstrap ─────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        if (FindFirstObjectByType<FurnitureProgressSaveSystem>() != null) return;

        // สร้างเฉพาะฉากร้านค้า (มีคลังเฟอร์นิเจอร์ + คอมร้านค้า) ไม่สร้างใน MainMenu / Ending
        if (FindFirstObjectByType<FurnitureInventory>() == null) return;
        if (FindFirstObjectByType<ComputerStation>() == null) return;

        new GameObject("FurnitureProgressSaveSystem").AddComponent<FurnitureProgressSaveSystem>();
    }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        StartCoroutine(RestoreRoutine());
    }

    // ── Public API ─────────────────────────────────────────────────
    /// <summary>เรียกจาก DayNightManager.SaveDayProgress() — ไม่ทำอะไรถ้าไม่ได้อยู่ในฉากร้านค้า</summary>
    public static void SaveIfAvailable()
    {
        if (Instance != null) Instance.SaveAll();
    }

    public void SaveAll()
    {
        // กันเซฟทับด้วยข้อมูลว่าง ถ้ายังโหลดไม่เสร็จ
        if (!_restoreDone)
        {
            Debug.LogWarning("[FurnitureProgressSaveSystem] ยังโหลดเฟอร์นิเจอร์ไม่เสร็จ — ข้ามการเซฟรอบนี้");
            return;
        }

        SaveData data = new SaveData();

        if (FurnitureInventory.Instance != null)
        {
            foreach (var inst in FurnitureInventory.Instance.storedInstances)
            {
                if (inst == null || inst.itemData == null) continue;
                Entry e = new Entry
                {
                    itemKey = ItemKey(inst.itemData),
                    instanceID = inst.instanceID,
                    level = inst.currentLevel,
                    isUnlocked = inst.isUnlocked,
                    isUpgraded = inst.isUpgraded
                };
                data.inventory.Add(e);
            }
        }

        foreach (var pf in FindObjectsByType<PlacedFurniture>(FindObjectsSortMode.None))
        {
            if (pf == null) continue;

            FurnitureItemData item = pf.itemData;
            if (item == null && pf.instanceData != null) item = pf.instanceData.itemData;
            if (item == null) item = pf.defaultItemData;

            int level = EffectiveLevel(pf);
            bool baked = _bakedSet.Contains(pf);

            Entry e = new Entry
            {
                itemKey = item != null ? ItemKey(item) : "",
                instanceID = pf.instanceID,
                level = level,
                isUnlocked = level >= 1,
                isUpgraded = level >= 2,
                isBaked = baked,
                bakedKey = baked ? BakedKey(pf) : "",
                x = pf.transform.position.x,
                y = pf.transform.position.y,
                z = pf.transform.position.z,
                rotZ = pf.transform.eulerAngles.z
            };
            data.placed.Add(e);
        }

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        Debug.Log($"💾 [FurnitureProgressSaveSystem] บันทึกเฟอร์นิเจอร์ — คลัง {data.inventory.Count} ชิ้น / วางอยู่ {data.placed.Count} ชิ้น");
    }

    // ── Restore ────────────────────────────────────────────────────
    IEnumerator RestoreRoutine()
    {
        // รอ 1 เฟรมให้ Start() ของทุกสคริปต์ทำงานก่อน (เช่น PlacedFurniture.Start สร้าง instanceData)
        yield return null;

        // จดของที่วางอยู่ใน Scene ตั้งแต่แรกไว้ก่อน (ก่อน Spawn อะไรเพิ่ม)
        foreach (var pf in FindObjectsByType<PlacedFurniture>(FindObjectsSortMode.None))
        {
            if (pf == null) continue;
            _baked[BakedKey(pf)] = pf;
            _bakedSet.Add(pf);
        }

        BuildRegistry();

        string json = PlayerPrefs.GetString(SaveKey, "");
        if (!string.IsNullOrEmpty(json))
        {
            SaveData data = null;
            try { data = JsonUtility.FromJson<SaveData>(json); }
            catch (Exception ex) { Debug.LogError("[FurnitureProgressSaveSystem] อ่านไฟล์เซฟไม่ได้: " + ex.Message); }

            if (data != null) Apply(data);
        }
        else
        {
            Debug.Log("[FurnitureProgressSaveSystem] ไม่มีเซฟเฟอร์นิเจอร์ — ใช้สถานะเริ่มต้นของฉาก");
        }

        _restoreDone = true;
    }

    void Apply(SaveData data)
    {
        bool sceneChanged = false;

        // 1) คลังเฟอร์นิเจอร์
        if (FurnitureInventory.Instance != null)
        {
            FurnitureInventory.Instance.storedInstances.Clear();
            foreach (var e in data.inventory)
            {
                FurnitureItemData item = Lookup(e.itemKey);
                if (item == null) { Debug.LogWarning($"[FurnitureProgressSaveSystem] หาไอเทม '{e.itemKey}' ไม่เจอ (คลัง) — ข้าม"); continue; }
                FurnitureInventory.Instance.storedInstances.Add(MakeInstance(item, e));
            }
        }

        // 2) ของที่วางไว้ใน Scene ตั้งแต่แรก — คืนเลเวล หรือลบถ้าผู้เล่นเคยเก็บไปแล้ว
        HashSet<string> matchedBaked = new HashSet<string>();
        List<Entry> toSpawn = new List<Entry>();

        foreach (var e in data.placed)
        {
            if (e.isBaked && !string.IsNullOrEmpty(e.bakedKey) &&
                _baked.TryGetValue(e.bakedKey, out PlacedFurniture pf) && pf != null)
            {
                ApplyLevel(pf, e.level);
                matchedBaked.Add(e.bakedKey);
            }
            else
            {
                toSpawn.Add(e); // ของที่ Spawn ตอนรัน (หรือของ Baked ที่หาไม่เจอ → สร้างใหม่จาก Prefab)
            }
        }

        foreach (var kv in _baked)
        {
            if (kv.Value != null && !matchedBaked.Contains(kv.Key))
            {
                Destroy(kv.Value.gameObject); // ผู้เล่นเคยเก็บชิ้นนี้ไปแล้ว
                _bakedSet.Remove(kv.Value);
                sceneChanged = true;
            }
        }

        // 3) Spawn ของที่ผู้เล่นวางเอง
        foreach (var e in toSpawn)
        {
            FurnitureItemData item = Lookup(e.itemKey);
            if (item == null || item.prefab == null)
            {
                Debug.LogWarning($"[FurnitureProgressSaveSystem] หาไอเทม/Prefab '{e.itemKey}' ไม่เจอ (ของที่วางไว้) — ข้าม");
                continue;
            }

            GameObject go = Instantiate(item.prefab, new Vector3(e.x, e.y, e.z), Quaternion.Euler(0f, 0f, e.rotZ));

            PlacedFurniture placed = go.GetComponent<PlacedFurniture>();
            if (placed == null) placed = go.AddComponent<PlacedFurniture>();

            placed.Setup(MakeInstance(item, e));
            sceneChanged = true;
        }

        if (sceneChanged && FurniturePlacementManager.Instance != null)
            FurniturePlacementManager.Instance.RebakeNavMesh();

        if (FurnitureEditUI.Instance != null) FurnitureEditUI.Instance.RefreshUI();

        StartCoroutine(RestoreVipBonusRoutine());
        Debug.Log($"✅ [FurnitureProgressSaveSystem] โหลดเฟอร์นิเจอร์สำเร็จ — คลัง {data.inventory.Count} ชิ้น / วางอยู่ {data.placed.Count} ชิ้น");
    }

    // โอกาสแมว VIP เพิ่มขึ้นตอนอัปเกรด (FurnitureObject.AttemptUpgrade) และ NPCSpawner เริ่มใหม่ทุกครั้งที่โหลดฉาก → คืนค่ากลับให้
    IEnumerator RestoreVipBonusRoutine()
    {
        int guard = 0;
        while (NPCSpawner.Instance == null && guard++ < 120) yield return null;
        if (NPCSpawner.Instance == null) yield break;

        int bonus = 0;

        foreach (var pf in FindObjectsByType<PlacedFurniture>(FindObjectsSortMode.None))
        {
            if (pf == null) continue;
            FurnitureObject fo = pf.GetComponent<FurnitureObject>();
            if (fo != null && fo.isUpgraded) bonus += fo.vipChanceBonusOnUpgrade;
        }

        if (FurnitureInventory.Instance != null)
        {
            foreach (var inst in FurnitureInventory.Instance.storedInstances)
            {
                if (inst == null || inst.itemData == null || inst.itemData.prefab == null) continue;
                if (inst.currentLevel < 2 && !inst.isUpgraded) continue;
                FurnitureObject fo = inst.itemData.prefab.GetComponent<FurnitureObject>();
                if (fo != null) bonus += fo.vipChanceBonusOnUpgrade;
            }
        }

        if (bonus > 0) NPCSpawner.Instance.IncreaseVIPChance(bonus);
    }

    // ── Helpers ────────────────────────────────────────────────────
    FurnitureInstanceData MakeInstance(FurnitureItemData item, Entry e)
    {
        FurnitureInstanceData inst = new FurnitureInstanceData(item, e.level, e.isUpgraded);
        if (!string.IsNullOrEmpty(e.instanceID)) inst.instanceID = e.instanceID;
        inst.currentLevel = e.level;
        inst.isUnlocked = e.isUnlocked;
        inst.isUpgraded = e.isUpgraded;
        return inst;
    }

    void ApplyLevel(PlacedFurniture pf, int level)
    {
        pf.currentLevel = Mathf.Clamp(level, 0, pf.maxLevel);

        if (pf.instanceData != null)
        {
            pf.instanceData.currentLevel = pf.currentLevel;
            pf.instanceData.isUnlocked = pf.currentLevel >= 1;
            pf.instanceData.isUpgraded = pf.currentLevel >= 2;
        }

        pf.UpdateVisual(); // ตั้ง FurnitureObject.isUnlocked/isUpgraded และเปลี่ยนภาพให้ตรงเลเวล
    }

    // PlacedFurniture.currentLevel กับ FurnitureObject.isUnlocked/isUpgraded อาจไม่ตรงกัน (ปลดล็อกไม่ได้บวก Level) → เอาค่าที่สูงกว่า
    int EffectiveLevel(PlacedFurniture pf)
    {
        int level = pf.currentLevel;

        FurnitureObject fo = pf.GetComponent<FurnitureObject>();
        if (fo != null)
        {
            int fromFlags = fo.isUpgraded ? 2 : (fo.isUnlocked ? 1 : 0);
            level = Mathf.Max(level, fromFlags);
        }
        return Mathf.Clamp(level, 0, pf.maxLevel);
    }

    static string ItemKey(FurnitureItemData d)
    {
        return !string.IsNullOrEmpty(d.id) ? d.id : d.name;
    }

    static string BakedKey(PlacedFurniture pf)
    {
        Vector3 p = pf.transform.position;
        return $"{pf.gameObject.name}@{p.x:F2},{p.y:F2}";
    }

    void BuildRegistry()
    {
        _registry.Clear();

        foreach (var d in knownItems) Register(d);

        foreach (var btn in FindObjectsByType<ShopItemButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (btn != null) Register(btn.itemData);

        foreach (var pf in FindObjectsByType<PlacedFurniture>(FindObjectsSortMode.None))
        {
            if (pf == null) continue;
            Register(pf.itemData);
            Register(pf.defaultItemData);
            if (pf.instanceData != null) Register(pf.instanceData.itemData);
        }

        if (FurnitureInventory.Instance != null)
            foreach (var inst in FurnitureInventory.Instance.storedInstances)
                if (inst != null) Register(inst.itemData);
    }

    void Register(FurnitureItemData d)
    {
        if (d == null) return;
        string key = ItemKey(d);
        if (!_registry.ContainsKey(key)) _registry[key] = d;
    }

    FurnitureItemData Lookup(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        _registry.TryGetValue(key, out FurnitureItemData d);
        return d;
    }
}
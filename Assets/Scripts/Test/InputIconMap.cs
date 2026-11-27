using System.Collections.Generic;
using UnityEngine;

// =====================================================================
// InputIconMap — ตารางจับคู่ "รูปปุ่มคีย์บอร์ด → รูปปุ่มจอย" ใช้ร่วมกันทั้งเกม
//
//  สร้างไฟล์ตารางไว้ที่  Assets/Resources/InputIconMap.asset  (ชื่อต้องตรงเป๊ะ และต้องอยู่ในโฟลเดอร์ Resources)
//    Project → คลิกขวา → Create → Gatto → Input Icon Map
//  แล้วเพิ่มแถวละ 1 ปุ่ม เช่น  Keyboard Mouse = รูปหมวกคีย์ E,  Xbox = รูปวงกลม A, ...
//
//  InputIconAutoSwap จะสลับรูปทุกอันในฉากที่ใช้รูปในตารางนี้ให้เอง ไม่ต้องแปะ InputIconSwitcher ทีละตัว
// =====================================================================
[CreateAssetMenu(fileName = "InputIconMap", menuName = "Gatto/Input Icon Map")]
public class InputIconMap : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        [Tooltip("ชื่อกันลืม เช่น Interact (E / A)")]
        public string note;
        public Sprite keyboardMouse;
        public Sprite xbox;
        [Tooltip("ว่าง = ใช้รูป Xbox")] public Sprite playStation;
        [Tooltip("ว่าง = ใช้รูป Xbox")] public Sprite nintendo;

        public Sprite For(InputScheme scheme, PadStyle style)
        {
            if (scheme == InputScheme.KeyboardMouse) return keyboardMouse;

            switch (style)
            {
                case PadStyle.PlayStation: return playStation != null ? playStation : xbox;
                case PadStyle.Nintendo: return nintendo != null ? nintendo : xbox;
                default: return xbox;
            }
        }
    }

    public List<Entry> entries = new List<Entry>();

    private Dictionary<Sprite, Entry> _lookup;

    void OnEnable() { _lookup = null; }
    void OnValidate() { _lookup = null; }

    void Build()
    {
        _lookup = new Dictionary<Sprite, Entry>();
        foreach (Entry e in entries)
        {
            if (e == null) continue;
            Add(e.keyboardMouse, e);
            Add(e.xbox, e);
            Add(e.playStation, e);
            Add(e.nintendo, e);
        }
    }

    void Add(Sprite s, Entry e)
    {
        if (s != null && !_lookup.ContainsKey(s)) _lookup[s] = e;
    }

    /// <summary>คืนรูปที่ควรแสดงตอนนี้แทน current  (null = รูปนี้ไม่อยู่ในตาราง หรือถูกต้องอยู่แล้ว ไม่ต้องเปลี่ยน)</summary>
    public Sprite Resolve(Sprite current, InputScheme scheme, PadStyle style)
    {
        if (current == null) return null;
        if (_lookup == null) Build();
        if (!_lookup.TryGetValue(current, out Entry e)) return null;

        Sprite target = e.For(scheme, style);
        return (target != null && target != current) ? target : null;
    }
}
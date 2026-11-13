using System.Collections.Generic;
using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>Kendiliğinden yeniden dolan tarla (saman, mısır...). Üstünden geçen taşıyıcı toplar.</summary>
    public class ResourceField : Station
    {
        System.Action<Carrier> onCarrier;   // ForEachCarrier için bir kez oluşturulur (karede çöp ayırmasın)
        public static readonly List<ResourceField> All = new List<ResourceField>();
        public ItemType produces;
        public Transform[] slots;
        public float regrowTime = 5f;

        Item[] grown;
        float[] timers;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public int GrownCount
        {
            get
            {
                if (grown == null) return 0;
                int n = 0;
                foreach (var g in grown) if (g != null) n++;
                return n;
            }
        }

        void Start()
        {
            grown = new Item[slots.Length];
            timers = new float[slots.Length];
            for (int i = 0; i < slots.Length; i++) Grow(i, false);
        }

        // ---------- tarla büyüklüğü (yükseltme): parsel başına 4 → 8 → 12 ürün ----------
        Vector3[] baseSlots;            // sahnedeki ilk 4 yuvanın yerel konumları
        Vector3 basePlantOffset;        // bitki süsü ile yuva merkezi arasındaki fark
        float basePlantScale = 1f;
        GameObject plantTemplate;
        readonly List<Transform> plants = new List<Transform>();
        int sizeLevel;

        public int SizeLevel => sizeLevel;
        public static int SlotsFor(int level) => 4 * (1 + Mathf.Clamp(level, 0, 2));

        public void SetSizeLevel(int level)
        {
            level = Mathf.Clamp(level, 0, 2);
            if (baseSlots == null) CaptureBase();
            if (level == sizeLevel && slots.Length == SlotsFor(level)) return;
            sizeLevel = level;

            // yerleşim: 4 → 2×2 (eski hali), 8 → 4 sütun × 2 sıra, 12 → 4 sütun × 3 sıra
            Vector3 c = Vector3.zero; foreach (var b in baseSlots) c += b; c /= baseSlots.Length;
            float hx = 0f, hz = 0f; foreach (var b in baseSlots) { hx = Mathf.Max(hx, Mathf.Abs(b.x - c.x)); hz = Mathf.Max(hz, Mathf.Abs(b.z - c.z)); }
            var pos = new List<Vector3>();
            if (level == 0) pos.AddRange(baseSlots);
            else
            {
                float[] cols = { -1.5f, -0.5f, 0.5f, 1.5f };
                float[] rows = level == 1 ? new[] { 1f, -1f } : new[] { 1.25f, 0f, -1.25f };
                foreach (var rz in rows) foreach (var cx in cols) pos.Add(new Vector3(c.x + cx * hx, c.y, c.z + rz * hz));
            }
            float sc = level == 0 ? 1f : (level == 1 ? 0.85f : 0.75f);

            // yuvalar: var olanlar yeniden yerleşir (üstündeki ürün de kayar), eksikler eklenir.
            // Bir yuva tarlanın doğrudan çocuğu olmayabilir (mısırda yuva, mısır sapının içinde): sap ile birlikte taşınır/çoğaltılır.
            var hold = new List<Transform>(holders);
            var list = new List<Transform>(slots);
            while (list.Count < pos.Count)
            {
                var src = hold[0];
                var clone = Instantiate(src.gameObject, src.parent).transform;
                clone.name = src.name;
                foreach (var it in clone.GetComponentsInChildren<Item>(true)) DestroyImmediate(it.gameObject);   // kopyalanan sapın üstündeki ürün gelmesin
                var slot = slotPath != null ? clone.Find(slotPath) : clone;
                hold.Add(clone);
                list.Add(slot != null ? slot : clone);
            }
            for (int i = 0; i < hold.Count; i++)
            {
                bool on = i < pos.Count;
                hold[i].gameObject.SetActive(on);
                if (!on) continue;
                hold[i].localPosition = pos[i] + holderOffset;
                hold[i].localScale = holderScale * sc;
            }
            holders = hold;
            slots = list.GetRange(0, pos.Count).ToArray();

            if (grown != null && grown.Length != slots.Length)   // oyun sırasında büyüdü: yeni yuvalar kısa sürede dolar
            {
                var g = new Item[slots.Length]; var tm = new float[slots.Length];
                for (int i = 0; i < slots.Length; i++)
                {
                    if (i < grown.Length) { g[i] = grown[i]; tm[i] = timers[i]; }
                    else tm[i] = 0.15f * (i - grown.Length + 1);
                }
                grown = g; timers = tm;
            }

            // bitki süsleri her yuvanın altında
            if (plantTemplate != null)
            {
                while (plants.Count < slots.Length)
                {
                    var p = Instantiate(plantTemplate, plantTemplate.transform.parent).transform;
                    p.name = plantTemplate.name;
                    plants.Add(p);
                }
                for (int i = 0; i < plants.Count; i++)
                {
                    bool on = i < slots.Length;
                    plants[i].gameObject.SetActive(on);
                    if (!on) continue;
                    var parent = plants[i].parent;
                    var wp = transform.TransformPoint(pos[i] + basePlantOffset);
                    plants[i].localPosition = parent ? parent.InverseTransformPoint(wp) : wp;
                    plants[i].localScale = Vector3.one * basePlantScale * sc;
                }
            }
        }

        List<Transform> holders;        // her yuvanın tarladaki doğrudan taşıyıcısı (çoğunlukla yuvanın kendisi)
        string slotPath;                 // taşıyıcının içinde yuvanın yolu (yuva = taşıyıcı ise null)
        Vector3 holderOffset, holderScale = Vector3.one;

        void CaptureBase()
        {
            holders = new List<Transform>();
            baseSlots = new Vector3[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                var h = slots[i];
                while (h.parent != null && h.parent != transform) h = h.parent;
                holders.Add(h);
                baseSlots[i] = transform.InverseTransformPoint(slots[i].position);   // yuvanın tarla uzayındaki gerçek yeri
            }
            if (holders[0] != slots[0])
            {
                var path = slots[0].name; var t = slots[0].parent;
                while (t != holders[0]) { path = t.name + "/" + path; t = t.parent; }
                slotPath = path;
            }
            holderOffset = holders[0].localPosition - baseSlots[0];
            holderScale = holders[0].localScale;
            var model = transform.Find("Model_v2");
            if (model == null) return;
            foreach (Transform ch in model) if (ch.name.StartsWith("Bitki_")) plants.Add(ch);
            if (plants.Count == 0) return;
            plantTemplate = plants[0].gameObject;
            basePlantScale = plants[0].localScale.x;
            // bitki ile yuva arasındaki ortalama fark (yerel uzayda)
            Vector3 pc = Vector3.zero, sc = Vector3.zero;
            foreach (var p in plants) pc += transform.InverseTransformPoint(p.position);
            foreach (var b in baseSlots) sc += b;
            basePlantOffset = pc / plants.Count - sc / baseSlots.Length;
            basePlantOffset.y = pc.y / plants.Count - sc.y / baseSlots.Length;
        }

        void Grow(int i, bool pop)
        {
            var it = GameConfig.I.Spawn(produces, slots[i].position);
            it.transform.SetParent(slots[i], false);
            it.transform.localPosition = Vector3.zero;
            it.transform.localScale = Vector3.one;
            grown[i] = it;
            if (pop) it.gameObject.AddComponent<PopAnim>();
        }

        void Update()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (grown[i] != null) continue;
                timers[i] -= Time.deltaTime;
                if (timers[i] <= 0f) Grow(i, true);
            }

            ForEachCarrier(onCarrier ??= c =>
            {
                if (!c.ReadyToTransfer() || c.stack.IsFull || !c.MayCollectFrom(this)) return;
                for (int i = 0; i < slots.Length; i++)
                {
                    if (grown[i] == null) continue;
                    var it = grown[i];
                    grown[i] = null;
                    timers[i] = regrowTime;
                    c.stack.Add(it);
                    if (c.isPlayer) { AudioFx.Play(Sfx.Pickup); Quests.Report(QuestType.Harvest); Fx.Sparkle(slots[i].position + Vector3.up * 0.3f); }
                    c.ResetTransfer();
                    return;
                }
            });
        }
    }
}

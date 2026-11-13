using System.Collections.Generic;
using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>Drive-thru arabası: sıraya girer, camda siparişini bekler, ödeyip gider.</summary>
    public class DriveThruCar : MonoBehaviour
    {
        public class Line { public ItemType type; public int need; public int got; }

        public float speed = 6f;
        [Header("Araç türü")]
        public string kindName = "SİPARİŞ";
        public float priceMultiplier = 1.2f;
        public float tipMultiplier = 1f;
        public ItemContainer trunk;
        public Renderer body;
        public GameObject barRoot;
        public Transform barFill;
        public Renderer barRenderer;
        public WorldLabel label;

        public readonly List<Line> order = new List<Line>();
        static readonly List<DriveThruCar> All = new List<DriveThruCar>();
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        /// <summary>Bu noktanın çevresinde araç yok mu? (yeni araç üst üste doğmasın)</summary>
        public static bool LaneClear(Vector3 p, float radius)
        {
            foreach (var c in All)
            {
                var d = c.transform.position - p; d.y = 0f;
                if (d.sqrMagnitude < radius * radius) return false;
            }
            return true;
        }
        public bool AtWindow { get; private set; }
        public bool Arrived { get; private set; }

        Vector3 target;
        Vector3[] exitPath;
        int exitIndex;
        bool isFront, leaving;
        float waited, patience = 45f;
        MaterialPropertyBlock barMpb;
        bool wasHonking;

        static readonly Color Green = new Color(0.30f, 0.75f, 0.35f);
        static readonly Color Yellow = new Color(1f, 0.70f, 0.15f);
        static readonly Color Red = new Color(0.90f, 0.22f, 0.20f);

        public void Init(List<Line> lines, float patienceSeconds, Vector3 firstTarget)
        {
            order.AddRange(lines);
            patience = patienceSeconds;
            target = firstTarget;
            if (body)
            {
                var mpb = new MaterialPropertyBlock();
                mpb.SetColor("_BaseColor", Random.ColorHSV(0f, 1f, 0.5f, 0.8f, 0.75f, 1f));
                body.SetPropertyBlock(mpb);
            }
            if (barRoot) barRoot.SetActive(false);
        }

        public void SetSlot(Vector3 pos, bool front)
        {
            target = pos;
            isFront = front;
        }

        /// <summary>Kalan sabır oranı (1 = yeni geldi, 0 = bitti).</summary>
        public float Remaining => Mathf.Clamp01(1f - waited / patience);

        /// <summary>Bahşiş oranı: yeşil +%30, sarı +%10, kırmızı 0.</summary>
        public float TipRate => Remaining > 0.5f ? 0.3f : (Remaining > 0f ? 0.1f : 0f);

        public Color TierColor => Remaining > 0.5f ? Green : (Remaining > 0f ? Yellow : Red);

        /// <summary>Açık zeminde okunabilir koyu tonlar (sipariş yazısı için).</summary>
        public Color LabelColor => Remaining > 0.5f ? new Color(0.10f, 0.50f, 0.15f) : (Remaining > 0f ? new Color(0.85f, 0.42f, 0f) : new Color(0.80f, 0.12f, 0.10f));

        public bool Complete
        {
            get
            {
                foreach (var l in order) if (l.got < l.need) return false;
                return true;
            }
        }

        public bool Wants(ItemType t)
        {
            foreach (var l in order) if (l.type == t && l.got < l.need) return true;
            return false;
        }

        public void Receive(Item it)
        {
            foreach (var l in order)
            {
                if (l.type != it.type || l.got >= l.need) continue;
                l.got++;
                break;
            }
            trunk.Add(it);
        }

        public int Payment()
        {
            int baseValue = 0;
            foreach (var l in order) baseValue += GameConfig.I.Price(l.type) * l.need;
            return Mathf.RoundToInt(baseValue * priceMultiplier * (1f + TipRate * tipMultiplier));
        }

        public void Leave(Vector3 exitPos) { Leave(new[] { exitPos }); }

        /// <summary>Hizmet bitti: yol noktalarını sırayla izleyip gider (sapakta döner).</summary>
        public void Leave(Vector3[] path)
        {
            leaving = true;
            exitPath = path;
            exitIndex = 0;
            AtWindow = false;
            if (barRoot) barRoot.SetActive(false);
            if (label) label.Set("");
        }

        void Update()
        {
            if (leaving)
            {
                var from = transform.position;
                bool last = exitIndex >= exitPath.Length - 1;
                if (MoveTo(exitPath[exitIndex], !last))
                {
                    if (last) { Destroy(gameObject); return; }
                    exitIndex++;
                }
                var mv = transform.position - from; mv.y = 0f;
                if (mv.sqrMagnitude > 1e-6f)   // gittiği yöne dönsün (sapakta kıvrılır)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(mv), 1f - Mathf.Exp(-10f * Time.deltaTime));
                return;
            }

            Arrived = MoveTo(target);
            AtWindow = isFront && Arrived;
            if (AtWindow) waited += Time.deltaTime;

            if (barRoot && barRoot.activeSelf != AtWindow) barRoot.SetActive(AtWindow);
            if (AtWindow && barFill)
            {
                barFill.localScale = new Vector3(Mathf.Max(0.001f, Remaining), 1f, 1f);
                if (barRenderer)
                {
                    if (barMpb == null) barMpb = new MaterialPropertyBlock();
                    barMpb.SetColor("_BaseColor", TierColor);
                    barRenderer.SetPropertyBlock(barMpb);
                }
            }
            if (label)
            {
                bool honk = AtWindow && Remaining <= 0f && Mathf.Repeat(Time.time, 1.2f) < 0.6f;
                label.Set(honk ? "BİP BİP!" : "");
                if (honk && !wasHonking) AudioFx.Play(Sfx.Horn, 0.45f);
                wasHonking = honk;
            }
        }

        bool MoveTo(Vector3 t, bool passThrough = false)
        {
            var p = transform.position;
            var d = t - p;
            d.y = 0f;
            float dist = d.magnitude;
            if (dist < 0.02f) return true;
            // Aynı şeritte öndeki araca çok yaklaşma: araçlar birbirinin içinden geçmesin
            var dir = d / dist;
            foreach (var o in All)
            {
                if (o == this) continue;
                var od = o.transform.position - p; od.y = 0f;
                float ahead = Vector3.Dot(od, dir);
                if (ahead > 0.1f && ahead < 3.7f && (od - dir * ahead).sqrMagnitude < 1.4f) return false;
            }
            float step = speed * Time.deltaTime * (passThrough ? 1f : Mathf.Clamp(dist / 1.5f, 0.35f, 1f)); // varışa yaklaşınca yavaşla (ara noktalarda değil)
            if (step >= dist) { transform.position = new Vector3(t.x, p.y, t.z); return true; }
            transform.position = p + d / dist * step;
            return false;
        }
    }
}

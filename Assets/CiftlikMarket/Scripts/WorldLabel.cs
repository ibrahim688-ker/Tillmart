using System.Collections.Generic;
using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>
    /// Kameraya dönük dünya içi yazı (sayaç, fiyat, müşteri balonu).
    /// Yazının arkasına otomatik boyutlanan bir arka plan çizer:
    /// Pill = yuvarlak rozet, Speech = konuşma balonu (kuyruklu), Thought = düşünce balonu (kabarcıklı).
    /// Yazı ve balon her şeyin üstünde çizilir, böylece modellerin arkasında kaybolmaz.
    /// </summary>
    [RequireComponent(typeof(TextMesh))]
    public class WorldLabel : MonoBehaviour
    {
        public enum Style { Plain, Pill, Speech, Thought }

        public Style style = Style.Pill;
        public Color fill = new Color(1f, 1f, 1f, 0.96f);
        public Color outline = new Color(0.15f, 0.17f, 0.2f, 0.92f);
        [Tooltip("Kuyruğun / kabarcıkların yönü (yazıya göre, x sağ, y yukarı)")]
        public Vector2 tailDir = new Vector2(0f, -1f);
        public float padding = 0.16f;

        TextMesh tm;
        MeshRenderer textRenderer;
        string last;
        Transform bg;
        MeshFilter bgFilter;
        Mesh bgMesh;
        Vector3 lastSize = new Vector3(-1, -1, -1);
        Style lastStyle;

        static Material bgMat;
        static readonly Dictionary<Font, Material> textMats = new Dictionary<Font, Material>();

        void Awake()
        {
            tm = GetComponent<TextMesh>();
            textRenderer = GetComponent<MeshRenderer>();
            if (Application.isPlaying) ApplyTextMaterial();
        }

        void ApplyTextMaterial()
        {
            if (style == Style.Plain || tm == null || tm.font == null) return;
            if (!textMats.TryGetValue(tm.font, out var m) || m == null)
            {
                var src = Resources.Load<Material>("LabelTextMat");
                if (src == null) return;
                m = new Material(src) { name = "LabelText_" + tm.font.name };
                m.mainTexture = tm.font.material.mainTexture;
                textMats[tm.font] = m;
            }
            textRenderer.sharedMaterial = m;
        }

        public void Set(string s)
        {
            if (tm == null) tm = GetComponent<TextMesh>();
            if (s == last) return;
            last = s;
            tm.text = s;
        }

        // ---------------- ürün resimleri (TextMesh <quad>) ----------------
        static Material iconMat;
        const int AtlasCols = 5;   // 5×5 ürün atlası (20 ürün)

        /// <summary>Yazıda ürün resmi kullanılacaksa çağrılır: ikinci materyal olarak ürün atlasını ekler.</summary>
        public void UseIcons()
        {
            if (tm == null) tm = GetComponent<TextMesh>();
            if (textRenderer == null) textRenderer = GetComponent<MeshRenderer>();
            if (iconMat == null) iconMat = Resources.Load<Material>("LabelIconMat");
            if (iconMat == null) return;
            tm.richText = true;
            var mats = textRenderer.sharedMaterials;
            if (mats.Length >= 2 && mats[1] == iconMat) return;
            textRenderer.sharedMaterials = new[] { mats.Length > 0 ? mats[0] : null, iconMat };
        }

        /// <summary>Satır içi ürün resmi etiketi (UseIcons çağrılmış etikette).</summary>
        public static string Icon(ItemType t, int size = 150)
        {
            int i = (int)t;
            float w = 1f / AtlasCols;
            float x = (i % AtlasCols) * w;
            float y = 1f - (i / AtlasCols + 1) * w;
            return "<quad material=1 size=" + size + " x=" + x.ToString(System.Globalization.CultureInfo.InvariantCulture)
                 + " y=" + y.ToString(System.Globalization.CultureInfo.InvariantCulture) + " width=0.2 height=0.2 />";
        }

        public void SetColor(Color c)
        {
            if (tm == null) tm = GetComponent<TextMesh>();
            if (tm.color != c) tm.color = c;
        }

        void OnEnable() { if (Application.isPlaying && !Active.Contains(this)) Active.Add(this); }
        void OnDisable() { Active.Remove(this); }

        void Start()
        {
            baseLocal = lastSetLocal = transform.localPosition;
            // Sabit istasyon yazıları yerinde kalır; hareketli balonlar (müşteri, araba, sipariş) onlardan kaçar.
            pinned = GetComponentInParent<Shelf>() != null || GetComponentInParent<Converter>() != null
                  || GetComponentInParent<UnlockZone>() != null
                  || (transform.parent != null && transform.parent.name.StartsWith("Tarla_"));
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam) transform.rotation = cam.transform.rotation;
            if (!Application.isPlaying) return;
            UpdateBackground();
            if (cam && Time.frameCount != solvedFrame) { solvedFrame = Time.frameCount; Solve(cam); }
        }

        // ---------------- üst üste binmeyi önleme ----------------

        public static readonly List<WorldLabel> Active = new List<WorldLabel>();
        static int solvedFrame = -1;
        static readonly List<WorldLabel> vis = new List<WorldLabel>();
        const float MarginPx = 6f;

        [System.NonSerialized] public bool pinned;
        Vector3 baseLocal, lastSetLocal;
        Vector2 cur, target, scrC, scrH;
        float ppu;
        bool hasBounds;
        Bounds bgBounds;

        // TextMesh.text okumak her seferinde yeni string ayırır: Set'te saklanan son yazıyı kullan
        string CurText { get { if (last == null && tm != null) last = tm.text; return last; } }

        bool Visible => style != Style.Plain && tm != null && textRenderer != null && textRenderer.enabled
                        && !string.IsNullOrEmpty(CurText) && hasBounds;

        static void Solve(Camera cam)
        {
            vis.Clear();
            float tanHalf = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            foreach (var l in Active)
            {
                if (l == null) continue;
                // Başka bir script konumu değiştirdiyse (ör. sipariş balonu arabayı izliyor) yeni taban odur.
                if ((l.transform.localPosition - l.lastSetLocal).sqrMagnitude > 1e-8f) l.baseLocal = l.transform.localPosition;
                if (!l.Visible) { l.cur = Vector2.zero; l.Apply(cam); continue; }
                Vector3 bw = l.transform.parent ? l.transform.parent.TransformPoint(l.baseLocal) : l.baseLocal;
                Vector3 sp = cam.WorldToScreenPoint(bw);
                if (sp.z <= 0.1f) { l.Apply(cam); continue; }
                l.ppu = cam.orthographic ? Screen.height / (2f * cam.orthographicSize) : Screen.height / (2f * sp.z * tanHalf);
                float s = l.transform.lossyScale.x * l.ppu;
                l.scrC = new Vector2(sp.x + l.bgBounds.center.x * s, sp.y + l.bgBounds.center.y * s);
                l.scrH = new Vector2(l.bgBounds.extents.x * s, l.bgBounds.extents.y * s);
                l.target = Vector2.zero;
                vis.Add(l);
            }

            for (int it = 0; it < 6; it++)
            {
                bool moved = false;
                for (int i = 0; i < vis.Count; i++)
                    for (int j = i + 1; j < vis.Count; j++)
                    {
                        var a = vis[i]; var b = vis[j];
                        if (a.pinned && b.pinned) continue;
                        Vector2 d = (b.scrC + b.target) - (a.scrC + a.target);
                        float ox = a.scrH.x + b.scrH.x + MarginPx - Mathf.Abs(d.x);
                        float oy = a.scrH.y + b.scrH.y + MarginPx - Mathf.Abs(d.y);
                        if (ox <= 0f || oy <= 0f) continue;
                        float wa = a.pinned ? 0f : 1f, wb = b.pinned ? 0f : 1f, sum = wa + wb;
                        // dikeyde ayır (balonlar üst üste dizilir); yatay örtüşme çok azsa yana kaydır
                        Vector2 push;
                        if (ox < oy * 0.5f) push = new Vector2(d.x >= 0f ? ox : -ox, 0f);
                        else push = new Vector2(0f, (d.y > 0f || (d.y == 0f && i < j)) ? oy : -oy);
                        a.target -= push * (wa / sum);
                        b.target += push * (wb / sum);
                        moved = true;
                    }
                if (!moved) break;
            }

            float k = 1f - Mathf.Exp(-12f * Time.deltaTime);
            foreach (var l in vis) { l.cur = Vector2.Lerp(l.cur, l.target, k); l.Apply(cam); }
        }

        void Apply(Camera cam)
        {
            Vector3 local = baseLocal;
            if (cur.sqrMagnitude > 0.01f && ppu > 0f)
            {
                Vector3 w = (cam.transform.right * cur.x + cam.transform.up * cur.y) / ppu;
                local += transform.parent ? transform.parent.InverseTransformVector(w) : w;
            }
            transform.localPosition = local;
            lastSetLocal = local;
        }

        // ---------------- arka plan ----------------

        void UpdateBackground()
        {
            bool show = style != Style.Plain && tm != null && !string.IsNullOrEmpty(CurText) && textRenderer.enabled;
            if (!show) { if (bg && bg.gameObject.activeSelf) bg.gameObject.SetActive(false); return; }

            if (bg == null)
            {
                if (bgMat == null) bgMat = Resources.Load<Material>("LabelBGMat");
                var go = new GameObject("LabelBG");
                go.transform.SetParent(transform, false);
                bg = go.transform;
                bgFilter = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = bgMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                bgMesh = new Mesh { name = "LabelBG" };
                bgMesh.MarkDynamic();
                bgFilter.sharedMesh = bgMesh;
            }
            if (!bg.gameObject.activeSelf) bg.gameObject.SetActive(true);

            var b = textRenderer.localBounds;
            Vector3 size = b.size;
            if ((size - lastSize).sqrMagnitude < 1e-6f && lastStyle == style) return;
            lastSize = size; lastStyle = style;
            Build(b.center, size);
        }

        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Color> c = new List<Color>();
        readonly List<int> t = new List<int>();

        void Build(Vector3 center, Vector3 size)
        {
            v.Clear(); c.Clear(); t.Clear();
            float hx = size.x * 0.5f + padding * (style == Style.Pill ? 1.1f : 1.3f);
            float hy = size.y * 0.5f + padding * (style == Style.Pill ? 0.55f : 0.9f);
            float r = Mathf.Min(style == Style.Thought ? hy : Mathf.Min(0.22f, hy), hx);
            float o = style == Style.Pill ? 0.035f : 0.05f;
            Vector2 cc = new Vector2(center.x, center.y);
            Vector2 dir = tailDir.sqrMagnitude > 0.001f ? tailDir.normalized : Vector2.down;

            // önce dış çizgi (koyu), sonra dolgu (açık) — sırayla çizildiği için dolgu üstte kalır
            for (int pass = 0; pass < 2; pass++)
            {
                float grow = pass == 0 ? o : 0f;
                Color col = pass == 0 ? outline : fill;
                float z = pass == 0 ? 0.03f : 0.02f;
                RoundRect(cc, hx + grow, hy + grow, r + grow, z, col);
                if (style == Style.Speech)
                {
                    Vector2 edge = cc + new Vector2(dir.x * hx * 0.5f, -hy);
                    float w = 0.13f + grow * 1.6f, len = 0.28f + grow * 1.4f;
                    Tri(edge + new Vector2(-w, 0.02f), edge + new Vector2(w, 0.02f), edge + new Vector2(dir.x * 0.12f, -len), z, col);
                }
                else if (style == Style.Thought)
                {
                    Vector2 edge = cc + new Vector2(dir.x * hx, dir.y * hy) * 0.92f;
                    Circle(edge + dir * 0.22f, 0.12f + grow, z, col);
                    Circle(edge + dir * 0.48f, 0.075f + grow, z, col);
                }
            }

            bgMesh.Clear();
            bgMesh.SetVertices(v);
            bgMesh.SetColors(c);
            bgMesh.SetTriangles(t, 0);
            bgMesh.RecalculateBounds();
            bgBounds = bgMesh.bounds;
            hasBounds = true;
        }

        void RoundRect(Vector2 cc, float hx, float hy, float r, float z, Color col)
        {
            int start = v.Count;
            v.Add(new Vector3(cc.x, cc.y, z)); c.Add(col);
            const int seg = 6;
            Vector2[] corners = { new Vector2(hx - r, hy - r), new Vector2(-hx + r, hy - r), new Vector2(-hx + r, -hy + r), new Vector2(hx - r, -hy + r) };
            for (int k = 0; k < 4; k++)
            {
                for (int s = 0; s <= seg; s++)
                {
                    float a = (k * 90f + s * 90f / seg) * Mathf.Deg2Rad;
                    Vector2 p = cc + corners[k] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                    v.Add(new Vector3(p.x, p.y, z)); c.Add(col);
                }
            }
            int n = v.Count - start - 1;
            for (int i = 0; i < n; i++) { t.Add(start); t.Add(start + 1 + (i + 1) % n); t.Add(start + 1 + i); }
        }

        void Circle(Vector2 p0, float r, float z, Color col)
        {
            int start = v.Count;
            v.Add(new Vector3(p0.x, p0.y, z)); c.Add(col);
            const int seg = 14;
            for (int s = 0; s < seg; s++)
            {
                float a = s * Mathf.PI * 2f / seg;
                v.Add(new Vector3(p0.x + Mathf.Cos(a) * r, p0.y + Mathf.Sin(a) * r, z)); c.Add(col);
            }
            for (int i = 0; i < seg; i++) { t.Add(start); t.Add(start + 1 + (i + 1) % seg); t.Add(start + 1 + i); }
        }

        void Tri(Vector2 a, Vector2 b, Vector2 d, float z, Color col)
        {
            int s = v.Count;
            v.Add(new Vector3(a.x, a.y, z)); v.Add(new Vector3(b.x, b.y, z)); v.Add(new Vector3(d.x, d.y, z));
            c.Add(col); c.Add(col); c.Add(col);
            t.Add(s); t.Add(s + 2); t.Add(s + 1);
        }
    }
}

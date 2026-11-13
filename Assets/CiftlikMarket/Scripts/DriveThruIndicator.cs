using UnityEngine;
using UnityEngine.UI;

namespace CiftlikMarket
{
    /// <summary>
    /// Drive-thru camında araba beklerken cam ekranda görünmüyorsa sol kenarda küçük yuvarlak bir
    /// direksiyon işareti çıkar (sesi kapalı oynayanlar için). Çevresindeki halka arabanın sabrını
    /// gösterir (yeşil → sarı → kırmızı); birden çok araba varsa köşede sayısı yazar.
    /// Sahneye eklemeye gerek yok, oyun başlarken kendini kurar.
    /// </summary>
    public class DriveThruIndicator : MonoBehaviour
    {
        static DriveThruIndicator inst;

        RectTransform root, body;
        Image ring, wheel;
        GameObject badge;
        Text badgeText;
        DriveThru dt;
        float findTimer, show, pulse;
        bool wasWaiting;

        static readonly Color Ink = new Color32(0x2B, 0x2D, 0x42, 0xFF);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (inst != null) return;
            var go = new GameObject("DriveThruIndicator");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<DriveThruIndicator>();
        }

        void Awake()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;   // HUD seviyesinde; pencereler açıkken zaten gizlenir
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            root = new GameObject("Root", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(canvasGo.transform, false);
            root.anchorMin = root.anchorMax = new Vector2(0f, 0.5f);
            root.pivot = new Vector2(0f, 0.5f);
            root.sizeDelta = new Vector2(150f, 130f);

            // sola bakan ok ucu (cam o tarafta)
            var tip = Img("Tip", root, MakeTriangle(), Ink);
            Place(tip.rectTransform, new Vector2(8f, 0f), new Vector2(34f, 44f));

            body = new GameObject("Body", typeof(RectTransform)).GetComponent<RectTransform>();
            body.SetParent(root, false);
            Place(body, new Vector2(84f, 0f), new Vector2(124f, 124f));

            Img("Outline", body, MakeCircle(1f, 0f), Ink).rectTransform.sizeDelta = new Vector2(124f, 124f);
            var track = Img("Track", body, MakeCircle(1f, 0.70f), new Color(0.85f, 0.87f, 0.9f));
            track.rectTransform.sizeDelta = new Vector2(112f, 112f);
            ring = Img("Ring", body, MakeCircle(1f, 0.70f), Color.green);
            ring.rectTransform.sizeDelta = new Vector2(112f, 112f);
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            ring.fillClockwise = false;
            Img("Face", body, MakeCircle(1f, 0f), new Color32(0xFF, 0xF7, 0xE6, 0xFF)).rectTransform.sizeDelta = new Vector2(80f, 80f);
            wheel = Img("Wheel", body, MakeWheel(), Ink);
            wheel.rectTransform.sizeDelta = new Vector2(68f, 68f);

            // araba sayısı rozeti
            var b = Img("Badge", body, MakeCircle(1f, 0f), Ink);
            b.rectTransform.anchoredPosition = new Vector2(44f, 44f);
            b.rectTransform.sizeDelta = new Vector2(46f, 46f);
            var bf = Img("Fill", b.rectTransform, MakeCircle(1f, 0f), new Color32(0xFF, 0x5A, 0x5F, 0xFF));
            bf.rectTransform.sizeDelta = new Vector2(38f, 38f);
            var tgo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            tgo.transform.SetParent(b.rectTransform, false);
            badgeText = tgo.GetComponent<Text>();
            badgeText.font = Resources.Load<Font>("Fonts/Baloo2-ExtraBold");
            if (badgeText.font == null) badgeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            badgeText.fontSize = 28;
            badgeText.fontStyle = FontStyle.Bold;
            badgeText.alignment = TextAnchor.MiddleCenter;
            badgeText.color = Color.white;
            badgeText.raycastTarget = false;
            var trt = (RectTransform)tgo.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
            badge = b.gameObject;

            root.localScale = Vector3.zero;
        }

        void Update()
        {
            if (dt == null)
            {
                findTimer -= Time.unscaledDeltaTime;
                if (findTimer <= 0f) { findTimer = 1f; dt = FindAnyObjectByType<DriveThru>(); }
            }

            var car = dt != null && dt.isActiveAndEnabled ? dt.FrontAtWindow : null;
            var cam = Camera.main;
            bool offScreen = false;
            float vy = 0.5f;
            if (car != null && cam != null)
            {
                var v = cam.WorldToViewportPoint(dt.WindowPos + Vector3.up * 1f);
                offScreen = v.z < 0f || v.x < 0.04f || v.x > 0.96f || v.y < 0.04f || v.y > 0.96f;
                vy = Mathf.Clamp(v.y, 0.22f, 0.78f);
            }
            bool want = offScreen && !UIBlock.Active;

            // yeni araba cama yanaşınca (görünmüyorsa) kısa titreşim
            bool waiting = car != null;
            if (waiting && !wasWaiting && offScreen) Haptics.Light();
            wasWaiting = waiting;

            show = Mathf.MoveTowards(show, want ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            float s = show <= 0f ? 0f : EaseOutBack(show);
            if (show <= 0f) { root.localScale = Vector3.zero; return; }

            // ekranda camın yüksekliğini izler (yumuşak)
            float h = ((RectTransform)root.parent).rect.height;
            var target = new Vector2(14f, (vy - 0.5f) * h);
            root.anchoredPosition = Vector2.Lerp(root.anchoredPosition, target, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));

            pulse += Time.unscaledDeltaTime;
            float beat = 1f + 0.06f * Mathf.Max(0f, Mathf.Sin(pulse * 5f));
            root.localScale = Vector3.one * s;
            body.localScale = Vector3.one * beat;

            if (car != null)
            {
                ring.fillAmount = Mathf.Max(0.02f, car.Remaining);
                ring.color = car.TierColor;
                // sabır bitince direksiyon hafifçe sallanır (korna)
                float wob = car.Remaining <= 0f ? Mathf.Sin(pulse * 22f) * 12f : 0f;
                wheel.rectTransform.localRotation = Quaternion.Euler(0f, 0f, wob);
                int n = dt.CarCount;
                badge.SetActive(n > 1);
                if (n > 1) badgeText.text = n.ToString();
            }
        }

        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        // ---------------- yardımcılar ----------------

        static Image Img(string name, RectTransform parent, Sprite sp, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var im = go.GetComponent<Image>();
            im.sprite = sp;
            im.color = c;
            im.raycastTarget = false;
            return im;
        }

        static void Place(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        const int N = 128;

        static Sprite ToSprite(Color32[] px)
        {
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            t.SetPixels32(px);
            t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>Yumuşak kenarlı daire / halka (inner = iç yarıçap oranı).</summary>
        static Sprite MakeCircle(float outer, float inner)
        {
            var px = new Color32[N * N];
            float r = N * 0.5f;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r)) / r;
                    float a = Mathf.Clamp01((outer - d) * r);
                    if (inner > 0f) a *= Mathf.Clamp01((d - inner) * r);
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            return ToSprite(px);
        }

        /// <summary>Direksiyon: dış simit, göbek ve üç kol.</summary>
        static Sprite MakeWheel()
        {
            var px = new Color32[N * N];
            float r = N * 0.5f;
            Vector2 c = new Vector2(r, r);
            Vector2[] spokes = { new Vector2(-1f, 0f), new Vector2(1f, 0f), new Vector2(0f, -1f) };
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f) - c;
                    float d = p.magnitude / r;
                    // simit
                    float a = Mathf.Clamp01((0.98f - d) * r) * Mathf.Clamp01((d - 0.74f) * r);
                    // göbek
                    a = Mathf.Max(a, Mathf.Clamp01((0.24f - d) * r));
                    // kollar
                    foreach (var s in spokes)
                    {
                        float along = Vector2.Dot(p, s) / r;
                        if (along < 0f || along > 0.8f) continue;
                        float off = Mathf.Abs(p.x * s.y - p.y * s.x) / r;
                        a = Mathf.Max(a, Mathf.Clamp01((0.11f - off) * r));
                    }
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            return ToSprite(px);
        }

        /// <summary>Sola bakan yuvarlatılmış üçgen.</summary>
        static Sprite MakeTriangle()
        {
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N, v = (y + 0.5f) / N;
                    // uç solda (0.08, 0.5), taban sağda x = 0.95
                    float half = (u - 0.08f) / 0.87f * 0.5f;
                    float a = u > 0.95f ? 0f : Mathf.Clamp01((half - Mathf.Abs(v - 0.5f)) * N * 0.8f);
                    if (u < 0.08f) a = 0f;
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            return ToSprite(px);
        }
    }
}

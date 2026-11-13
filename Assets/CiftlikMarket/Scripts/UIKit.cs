using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CiftlikMarket
{
    /// <summary>
    /// Oyunun "şekerli" arayüz dili: kalın koyu kontur, altta koyu dudak (3D his), üstte parlama,
    /// her yer yuvarlak. Sprite'lar çalışma anında koddan çizilir (doku dosyası gerekmez).
    /// </summary>
    public static class UIKit
    {
        // ---------------- renkler ----------------
        public static readonly Color Ink = Hex("2B2D42");
        public static readonly Color Cream = Hex("FFF7E6");
        public static readonly Color CreamLip = Hex("EBCF9C");
        public static readonly Color Green = Hex("5BD15B");
        public static readonly Color GreenLip = Hex("2F9E3A");
        public static readonly Color Gray = Hex("BAC1CC");
        public static readonly Color GrayLip = Hex("8A92A0");
        public static readonly Color Gold = Hex("FFC93C");
        public static readonly Color GoldLip = Hex("DC9410");
        public static readonly Color Red = Hex("FF5A5F");
        public static readonly Color RedLip = Hex("C9343B");
        public static readonly Color Yellow = Hex("FFD84D");
        public static readonly Color YellowLip = Hex("D9A21B");
        public static readonly Color TextDark = Hex("3A2C1A");
        public static readonly Color TextSoft = Hex("7A6A55");

        public static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }
        public static Color Darker(Color c, float k = 0.72f) => new Color(c.r * k, c.g * k, c.b * k, c.a);

        // ---------------- fontlar ----------------
        static Font bold, semi;
        public static Font Bold => bold ? bold : (bold = Resources.Load<Font>("Fonts/Baloo2-ExtraBold") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
        public static Font Semi => semi ? semi : (semi = Resources.Load<Font>("Fonts/Baloo2-SemiBold") ?? Bold);

        /// <summary>Joystick'in yok sayacağı HUD butonları (dokunuş oyuncuyu yürütmesin).</summary>
        public static readonly List<RectTransform> HudBlockers = new List<RectTransform>();

        // ---------------- sprite'lar ----------------
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        const int RR = 128, RB = 62;   // yuvarlak köşe dokusu, 9-dilim kenarı

        /// <summary>9 dilimli yuvarlak köşe. Köşe yarıçapı (birim) = 62 / pixelsPerUnitMultiplier.</summary>
        public static Sprite Round => Get("round", () =>
        {
            var px = Canvas(RR, RR);
            FillRoundRect(px, RR, RR, 0, 0, RR, RR, RB, Color.white);
            return Make(px, RR, RR, new Vector4(RB + 1, RB + 1, RB + 1, RB + 1));
        });

        public static Sprite Circle => Get("circle", () =>
        {
            const int S = 256; var px = Canvas(S, S);
            FillCircle(px, S, S, S / 2f, S / 2f, S / 2f - 1f, Color.white);
            return Make(px, S, S, Vector4.zero);
        });

        /// <summary>Altın para: koyu kenar, parlak yüz, iç halka, parlama.</summary>
        public static Sprite Coin => Get("coin", () =>
        {
            const int S = 256; var px = Canvas(S, S); float c = S / 2f;
            FillCircle(px, S, S, c, c, 126, Hex("8A5A12"));
            FillCircle(px, S, S, c, c - 4, 118, Hex("E39A1A"));
            FillCircle(px, S, S, c, c + 4, 112, Hex("FFC83A"));
            FillCircle(px, S, S, c, c + 4, 86, Hex("F0A92A"));
            FillCircle(px, S, S, c, c + 6, 80, Hex("FFD45A"));
            FillEllipse(px, S, S, c - 42, c + 62, 34, 16, new Color(1, 1, 1, 0.55f), -30f);
            FillCircle(px, S, S, c + 58, c - 50, 9, new Color(1, 1, 1, 0.35f));
            return Make(px, S, S, Vector4.zero);
        });

        /// <summary>Dikey yumuşak geçiş (ekran zemini).</summary>
        public static Sprite Gradient(Color top, Color bottom) => Get("grad" + ColorUtility.ToHtmlStringRGB(top) + ColorUtility.ToHtmlStringRGB(bottom), () =>
        {
            const int W = 4, H = 256; var px = new Color[W * H];
            for (int y = 0; y < H; y++) { var col = Color.Lerp(bottom, top, y / (H - 1f)); for (int x = 0; x < W; x++) px[y * W + x] = col; }
            return Make(px, W, H, Vector4.zero);
        });

        public static Sprite ArrowUp => Get("arrowup", () => Poly(new[] { V(0.5f, 0.92f), V(0.9f, 0.5f), V(0.66f, 0.5f), V(0.66f, 0.1f), V(0.34f, 0.1f), V(0.34f, 0.5f), V(0.1f, 0.5f) }));
        public static Sprite ArrowLeft => Get("arrowleft", () => Poly(new[] { V(0.1f, 0.5f), V(0.52f, 0.9f), V(0.52f, 0.64f), V(0.9f, 0.64f), V(0.9f, 0.36f), V(0.52f, 0.36f), V(0.52f, 0.1f) }));
        public static Sprite PlayTri => Get("play", () => Poly(new[] { V(0.28f, 0.14f), V(0.86f, 0.5f), V(0.28f, 0.86f) }));
        public static Sprite Bolt => Get("bolt", () => Poly(new[] { V(0.62f, 0.95f), V(0.2f, 0.42f), V(0.47f, 0.42f), V(0.36f, 0.05f), V(0.82f, 0.6f), V(0.54f, 0.6f) }));
        public static Sprite Check => Get("check", () => Poly(new[] { V(0.08f, 0.52f), V(0.24f, 0.68f), V(0.40f, 0.50f), V(0.80f, 0.92f), V(0.94f, 0.76f), V(0.40f, 0.18f) }));
        public static Sprite Plus => Get("plus", () => Poly(new[] { V(0.38f, 0.1f), V(0.62f, 0.1f), V(0.62f, 0.38f), V(0.9f, 0.38f), V(0.9f, 0.62f), V(0.62f, 0.62f), V(0.62f, 0.9f), V(0.38f, 0.9f), V(0.38f, 0.62f), V(0.1f, 0.62f), V(0.1f, 0.38f), V(0.38f, 0.38f) }));

        public static Sprite Cross => Get("cross", () =>
        {
            const int S = 128; var px = Canvas(S, S);
            FillCapsule(px, S, S, V(0.25f, 0.25f) * S, V(0.75f, 0.75f) * S, 13, Color.white);
            FillCapsule(px, S, S, V(0.25f, 0.75f) * S, V(0.75f, 0.25f) * S, 13, Color.white);
            return Make(px, S, S, Vector4.zero);
        });

        /// <summary>Üst üste üç kasa (kapasite).</summary>
        public static Sprite Boxes => Get("boxes", () =>
        {
            const int S = 128; var px = Canvas(S, S);
            FillRoundRect(px, S, S, 14, 12, 50, 44, 8, Color.white);
            FillRoundRect(px, S, S, 64, 12, 50, 44, 8, Color.white);
            FillRoundRect(px, S, S, 39, 62, 50, 44, 8, Color.white);
            return Make(px, S, S, Vector4.zero);
        });

        /// <summary>Saat (sen yokken birikim).</summary>
        public static Sprite Clock => Get("clock", () =>
        {
            const int S = 128; var px = Canvas(S, S);
            FillCircle(px, S, S, 64, 64, 54, Color.white);
            FillCircle(px, S, S, 64, 64, 40, new Color(1, 1, 1, 0f), true);
            FillCapsule(px, S, S, new Vector2(64, 64), new Vector2(64, 92), 6, Color.white);
            FillCapsule(px, S, S, new Vector2(64, 64), new Vector2(86, 56), 6, Color.white);
            return Make(px, S, S, Vector4.zero);
        });

        /// <summary>Tablet simgesi (HUD butonu): çerçeve, ekran, ekranda yükselen çubuklar.</summary>
        public static Sprite Tablet => Get("tablet", () =>
        {
            const int W = 128, H = 160; var px = Canvas(W, H);
            FillRoundRect(px, W, H, 10, 6, 108, 148, 20, Hex("2E3440"));
            FillRoundRect(px, W, H, 20, 22, 88, 118, 10, Hex("8FDBFF"));
            FillRoundRect(px, W, H, 30, 34, 16, 34, 4, Hex("5BD15B"));
            FillRoundRect(px, W, H, 56, 34, 16, 56, 4, Hex("FFC93C"));
            FillRoundRect(px, W, H, 82, 34, 16, 80, 4, Hex("FF7A59"));
            FillCircle(px, W, H, 64, 146, 4, Hex("6B7383"));
            return Make(px, W, H, Vector4.zero);
        });

        /// <summary>Müzik notası (çift sekizlik).</summary>
        public static Sprite MusicNote => Get("note", () =>
        {
            const int S = 128; var px = Canvas(S, S);
            FillEllipse(px, S, S, 38, 34, 18, 14, Color.white, -20f);
            FillEllipse(px, S, S, 92, 44, 18, 14, Color.white, -20f);
            FillRoundRect(px, S, S, 48, 34, 10, 72, 3, Color.white);
            FillRoundRect(px, S, S, 102, 44, 10, 66, 3, Color.white);
            for (int k = 0; k < 2; k++) FillCapsule(px, S, S, new Vector2(53, 104 - k * 18), new Vector2(107, 108 - k * 18), 7, Color.white);
            return Make(px, S, S, Vector4.zero);
        });

        /// <summary>Hoparlör + ses dalgaları.</summary>
        public static Sprite Speaker => Get("speaker", () =>
        {
            const int S = 128; var px = Canvas(S, S);
            FillRoundRect(px, S, S, 14, 46, 26, 36, 5, Color.white);
            FillPoly(px, S, new[] { V(0.26f, 0.36f), V(0.52f, 0.14f), V(0.52f, 0.86f), V(0.26f, 0.64f) }, Color.white);
            for (int r = 0; r < 2; r++)
            {
                float rad = 22 + r * 18;
                for (int a = -40; a <= 40; a += 4)
                {
                    float an = a * Mathf.Deg2Rad, an2 = (a + 4) * Mathf.Deg2Rad;
                    FillCapsule(px, S, S, new Vector2(70 + Mathf.Cos(an) * rad, 64 + Mathf.Sin(an) * rad), new Vector2(70 + Mathf.Cos(an2) * rad, 64 + Mathf.Sin(an2) * rad), 5, Color.white);
                }
            }
            return Make(px, S, S, Vector4.zero);
        });

        /// <summary>Harita iğnesi (şubeler).</summary>
        public static Sprite Pin => Get("pin", () =>
        {
            const int S = 128; var px = Canvas(S, S);
            FillCircle(px, S, S, 64, 80, 38, Color.white);
            FillPoly(px, S, new[] { V(0.24f, 0.58f), V(0.76f, 0.58f), V(0.5f, 0.06f) }, Color.white);
            FillCircle(px, S, S, 64, 80, 15, new Color(1, 1, 1, 0), true);
            return Make(px, S, S, Vector4.zero);
        });

        /// <summary>Ayarlar dişlisi.</summary>
        public static Sprite Gear => Get("gear", () =>
        {
            const int S = 128; var px = Canvas(S, S);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = x + 0.5f - 64f, dy = y + 0.5f - 64f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy), a = Mathf.Atan2(dy, dx);
                    float tooth = Mathf.Clamp01((Mathf.Cos(a * 8f) - 0.15f) * 3f);   // 8 diş
                    float outer = 44f + 14f * tooth;
                    float cov = Mathf.Clamp01(outer - r + 0.5f) * Mathf.Clamp01(r - 18f + 0.5f);
                    Blend(px, y * S + x, Color.white, cov, false);
                }
            return Make(px, S, S, Vector4.zero);
        });

        /// <summary>Kilit simgesi.</summary>
        public static Sprite Lock => Get("lock", () =>
        {
            const int S = 128; var px = Canvas(S, S);
            FillCircle(px, S, S, 64, 78, 32, Color.white);
            FillCircle(px, S, S, 64, 78, 20, new Color(1, 1, 1, 0), true);
            FillRoundRect(px, S, S, 24, 10, 80, 64, 12, Color.white);
            return Make(px, S, S, Vector4.zero);
        });

        public static Sprite Icon(string name) => Get("icon_" + name, () =>
        {
            var s = Resources.Load<Sprite>("UI/Icons/" + name);
            if (s == null)
            {
                var t = Resources.Load<Texture2D>("UI/Icons/" + name);
                if (t != null) s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
            }
            return s;
        });

        static Sprite Get(string key, System.Func<Sprite> make)
        {
            if (cache.TryGetValue(key, out var s) && s) return s;
            s = make();
            cache[key] = s;
            return s;
        }

        static Vector2 V(float x, float y) => new Vector2(x, y);

        static Color[] Canvas(int w, int h) { var px = new Color[w * h]; var clear = new Color(1, 1, 1, 0); for (int i = 0; i < px.Length; i++) px[i] = clear; return px; }

        static Sprite Make(Color[] px, int w, int h, Vector4 border)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            t.SetPixels(px); t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        static void Blend(Color[] px, int i, Color c, float a, bool erase)
        {
            if (a <= 0f) return;
            if (erase) { px[i].a *= 1f - a; return; }
            var d = px[i]; float oa = a * c.a, na = oa + d.a * (1f - oa);
            if (na <= 0f) return;
            px[i] = new Color((c.r * oa + d.r * d.a * (1f - oa)) / na, (c.g * oa + d.g * d.a * (1f - oa)) / na, (c.b * oa + d.b * d.a * (1f - oa)) / na, na);
        }

        static void FillCircle(Color[] px, int w, int h, float cx, float cy, float r, Color c, bool erase = false)
        {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    Blend(px, y * w + x, c, Mathf.Clamp01(r - d + 0.5f), erase);
                }
        }

        static void FillEllipse(Color[] px, int w, int h, float cx, float cy, float rx, float ry, Color c, float rotDeg)
        {
            float cs = Mathf.Cos(rotDeg * Mathf.Deg2Rad), sn = Mathf.Sin(rotDeg * Mathf.Deg2Rad);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float u = (dx * cs + dy * sn) / rx, v = (-dx * sn + dy * cs) / ry;
                    float d = (Mathf.Sqrt(u * u + v * v) - 1f) * Mathf.Min(rx, ry);
                    Blend(px, y * w + x, c, Mathf.Clamp01(0.5f - d), false);
                }
        }

        static void FillRoundRect(Color[] px, int w, int h, float x0, float y0, float rw, float rh, float r, Color c)
        {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float px_ = x + 0.5f, py = y + 0.5f;
                    float qx = Mathf.Max(Mathf.Abs(px_ - (x0 + rw / 2f)) - (rw / 2f - r), 0f);
                    float qy = Mathf.Max(Mathf.Abs(py - (y0 + rh / 2f)) - (rh / 2f - r), 0f);
                    float d = Mathf.Sqrt(qx * qx + qy * qy) - r;
                    Blend(px, y * w + x, c, Mathf.Clamp01(0.5f - d), false);
                }
        }

        static void FillCapsule(Color[] px, int w, int h, Vector2 a, Vector2 b, float r, Color c)
        {
            Vector2 ab = b - a; float len2 = Mathf.Max(ab.sqrMagnitude, 1e-4f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                    float d = Vector2.Distance(p, a + ab * t) - r;
                    Blend(px, y * w + x, c, Mathf.Clamp01(0.5f - d), false);
                }
        }

        /// <summary>Normalize (0..1) köşelerden beyaz çokgen; 4x4 örnekleme ile yumuşak kenar.</summary>
        static Sprite Poly(Vector2[] pts)
        {
            const int S = 128; var px = Canvas(S, S);
            FillPoly(px, S, pts, Color.white);
            return Make(px, S, S, Vector4.zero);
        }

        static void FillPoly(Color[] px, int S, Vector2[] pts, Color col)
        {
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int hit = 0;
                    for (int sy = 0; sy < 4; sy++)
                        for (int sx = 0; sx < 4; sx++)
                            if (Inside(pts, new Vector2((x + (sx + 0.5f) / 4f) / S, (y + (sy + 0.5f) / 4f) / S))) hit++;
                    Blend(px, y * S + x, col, hit / 16f, false);
                }
        }

        static bool Inside(Vector2[] poly, Vector2 p)
        {
            bool c = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if (((poly[i].y > p.y) != (poly[j].y > p.y)) && (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)) c = !c;
            return c;
        }

        // ---------------- kurucu yardımcılar ----------------

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image Img(string name, Transform parent, Sprite sprite, Color col, float ppuMul = 1f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = col;
            img.raycastTarget = false;
            if (sprite != null && sprite.border != Vector4.zero) { img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = ppuMul; }
            return img;
        }

        /// <summary>Kalın çizgili, gölgeli oyun yazısı.</summary>
        public static Text Label(string name, Transform parent, string text, int size, Color col, TextAnchor anchor = TextAnchor.MiddleCenter, bool outline = false, bool semi = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = semi ? Semi : Bold;
            t.text = text; t.fontSize = size; t.color = col; t.alignment = anchor;
            t.raycastTarget = false; t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 0.85f;
            if (outline)
            {
                var o = go.AddComponent<Outline>(); o.effectColor = Ink; o.effectDistance = new Vector2(3.5f, -3.5f);
                var o2 = go.AddComponent<Outline>(); o2.effectColor = Ink; o2.effectDistance = new Vector2(-3.5f, 3.5f);
                var s = go.AddComponent<Shadow>(); s.effectColor = Ink; s.effectDistance = new Vector2(0, -7f);
            }
            return t;
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        public static void Stretch(RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }

        /// <summary>
        /// "Tombul" kutu: koyu kontur + altta koyu dudak + açık yüz + üstte parlama.
        /// Dönen Chunk'ın Face'ine içerik eklenir. radius birim cinsinden köşe yarıçapı (büyükse hap/daire olur).
        /// </summary>
        public static Chunk MakeChunk(string name, Transform parent, Color fill, Color lip, float radius, float outlineW = 6f, float lipH = 12f, bool shine = true)
        {
            var c = new Chunk();
            float mul = RB / Mathf.Max(radius, 1f);
            c.Root = Img(name, parent, Round, Ink, mul);
            var lipImg = Img("Lip", c.Root.transform, Round, lip, RB / Mathf.Max(radius - outlineW, 1f));
            Stretch(lipImg.rectTransform, outlineW, outlineW, outlineW, outlineW);
            c.Face = Img("Face", c.Root.transform, Round, fill, RB / Mathf.Max(radius - outlineW, 1f));
            Stretch(c.Face.rectTransform, outlineW, outlineW + lipH, outlineW, outlineW);
            c.LipH = lipH; c.OutlineW = outlineW;
            if (shine)
            {
                var sh = Img("Shine", c.Face.transform, Round, new Color(1, 1, 1, 0.28f), RB / Mathf.Max(radius * 0.6f, 1f));
                var rt = sh.rectTransform;
                rt.anchorMin = new Vector2(0, 0.55f); rt.anchorMax = new Vector2(1, 1);
                rt.offsetMin = new Vector2(Mathf.Min(radius * 0.45f, 26f), 0); rt.offsetMax = new Vector2(-Mathf.Min(radius * 0.45f, 26f), -7f);
            }
            return c;
        }

        /// <summary>Tıklanabilir tombul buton: basınca yüzü aşağı iner.</summary>
        public static Chunk MakeButton(string name, Transform parent, Color fill, Color lip, float radius, System.Action onClick, float outlineW = 6f, float lipH = 12f)
        {
            var c = MakeChunk(name, parent, fill, lip, radius, outlineW, lipH);
            c.Root.raycastTarget = true;
            var b = c.Root.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { if (b.interactable) onClick?.Invoke(); });
            c.Button = b;
            var p = c.Root.gameObject.AddComponent<ChunkPress>();
            p.face = c.Face.rectTransform; p.depth = lipH * 0.75f;
            return c;
        }

        /// <summary>Kendi tuvalini kurar (ekran üstü, 1080x1920 referans).</summary>
        public static Canvas MakeCanvas(string name, Transform parent, int order, bool raycast = true)
        {
            var go = raycast ? new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
                             : new GameObject(name, typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(parent, false);
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var cs = go.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;
            return c;
        }

        /// <summary>
        /// Açılır pencere: karartma + krem tombul kart + kartın üst kenarına oturan renkli başlık şeridi.
        /// İçerik Popup.Body'ye eklenir (kartın yüzü).
        /// </summary>
        public static Popup MakePopup(Transform parent, string name, string title, Color col, Color lip, Vector2 size, float yOffset = 0f)
        {
            var p = new Popup();
            p.Dim = Img(name, parent, null, new Color(0.08f, 0.06f, 0.12f, 0.62f));
            p.Dim.raycastTarget = true;
            Stretch(p.Dim.rectTransform);
            p.Card = Node("Card", p.Dim.transform);
            Place(p.Card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, yOffset), size);
            p.Card.gameObject.AddComponent<PopupFit>();   // dar ekranlarda çarpı düğmesi dışarı taşmasın
            var body = MakeChunk("Body", p.Card, Cream, CreamLip, 70f, 8f, 18f, false);
            Stretch(body.Rect);
            body.Root.raycastTarget = true;
            body.Root.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;   // karta dokunmak kapatmasın
            p.Body = body.Face.rectTransform;
            p.Banner = MakeChunk("Banner", p.Card, col, lip, 60f, 8f, 14f);
            Place(p.Banner.Rect, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(Mathf.Min(620f, size.x - 120f), 126));
            p.Title = Label("Title", p.Banner.Face.transform, title, 58, Color.white, TextAnchor.MiddleCenter, true);
            Stretch(p.Title.rectTransform, 10, -4, 10, 0);
            return p;
        }

        public static void SetChunkColor(Chunk c, Color fill, Color lip)
        {
            c.Face.color = fill;
            c.Root.transform.GetChild(0).GetComponent<Image>().color = lip;
        }

        public static string Money(int v)
        {
            if (v >= 1000000) return (v / 1000000f).ToString(v >= 10000000 ? "0" : "0.#") + "M";
            if (v >= 100000) return (v / 1000).ToString() + "K";
            return v.ToString("#,0", System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
        }
    }

    public class Popup
    {
        public Image Dim;
        public RectTransform Card, Body;
        public Chunk Banner;
        public Text Title;
    }

    public class Chunk
    {
        public Image Root, Face;
        public Button Button;
        public float LipH, OutlineW;
        public RectTransform Rect => Root.rectTransform;
    }

    /// <summary>Basılınca yüzü dudağın içine indirir (fiziksel buton hissi).</summary>
    public class ChunkPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public RectTransform face;
        public float depth = 8f;
        bool down;
        Vector2 baseMin, baseMax;
        bool init;

        void Init() { if (init || !face) return; init = true; baseMin = face.offsetMin; baseMax = face.offsetMax; }

        public void OnPointerDown(PointerEventData e) { Init(); AudioFx.Play(Sfx.Tap, 0.7f); if (!face) return; down = true; face.offsetMin = baseMin - new Vector2(0, depth); face.offsetMax = baseMax - new Vector2(0, depth); }
        public void OnPointerUp(PointerEventData e) { Release(); }
        public void OnPointerExit(PointerEventData e) { Release(); }
        void OnDisable() { Release(); }
        void Release() { if (!down || !face) return; down = false; face.offsetMin = baseMin; face.offsetMax = baseMax; }
    }

    /// <summary>Hafif zıplama / nabız (UI, gerçek zamandan bağımsız).</summary>
    public class UIPunch : MonoBehaviour
    {
        float t = 1f, amount;
        public void Punch(float a = 0.15f) { amount = a; t = 0f; }
        void Update()
        {
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / 0.28f);
            float s = 1f + amount * Mathf.Sin(t * Mathf.PI) * (1f - t * 0.4f);
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}

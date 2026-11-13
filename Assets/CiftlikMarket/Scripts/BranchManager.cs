using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CiftlikMarket
{
    /// <summary>
    /// Çoklu market (şube) sistemi. Yeni şube açmak hiçbir şeyi sıfırlamaz: eski market olduğu gibi kalır ve
    /// sen yokken kazanmaya devam eder; para ortaktır. Her şube ayrı bir bölgededir (ortam, renkler, hava, gelir çarpanı).
    /// Şube kayıtları SaveKeys ile ayrılır; "cmg_" anahtarları (şube sayısı, bulunulan şube, ses) ortaktır.
    /// </summary>
    public class BranchManager : MonoBehaviour
    {
        public static BranchManager I;

        public enum Weather { Clear, Rain }

        public class Theme
        {
            public string name, shortName;
            public Color grass, floor, stripe, path, fog, light;
            public float mult;
            public bool ready;
            public Weather weather;
            public Theme(string n, string sn, string g, string f, string s, string pth, float m, bool r, Weather w = Weather.Clear, string fogHex = null, string lightHex = "FFFFFF")
            {
                name = n; shortName = sn; grass = Hex(g); floor = Hex(f); stripe = Hex(s); path = Hex(pth); mult = m; ready = r; weather = w;
                fog = fogHex != null ? Hex(fogHex) : Color.clear; light = Hex(lightHex);
            }
        }

        public static readonly Theme[] Themes =
        {
            new Theme("MERKEZ ŞUBE", "Merkez", "6CC23E", "F5D7A1", "4A90E2", "CF9663", 1f, true),
            new Theme("KARADENİZ ŞUBESİ", "Karadeniz", "3E9A3C", "E3CDA8", "2E7D32", "8F6342", 1.5f, true, Weather.Rain, "AFC4BC", "D9E4EC"),
            new Theme("EGE ŞUBESİ", "Ege", "A9C46C", "F3E7CF", "00897B", "D9B98C", 2f, false),
            new Theme("KAPADOKYA ŞUBESİ", "Kapadokya", "C9B97E", "F2DCB8", "D84315", "C79A6B", 2.5f, false),
        };

        /// <summary>Şu an bulunulan şube (1'den başlar).</summary>
        public static int Branch => Mathf.Clamp(PlayerPrefs.GetInt("cmg_branch", 1), 1, Themes.Length);

        /// <summary>Sahip olunan şube sayısı.</summary>
        public static int Owned => Mathf.Clamp(Mathf.Max(PlayerPrefs.GetInt("cmg_branches", 1), Branch), 1, Themes.Length);

        public static Theme Current => Themes[Branch - 1];
        public static float IncomeMultiplier => Current.mult;
        public static string BranchName => Current.name;
        public static bool CanOpenNext => Owned < Themes.Length && Themes[Owned].ready;
        public static string NextBranchName => Owned < Themes.Length ? Themes[Owned].name : "";

        public static string MultText(float m) => "×" + m.ToString(m % 1f == 0f ? "0" : "0.0", System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));

        GameObject confirmPanel, toast;
        Text confirmBody;
        float toastTimer;

        void Awake()
        {
            I = this;
            BuildUI();
        }

        void Start()
        {
            ApplyTheme();
            if (PlayerPrefs.GetInt("cmg_justopened", 0) == 1)
            {
                PlayerPrefs.SetInt("cmg_justopened", 0);
                ShowToast(BranchName + " AÇILDI!\nBurada satışlar " + MultText(IncomeMultiplier));
            }
        }

        void Update()
        {
            if (toast && toast.activeSelf)
            {
                toastTimer -= Time.unscaledDeltaTime;
                if (toastTimer <= 0f) toast.SetActive(false);
            }
        }

        // ---------------- Tema / ortam ----------------

        /// <summary>Şubenin bölgesine göre zemin, çimen, patika, duvar şeridi renkleri; hava ve ışık (ortak materyallere dokunmadan).</summary>
        void ApplyTheme()
        {
            if (Branch == 1) return;
            var t = Current;
            var mpb = new MaterialPropertyBlock();
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                var m = r.sharedMaterial;
                if (m == null) continue;
                string n = m.name;
                Color? c = n.StartsWith("Cim") ? t.grass : n.StartsWith("Zemin") ? t.floor : n.StartsWith("DuvarSerit") ? t.stripe
                         : n.StartsWith("Patika") ? t.path : (Color?)null;
                if (c == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", c.Value);
                r.SetPropertyBlock(mpb);
            }
            var cam = Camera.main;
            if (cam) cam.backgroundColor = t.fog.a > 0f ? Color.Lerp(t.grass, t.fog, 0.55f) : t.grass * 0.9f;
            RegionEnvironment.Apply(t);
        }

        // ---------------- Yeni şube ve gezinme ----------------

        /// <summary>Onay penceresini göster (tabletteki "YENİ ŞUBE AÇ").</summary>
        public void AskOpenNewBranch(int cost, Action onConfirmed)
        {
            if (!CanOpenNext) return;
            var next = Themes[Owned];
            pendingConfirm = onConfirmed;
            pendingCost = cost;
            confirmBody.text = "<size=46><b>" + next.name + "</b></size>\nAçılsın mı?\n\n" +
                               "<color=#7A6A55>" + Current.shortName + " marketin olduğu gibi kalır ve\nsen yokken kazanmaya devam eder.</color>";
            confirmReward.text = next.shortName + "'de satışlar <b>" + MultText(next.mult) + "</b> değerli";
            confirmPanel.SetActive(true);
            AudioFx.Play(Sfx.Popup, 0.8f);
            if (confirmCard && confirmCard.GetComponent<PopAnim>() == null) confirmCard.gameObject.AddComponent<PopAnim>();
            IsConfirmOpen = true;
        }

        public static bool IsConfirmOpen { get; private set; }
        Action pendingConfirm;
        int pendingCost;

        void OnConfirm()
        {
            confirmPanel.SetActive(false);
            IsConfirmOpen = false;
            if (GameManager.I == null || GameManager.I.Money < pendingCost) { pendingConfirm = null; return; }
            GameManager.I.Spend(pendingCost);     // para ortak: şubenin bedeli ödenir
            pendingConfirm?.Invoke();
            pendingConfirm = null;
        }

        void OnCancel()
        {
            confirmPanel.SetActive(false);
            IsConfirmOpen = false;
            pendingConfirm = null;
        }

        /// <summary>Yeni şubeyi açar ve oraya gider. Eski şubenin kaydı aynen kalır.</summary>
        public static void OpenNewBranch()
        {
            if (!CanOpenNext) return;
            int next = Owned + 1;
            PlayerPrefs.SetInt("cmg_branches", next);
            PlayerPrefs.SetInt("cmg_justopened", 1);
            TravelTo(next);
        }

        /// <summary>Başka bir şubeye git: bu şubeyi kaydet, kısa bir geçişle sahneyi o şubenin kaydıyla yeniden yükle.</summary>
        public static void TravelTo(int branch)
        {
            branch = Mathf.Clamp(branch, 1, Owned);
            if (GameManager.I) GameManager.I.Save();          // para + bu şubenin stoğu ve zamanı
            PlayerPrefs.SetInt("cmg_branch", branch);
            PlayerPrefs.Save();
            TravelFade.Go(Themes[branch - 1].name, () =>
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0 ? SceneManager.GetActiveScene().buildIndex : 0));
        }

        // ---------------- Arayüz ----------------

        void ShowToast(string msg)
        {
            toastText.text = msg;
            toast.SetActive(true);
            if (toast.GetComponent<PopAnim>() == null) toast.AddComponent<PopAnim>();
            toastTimer = 4f;
            AudioFx.Play(Sfx.Unlock);
        }

        Text toastText;

        void BuildUI()
        {
            var canvas = UIKit.MakeCanvas("BranchCanvas", transform, 45);

            // Sol üst: şube rozeti (krem hap + solda yeşil disk içinde market tabelası)
            var badge = UIKit.Node("BranchBadge", canvas.transform);
            UIKit.Place(badge, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -36), new Vector2(320, 80));
            var pill = UIKit.MakeChunk("Pill", badge, UIKit.Cream, UIKit.CreamLip, 38f, 5f, 8f);
            UIKit.Stretch(pill.Rect, 36, 3, 0, 3);
            string bonus = IncomeMultiplier > 1f ? "  <color=#2F9E3A>" + MultText(IncomeMultiplier) + "</color>" : "";
            var bt = UIKit.Label("Text", pill.Face.transform, BranchName + bonus, 30, UIKit.TextDark, TextAnchor.MiddleLeft);
            UIKit.Stretch(bt.rectTransform, 54, -3, 14, 0);
            bt.resizeTextForBestFit = true; bt.resizeTextMinSize = 18; bt.resizeTextMaxSize = 30;
            bt.horizontalOverflow = HorizontalWrapMode.Wrap; bt.verticalOverflow = VerticalWrapMode.Truncate;
            var disc = UIKit.MakeChunk("Disc", badge, UIKit.Hex("4CC97A"), UIKit.Hex("2B9A55"), 42f, 5f, 6f);
            UIKit.Place(disc.Rect, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(84, 88));
            var di = UIKit.Img("Inner", disc.Face.transform, UIKit.Circle, new Color(1, 1, 1, 0.95f));
            UIKit.Stretch(di.rectTransform, 6, 6, 6, 6);
            var icon = UIKit.Img("Icon", di.transform, UIKit.Icon("tabela"), Color.white);
            icon.preserveAspect = true;
            UIKit.Stretch(icon.rectTransform, 2, 2, 2, 2);

            // Toast (şube açıldı)
            var t = UIKit.MakeChunk("Toast", canvas.transform, UIKit.Green, UIKit.GreenLip, 70f, 8f, 16f);
            UIKit.Place(t.Rect, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880, 240));
            toast = t.Root.gameObject;
            toastText = UIKit.Label("Text", t.Face.transform, "", 48, Color.white, TextAnchor.MiddleCenter, true);
            UIKit.Stretch(toastText.rectTransform, 30, 0, 30, 0);
            toast.SetActive(false);

            // Onay penceresi
            var p = UIKit.MakePopup(canvas.transform, "Confirm", "YENİ ŞUBE", UIKit.Hex("FF8A3D"), UIKit.Hex("D25E17"), new Vector2(920, 960));
            confirmPanel = p.Dim.gameObject;
            confirmCard = p.Card;

            var store = UIKit.MakeChunk("Store", p.Body, UIKit.Hex("4CC97A"), UIKit.Hex("2B9A55"), 90f, 7f, 10f);
            UIKit.Place(store.Rect, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -92), new Vector2(180, 190));
            var si = UIKit.Img("Inner", store.Face.transform, UIKit.Circle, new Color(1, 1, 1, 0.95f));
            UIKit.Stretch(si.rectTransform, 12, 12, 12, 12);
            var sIcon = UIKit.Img("Icon", si.transform, UIKit.Icon("tabela"), Color.white);
            sIcon.preserveAspect = true;
            UIKit.Stretch(sIcon.rectTransform, 4, 4, 4, 4);

            confirmBody = UIKit.Label("Body", p.Body, "", 38, UIKit.TextDark, TextAnchor.UpperCenter, false, true);
            var cbr = confirmBody.rectTransform;
            cbr.anchorMin = new Vector2(0, 0); cbr.anchorMax = new Vector2(1, 1);
            cbr.offsetMin = new Vector2(50, 360); cbr.offsetMax = new Vector2(-50, -300);

            var reward = UIKit.MakeChunk("Reward", p.Body, UIKit.Hex("E6F7E1"), UIKit.Hex("BFE3B5"), 40f, 5f, 8f, false);
            UIKit.Place(reward.Rect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 196), new Vector2(760, 130));
            confirmReward = UIKit.Label("Text", reward.Face.transform, "", 38, UIKit.Hex("2F7D32"), TextAnchor.MiddleCenter);
            UIKit.Stretch(confirmReward.rectTransform, 16, -4, 16, 0);

            var cancel = UIKit.MakeButton("Cancel", p.Body, UIKit.Gray, UIKit.GrayLip, 60f, OnCancel, 6f, 12f);
            UIKit.Place(cancel.Rect, new Vector2(0.27f, 0), new Vector2(0.5f, 0), new Vector2(0, 36), new Vector2(370, 136));
            UIKit.Stretch(UIKit.Label("T", cancel.Face.transform, "VAZGEÇ", 50, Color.white, TextAnchor.MiddleCenter, true).rectTransform, 0, -4, 0, 0);

            var ok = UIKit.MakeButton("Ok", p.Body, UIKit.Green, UIKit.GreenLip, 60f, OnConfirm, 6f, 12f);
            UIKit.Place(ok.Rect, new Vector2(0.73f, 0), new Vector2(0.5f, 0), new Vector2(0, 36), new Vector2(370, 136));
            UIKit.Stretch(UIKit.Label("T", ok.Face.transform, "ŞUBEYİ AÇ", 50, Color.white, TextAnchor.MiddleCenter, true).rectTransform, 0, -4, 0, 0);
            confirmPanel.SetActive(false);
        }

        RectTransform confirmCard;
        Text confirmReward;

        static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }
    }
}

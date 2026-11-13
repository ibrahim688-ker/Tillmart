using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CiftlikMarket
{
    public enum QuestType
    {
        ServeCustomers, StockShelf, DriveThruOrders, Harvest, BuyUpgrades, EarnMoney,
        SellProduct,     // belirli bir üründen N tane sat (ör. 10 yoğurt)
        CollectProduct,  // makineden / hayvandan belirli üründen N tane al (ör. 15 süt)
        FeedAnimals,     // ineklere / tavuklara N yem koy
        LoadMachines,    // makinelere N girdi koy (yoğurt, mandıra, değirmen...)
        HappyCars,       // N arabayı sabrı yeşildeyken (tam bahşişle) gönder
        CashierSelf,     // kasada kendin durup N müşterinin parasını al
        RushSales,       // yoğun saatte N satış yap
        UnlockArea,      // N yeni alan / makine / raf aç
    }

    /// <summary>
    /// Günlük görevler (GDD 3.12): her gün 3 görev. Oyun içi olaylar Report() ile sayılır
    /// (kasada ödeme, rafa koyma, drive-thru siparişi, tarladan toplama, yükseltme, satış geliri).
    /// Biten görevin ödülü panelden alınır; üçü de alınınca ek "sandık" ödülü. Gece yarısı yenilenir.
    /// Hedefler ve ödüller oyuncunun ilerlemesine (açılan alan sayısı, kazanç hızı) göre ölçeklenir.
    /// Kayıt bütün şubeler için ortaktır (cmg_ anahtarları).
    /// </summary>
    public class Quests : MonoBehaviour
    {
        public static Quests I;
        public static bool IsOpen { get; private set; }
        const int Count = 3;

        class Quest { public QuestType type; public ItemType item; public int target, progress, reward; public bool claimed; }
        readonly Quest[] quests = new Quest[Count];
        bool bonusClaimed;
        int bonusReward;
        string date;
        bool dirty;
        float saveTimer;

        GameObject panel, hudRoot;
        RectTransform hudBadge;
        UIPunch hudPunch;
        Text timerText, bonusText;
        Chunk bonusBtn;
        Text bonusBtnText;
        Image bonusFill;
        readonly Row[] rows = new Row[Count];

        class Row { public Text title, prog, btnText; public Image icon, fill, check; public Chunk btn; public RectTransform fillRt; public UIPunch punch; }

        static string Today => DateTime.Now.ToString("yyyyMMdd");
        static bool TutorialDone => PlayerPrefs.GetInt("cmg_tut_done", 0) == 1;

        /// <summary>Oyun olayını görev ilerlemesine ekler (görev yoksa bir şey yapmaz).</summary>
        public static void Report(QuestType t, int amount = 1)
        {
            if (I == null || amount <= 0) return;
            I.Add(t, amount, null);
        }

        /// <summary>Ürüne bağlı görevler için (ürün satışı, makineden ürün alma).</summary>
        public static void Report(QuestType t, ItemType item, int amount = 1)
        {
            if (I == null || amount <= 0) return;
            I.Add(t, amount, item);
        }

        void Awake()
        {
            I = this;
            for (int i = 0; i < Count; i++) quests[i] = new Quest();
            Load();
            BuildUI();
            panel.SetActive(false);
        }

        void Start() { if (date != Today) Generate(); }

        void Update()
        {
            if (date != Today) { Generate(); if (IsOpen) Refresh(); }   // gece yarısı oyun açıkken de yenilensin

            bool show = TutorialDone && !OfficeUI.IsOpen;
            if (hudRoot.activeSelf != show) hudRoot.SetActive(show);
            if (show)
            {
                bool claimable = AnyClaimable();
                if (hudBadge.gameObject.activeSelf != claimable) hudBadge.gameObject.SetActive(claimable);
                if (claimable) hudBadge.localScale = Vector3.one * (1f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 0.14f);
            }

            if (IsOpen) UpdateTimer();

            if (dirty)
            {
                saveTimer += Time.unscaledDeltaTime;
                if (saveTimer > 3f) Save();
            }
        }

        void OnApplicationPause(bool p) { if (p && dirty) Save(); }
        void OnDestroy() { if (dirty) Save(); }

        // ---------------- veri ----------------

        void Add(QuestType t, int amount, ItemType? item)
        {
            if (date != Today) Generate();
            for (int i = 0; i < Count; i++)
            {
                var q = quests[i];
                if (q.type != t || q.claimed || q.progress >= q.target) continue;
                if (HasItem(q.type) && (!item.HasValue || item.Value != q.item)) continue;
                q.progress = Mathf.Min(q.target, q.progress + amount);
                dirty = true;
                if (q.progress >= q.target)
                {
                    // görev bitti: butonu zıplat, kısa ses
                    AudioFx.Play(Sfx.LevelUp, 0.5f);
                    Haptics.Light();
                    if (hudPunch) hudPunch.Punch(0.25f);
                    Save();
                }
                if (IsOpen) Refresh();
            }
        }

        bool AnyClaimable()
        {
            int claimed = 0;
            foreach (var q in quests)
            {
                if (!q.claimed && q.progress >= q.target) return true;
                if (q.claimed) claimed++;
            }
            return claimed == Count && !bonusClaimed;
        }

        void Load()
        {
            date = PlayerPrefs.GetString("cmg_q_date", "");
            for (int i = 0; i < Count; i++)
            {
                var q = quests[i];
                q.type = (QuestType)PlayerPrefs.GetInt("cmg_q" + i + "_type", 0);
                q.target = PlayerPrefs.GetInt("cmg_q" + i + "_target", 10);
                q.progress = PlayerPrefs.GetInt("cmg_q" + i + "_prog", 0);
                q.reward = PlayerPrefs.GetInt("cmg_q" + i + "_reward", 100);
                q.claimed = PlayerPrefs.GetInt("cmg_q" + i + "_claimed", 0) == 1;
                q.item = (ItemType)PlayerPrefs.GetInt("cmg_q" + i + "_item", 0);
            }
            bonusClaimed = PlayerPrefs.GetInt("cmg_q_bonus", 0) == 1;
            bonusReward = PlayerPrefs.GetInt("cmg_q_bonus_reward", 500);
        }

        void Save()
        {
            dirty = false;
            saveTimer = 0f;
            PlayerPrefs.SetString("cmg_q_date", date);
            for (int i = 0; i < Count; i++)
            {
                var q = quests[i];
                PlayerPrefs.SetInt("cmg_q" + i + "_type", (int)q.type);
                PlayerPrefs.SetInt("cmg_q" + i + "_target", q.target);
                PlayerPrefs.SetInt("cmg_q" + i + "_prog", q.progress);
                PlayerPrefs.SetInt("cmg_q" + i + "_reward", q.reward);
                PlayerPrefs.SetInt("cmg_q" + i + "_claimed", q.claimed ? 1 : 0);
                PlayerPrefs.SetInt("cmg_q" + i + "_item", (int)q.item);
            }
            PlayerPrefs.SetInt("cmg_q_bonus", bonusClaimed ? 1 : 0);
            PlayerPrefs.SetInt("cmg_q_bonus_reward", bonusReward);
            PlayerPrefs.Save();
        }

        static int Round(int v) => v < 1000 ? Mathf.Max(10, Mathf.RoundToInt(v / 10f) * 10) : Mathf.RoundToInt(v / 50f) * 50;

        /// <summary>Bugünün 3 görevini seçer (aynı tür iki kez çıkmaz; drive-thru açık değilse o görev gelmez).</summary>
        void Generate()
        {
            date = Today;
            int u = 0;
            string unlocked = PlayerPrefs.GetString(SaveKeys.P("cm_unlocked"), "");
            if (unlocked.Length > 0) u = unlocked.Split(',').Length;
            float rate = OfflineEarnings.I != null ? OfflineEarnings.I.RatePerMinute : 0f;

            // o an yapılabilecek görevlerden havuz kur
            var sellTypes = new List<ItemType>();
            foreach (var sh in Shelf.All) if (!sellTypes.Contains(sh.type)) sellTypes.Add(sh.type);
            var outTypes = new List<ItemType>();
            bool animals = false, machines = false;
            foreach (var c in Converter.All)
            {
                if (!c.isActiveAndEnabled) continue;
                if (!outTypes.Contains(c.output)) outTypes.Add(c.output);
                if (IsAnimal(c)) animals = true; else machines = true;
            }
            bool unlockLeft = false;
            foreach (var z in FindObjectsByType<UnlockZone>(FindObjectsSortMode.None)) if (z.isActiveAndEnabled) { unlockLeft = true; break; }
            foreach (var z in UnlockZone.OfficeItems) if (z.RequirementsMet && !z.opensNewBranch) { unlockLeft = true; break; }
            bool driveThru = GameManager.I != null && GameManager.I.IsUnlocked("drive_thru");
            bool cashierHired = GameManager.I != null && GameManager.I.IsUnlocked("kasiyer");

            var pool = new List<QuestType> { QuestType.ServeCustomers, QuestType.StockShelf, QuestType.Harvest, QuestType.BuyUpgrades, QuestType.EarnMoney, QuestType.CashierSelf };
            if (driveThru) { pool.Add(QuestType.DriveThruOrders); pool.Add(QuestType.HappyCars); }
            if (sellTypes.Count > 0) pool.Add(QuestType.SellProduct);
            if (outTypes.Count > 0) pool.Add(QuestType.CollectProduct);
            if (animals) pool.Add(QuestType.FeedAnimals);
            if (machines) pool.Add(QuestType.LoadMachines);
            if (Shelf.All.Count >= 2) pool.Add(QuestType.RushSales);
            if (unlockLeft) pool.Add(QuestType.UnlockArea);
            if (cashierHired) pool.Remove(QuestType.CashierSelf);   // kasiyer varken oyuncu kasada pek durmaz
            for (int i = pool.Count - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); var t = pool[i]; pool[i] = pool[j]; pool[j] = t; }

            for (int i = 0; i < Count; i++)
            {
                var q = quests[i];
                q.type = pool[i % pool.Count];
                q.progress = 0;
                q.claimed = false;
                q.item = ItemType.Sut;
                float minutes;   // görevin kabaca kaç dakikalık oyun olduğu (ödül = kazanç hızı × dakika)
                switch (q.type)
                {
                    case QuestType.ServeCustomers: q.target = Mathf.Min(60, 10 + 2 * u); minutes = 5f; break;
                    case QuestType.StockShelf: q.target = Mathf.Min(120, 20 + 4 * u); minutes = 5f; break;
                    case QuestType.DriveThruOrders: q.target = Mathf.Min(12, 3 + u / 4); minutes = 6f; break;
                    case QuestType.Harvest: q.target = Mathf.Min(100, 20 + 3 * u); minutes = 4f; break;
                    case QuestType.BuyUpgrades: q.target = Mathf.Min(6, 2 + u / 8); minutes = 4f; break;
                    case QuestType.SellProduct:
                        q.item = sellTypes[UnityEngine.Random.Range(0, sellTypes.Count)];
                        q.target = Mathf.Min(30, 5 + u / 2); minutes = 5f; break;
                    case QuestType.CollectProduct:
                        q.item = outTypes[UnityEngine.Random.Range(0, outTypes.Count)];
                        q.target = Mathf.Min(40, 8 + u); minutes = 4f; break;
                    case QuestType.FeedAnimals: q.target = Mathf.Min(80, 15 + 2 * u); minutes = 4f; break;
                    case QuestType.LoadMachines: q.target = Mathf.Min(60, 10 + 2 * u); minutes = 5f; break;
                    case QuestType.HappyCars: q.target = Mathf.Min(8, 2 + u / 5); minutes = 6f; break;
                    case QuestType.CashierSelf: q.target = Mathf.Min(20, 5 + u / 3); minutes = 3f; break;
                    case QuestType.RushSales: q.target = Mathf.Min(20, 5 + u / 4); minutes = 5f; break;
                    case QuestType.UnlockArea: q.target = 1; minutes = 6f; break;
                    default: q.target = Round(Mathf.Max(300, (int)(rate * 12f))); minutes = 4f; break;   // EarnMoney
                }
                q.reward = Round(Mathf.Max(150, (int)(rate * minutes)));
            }
            bonusClaimed = false;
            bonusReward = Round(Mathf.Max(500, (int)(rate * 12f)));
            Save();
        }

        static bool HasItem(QuestType t) => t == QuestType.SellProduct || t == QuestType.CollectProduct;

        /// <summary>İnek ve kümes (yem yiyen hayvanlar); diğer dönüştürücüler makinedir.</summary>
        public static bool IsAnimal(Converter c) => c.input == ItemType.Saman || (c.input == ItemType.Misir && c.output == ItemType.Yumurta);

        static string Describe(Quest q)
        {
            int n = q.target;
            string item = GameConfig.I != null ? GameConfig.I.Name(q.item) : q.item.ToString();
            switch (q.type)
            {
                case QuestType.ServeCustomers: return n + " müşteriye satış yap";
                case QuestType.StockShelf: return "Raflara " + n + " ürün koy";
                case QuestType.DriveThruOrders: return n + " drive-thru siparişi tamamla";
                case QuestType.Harvest: return "Tarlalardan " + n + " ürün topla";
                case QuestType.BuyUpgrades: return n + " yükseltme al";
                case QuestType.SellProduct: return n + " " + item + " sat";
                case QuestType.CollectProduct: return n + " " + item + " üret ve topla";
                case QuestType.FeedAnimals: return "Hayvanlara " + n + " yem ver";
                case QuestType.LoadMachines: return "Makinelere " + n + " ürün koy";
                case QuestType.HappyCars: return n + " arabayı beklemeden gönder (yeşil süre)";
                case QuestType.CashierSelf: return "Kasada dur, " + n + " müşterinin parasını al";
                case QuestType.RushSales: return "Yoğun saatte " + n + " satış yap";
                case QuestType.UnlockArea: return n == 1 ? "Yeni bir alan aç" : n + " yeni alan aç";
                default: return "Satışlardan " + UIKit.Money(n) + " kazan";
            }
        }

        static string ProducerIcon(ItemType t)
        {
            switch (t)
            {
                case ItemType.Sut: return "inek_1";
                case ItemType.Yumurta: return "kumes_1";
                case ItemType.Yogurt: return "makine_yogurt";
                case ItemType.Ayran: return "makine_ayran";
                case ItemType.Peynir: return "makine_peynir";
                case ItemType.Tereyag: return "makine_tereyag";
                case ItemType.Un: return "degirmen";
                case ItemType.Pogaca: return "firin";
                case ItemType.Salca: return "makine_salca";
                default: return "inek_1";
            }
        }

        static Sprite IconOf(Quest q)
        {
            switch (q.type)
            {
                case QuestType.ServeCustomers: return UIKit.Icon("calisan_kasiyer");
                case QuestType.StockShelf: return UIKit.Icon("raf_sut");
                case QuestType.DriveThruOrders: return UIKit.Icon("drive_thru");
                case QuestType.Harvest: return UIKit.Icon("tarla_misir");
                case QuestType.BuyUpgrades: return UIKit.Icon("makine_yogurt");
                case QuestType.SellProduct: return UIKit.Icon("raf_" + q.item.ToString().ToLowerInvariant());
                case QuestType.CollectProduct: return UIKit.Icon(ProducerIcon(q.item));
                case QuestType.FeedAnimals: return UIKit.Icon("tarla_saman");
                case QuestType.LoadMachines: return UIKit.Icon("makine_peynir");
                case QuestType.HappyCars: return UIKit.Icon("drive_thru");
                case QuestType.CashierSelf: return UIKit.Icon("player");
                case QuestType.RushSales: return UIKit.Icon("calisan_kasiyer");
                case QuestType.UnlockArea: return UIKit.Icon("tabela");
                default: return UIKit.Coin;
            }
        }

        // ---------------- panel ----------------

        public void Open()
        {
            if (IsOpen || DailyReward.IsOpen) return;
            if (date != Today) Generate();
            Refresh();
            panel.SetActive(true);
            IsOpen = true;
            AudioFx.Play(Sfx.Popup, 0.8f);
            var card = panel.transform.Find("Card");
            if (card) card.gameObject.AddComponent<PopAnim>();
        }

        public void Close()
        {
            if (!IsOpen) return;
            panel.SetActive(false);
            IsOpen = false;
        }

        void Refresh()
        {
            int claimed = 0;
            for (int i = 0; i < Count; i++)
            {
                var q = quests[i]; var r = rows[i];
                bool done = q.progress >= q.target;
                if (q.claimed) claimed++;
                r.title.text = Describe(q);
                r.icon.sprite = IconOf(q);
                r.prog.text = q.type == QuestType.EarnMoney ? UIKit.Money(q.progress) + " / " + UIKit.Money(q.target) : q.progress + " / " + q.target;
                float f = q.target > 0 ? Mathf.Clamp01((float)q.progress / q.target) : 0f;
                r.fillRt.anchorMax = new Vector2(Mathf.Max(0.001f, f), 1f);
                r.fill.color = done ? UIKit.Green : UIKit.Hex("FFB020");
                r.check.gameObject.SetActive(q.claimed);
                r.btn.Root.gameObject.SetActive(!q.claimed);
                if (!q.claimed)
                {
                    r.btn.Button.interactable = done;
                    UIKit.SetChunkColor(r.btn, done ? UIKit.Green : UIKit.Gray, done ? UIKit.GreenLip : UIKit.GrayLip);
                    r.btnText.text = UIKit.Money(q.reward);
                }
            }
            bonusFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.001f, claimed / (float)Count), 1f);
            bonusText.text = bonusClaimed ? "Bugünün sandığını aldın!" : "Üç görevi de bitir: <color=#2F9E3A><b>+" + UIKit.Money(bonusReward) + "</b></color>  (" + claimed + "/" + Count + ")";
            bool bonusReady = claimed == Count && !bonusClaimed;
            bonusBtn.Root.gameObject.SetActive(!bonusClaimed);
            bonusBtn.Button.interactable = bonusReady;
            UIKit.SetChunkColor(bonusBtn, bonusReady ? UIKit.Gold : UIKit.Gray, bonusReady ? UIKit.GoldLip : UIKit.GrayLip);
            UpdateTimer();
        }

        void UpdateTimer()
        {
            var left = DateTime.Today.AddDays(1) - DateTime.Now;
            string s = left.Hours > 0 ? left.Hours + " sa " + left.Minutes + " dk" : left.Minutes + " dk " + left.Seconds + " sn";
            if (timerText.text.Length == 0 || Time.frameCount % 30 == 0) timerText.text = "Yeni görevler: " + s;
        }

        void ClaimQuest(int i)
        {
            var q = quests[i];
            if (q.claimed || q.progress < q.target) return;
            q.claimed = true;
            GameManager.I.AddMoney(q.reward);
            GameManager.I.Save();
            AudioFx.Play(Sfx.Coin);
            Haptics.Medium();
            Save();
            Refresh();
            rows[i].punch.Punch(0.12f);
        }

        void ClaimBonus()
        {
            foreach (var q in quests) if (!q.claimed) return;
            if (bonusClaimed) return;
            bonusClaimed = true;
            GameManager.I.AddMoney(bonusReward);
            GameManager.I.Save();
            AudioFx.Play(Sfx.Coin);
            AudioFx.Play(Sfx.Unlock, 0.6f);
            Haptics.Medium();
            Save();
            Refresh();
        }

        void BuildUI()
        {
            var canvas = UIKit.MakeCanvas("QuestCanvas", transform, 45);          // pencere: tabletin ve hoş geldin penceresinin üstünde
            var hudCanvas = UIKit.MakeCanvas("QuestHud", transform, 19);         // HUD butonu: pencerelerin altında

            // HUD butonu: hediye butonunun altında, turuncu, görev panosu
            var hud = UIKit.MakeButton("QuestButton", hudCanvas.transform, UIKit.Hex("FF9F45"), UIKit.Hex("D4701C"), 56f, () => { if (IsOpen) Close(); else Open(); }, 5f, 9f);
            UIKit.Place(hud.Rect, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-82, -452), new Vector2(112, 116));
            var gi = UIKit.Img("Board", hud.Face.transform, UIKit.Icon("hud_gorev"), Color.white);
            gi.preserveAspect = true;
            UIKit.Stretch(gi.rectTransform, 14, 12, 14, 12);
            hudBadge = DailyReward.MakeBadge(hud.Rect);
            hudPunch = hud.Root.gameObject.AddComponent<UIPunch>();
            UIKit.HudBlockers.Add(hud.Rect);
            hudRoot = hud.Root.gameObject;

            var p = UIKit.MakePopup(canvas.transform, "Dim", "GÖREVLER", UIKit.Hex("FF9F45"), UIKit.Hex("D4701C"), new Vector2(940, 1240), 10f);
            panel = p.Dim.gameObject;
            p.Card.name = "Card";
            p.Dim.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            DailyReward.MakeCloseButton(p.Card, Close);

            timerText = UIKit.Label("Timer", p.Body, "", 30, UIKit.TextSoft, TextAnchor.MiddleCenter, false, true);
            UIKit.Place(timerText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -84), new Vector2(800, 50));

            for (int i = 0; i < Count; i++)
            {
                var r = new Row();
                var card = UIKit.MakeChunk("Quest" + i, p.Body, Color.white, UIKit.CreamLip, 46f, 6f, 10f, false);
                UIKit.Place(card.Rect, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150 - i * 236), new Vector2(840, 220));
                var face = card.Face.transform;
                r.punch = card.Root.gameObject.AddComponent<UIPunch>();

                var disc = UIKit.MakeChunk("Disc", face, UIKit.Hex("FF9F45"), UIKit.Hex("D4701C"), 70f, 6f, 8f, false);
                UIKit.Place(disc.Rect, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 2), new Vector2(150, 156));
                var inner = UIKit.Img("Inner", disc.Face.transform, UIKit.Circle, new Color(1, 1, 1, 0.94f));
                UIKit.Stretch(inner.rectTransform, 10, 10, 10, 10);
                r.icon = UIKit.Img("Icon", inner.transform, null, Color.white);
                r.icon.preserveAspect = true;
                UIKit.Stretch(r.icon.rectTransform, 8, 8, 8, 8);

                r.title = UIKit.Label("Title", face, "", 36, UIKit.TextDark, TextAnchor.UpperLeft);
                r.title.resizeTextForBestFit = true; r.title.resizeTextMinSize = 24; r.title.resizeTextMaxSize = 36;
                r.title.verticalOverflow = VerticalWrapMode.Truncate;
                var trt = r.title.rectTransform;
                trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1); trt.pivot = new Vector2(0, 1);
                trt.offsetMin = new Vector2(186, -110); trt.offsetMax = new Vector2(-232, -18);

                // ilerleme çubuğu
                var bar = UIKit.Img("Bar", face, UIKit.Round, UIKit.Ink, 62f / 22f);
                UIKit.Place(bar.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(186, 24), new Vector2(410, 52));
                var barBg = UIKit.Img("Bg", bar.transform, UIKit.Round, UIKit.Hex("E3D9C6"), 62f / 17f);
                UIKit.Stretch(barBg.rectTransform, 5, 5, 5, 5);
                r.fill = UIKit.Img("Fill", barBg.transform, UIKit.Round, UIKit.Green, 62f / 17f);
                r.fillRt = r.fill.rectTransform;
                r.fillRt.anchorMin = Vector2.zero; r.fillRt.anchorMax = new Vector2(0.5f, 1); r.fillRt.offsetMin = r.fillRt.offsetMax = Vector2.zero;
                r.prog = UIKit.Label("Prog", bar.transform, "", 30, Color.white, TextAnchor.MiddleCenter, true);
                UIKit.Stretch(r.prog.rectTransform, 0, -2, 0, 0);

                // ödül butonu
                int idx = i;
                r.btn = UIKit.MakeButton("Claim", face, UIKit.Green, UIKit.GreenLip, 50f, () => ClaimQuest(idx), 6f, 12f);
                UIKit.Place(r.btn.Rect, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-18, 2), new Vector2(200, 108));
                var coin = UIKit.Img("Coin", r.btn.Face.transform, UIKit.Coin, Color.white);
                UIKit.Place(coin.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(54, 54));
                r.btnText = UIKit.Label("T", r.btn.Face.transform, "", 38, Color.white, TextAnchor.MiddleCenter, true);
                UIKit.Stretch(r.btnText.rectTransform, 58, -4, 6, 0);
                r.btnText.resizeTextForBestFit = true; r.btnText.resizeTextMinSize = 24; r.btnText.resizeTextMaxSize = 38;

                // alındı işareti
                var chk = UIKit.Img("Done", face, UIKit.Circle, UIKit.Ink);
                UIKit.Place(chk.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-64, 2), new Vector2(96, 96));
                var cIn = UIKit.Img("Fill", chk.transform, UIKit.Circle, UIKit.Green);
                UIKit.Stretch(cIn.rectTransform, 7, 7, 7, 7);
                var tick = UIKit.Img("T", cIn.transform, UIKit.Check, Color.white);
                UIKit.Stretch(tick.rectTransform, 14, 14, 14, 14);
                r.check = chk;
                rows[i] = r;
            }

            // günün sandığı
            var bonus = UIKit.MakeChunk("Bonus", p.Body, UIKit.Hex("FFF1CF"), UIKit.Hex("E8C987"), 46f, 6f, 10f, false);
            UIKit.Place(bonus.Rect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(840, 230));
            var bface = bonus.Face.transform;
            var chest = UIKit.Img("Chest", bface, UIKit.Icon("hud_hediye"), Color.white);
            chest.preserveAspect = true;
            UIKit.Place(chest.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 4), new Vector2(140, 140));
            bonusText = UIKit.Label("Text", bface, "", 32, UIKit.TextDark, TextAnchor.UpperLeft, false, true);
            bonusText.resizeTextForBestFit = true; bonusText.resizeTextMinSize = 22; bonusText.resizeTextMaxSize = 32;
            var brt = bonusText.rectTransform;
            brt.anchorMin = new Vector2(0, 1); brt.anchorMax = new Vector2(1, 1); brt.pivot = new Vector2(0, 1);
            brt.offsetMin = new Vector2(186, -120); brt.offsetMax = new Vector2(-232, -18);
            var bbar = UIKit.Img("Bar", bface, UIKit.Round, UIKit.Ink, 62f / 22f);
            UIKit.Place(bbar.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(186, 26), new Vector2(410, 44));
            var bbg = UIKit.Img("Bg", bbar.transform, UIKit.Round, UIKit.Hex("E3D9C6"), 62f / 17f);
            UIKit.Stretch(bbg.rectTransform, 5, 5, 5, 5);
            bonusFill = UIKit.Img("Fill", bbg.transform, UIKit.Round, UIKit.Gold, 62f / 17f);
            var bfr = bonusFill.rectTransform; bfr.anchorMin = Vector2.zero; bfr.anchorMax = new Vector2(0, 1); bfr.offsetMin = bfr.offsetMax = Vector2.zero;
            bonusBtn = UIKit.MakeButton("Open", bface, UIKit.Gold, UIKit.GoldLip, 50f, ClaimBonus, 6f, 12f);
            UIKit.Place(bonusBtn.Rect, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-18, 2), new Vector2(200, 108));
            bonusBtnText = UIKit.Label("T", bonusBtn.Face.transform, "AÇ", 44, Color.white, TextAnchor.MiddleCenter, true);
            UIKit.Stretch(bonusBtnText.rectTransform, 0, -4, 0, 0);
        }
    }
}

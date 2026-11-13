using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CiftlikMarket
{
    /// <summary>
    /// Yükseltme tableti. Para göstergesinin altındaki tablet butonuna dokununca açılır.
    /// Ana sayfada bölümler (Karakter, Çalışanlar, Makineler, Market) yuvarlak baloncuklar halinde durur;
    /// birine dokununca o bölümün yükseltme kartları listelenir. Arayüz çalışma anında koddan kurulur.
    /// </summary>
    public class OfficeUI : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        static RectTransform hudButtonRect;

        /// <summary>Joystick bu dokunuşu yok saysın mı? (HUD butonunun üstü)</summary>
        public static bool IsOverButton(Vector2 screenPos)
        {
            if (hudButtonRect && hudButtonRect.gameObject.activeInHierarchy &&
                RectTransformUtility.RectangleContainsScreenPoint(hudButtonRect, screenPos, null)) return true;
            foreach (var r in UIKit.HudBlockers)
                if (r && r.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(r, screenPos, null)) return true;
            return false;
        }

        // ---------------- bölümler ----------------

        enum Cat { Karakter, Calisanlar, Makineler, Market }

        class CatInfo
        {
            public Cat cat; public string name; public Color col, lip; public string icon;
            public Chunk bubble, pill; public Image inner, iconImg, lockImg; public RectTransform badge; public Text pillText;
        }

        readonly CatInfo[] cats =
        {
            new CatInfo { cat = Cat.Karakter,   name = "KARAKTER",   col = UIKit.Hex("4DA6FF"), lip = UIKit.Hex("2A78CF"), icon = "player" },
            new CatInfo { cat = Cat.Calisanlar, name = "ÇALIŞANLAR", col = UIKit.Hex("FF9F45"), lip = UIKit.Hex("D4701C"), icon = "calisan_bakici" },
            new CatInfo { cat = Cat.Makineler,  name = "MAKİNELER",  col = UIKit.Hex("A77BFF"), lip = UIKit.Hex("7A4FD6"), icon = "makine_yogurt" },
            new CatInfo { cat = Cat.Market,     name = "MARKET",     col = UIKit.Hex("4CC97A"), lip = UIKit.Hex("2B9A55"), icon = "tabela" },
        };

        static Cat CatOf(Upgrade u)
        {
            switch (u.kind)
            {
                case UpgradeKind.PlayerCapacity:
                case UpgradeKind.PlayerSpeed:
                case UpgradeKind.PlayerFlow: return Cat.Karakter;
                case UpgradeKind.WorkerCapacity:
                case UpgradeKind.WorkerSpeed:
                case UpgradeKind.WorkerCount:
                case UpgradeKind.CashierSpeed:
                case UpgradeKind.SecondCheckout:
                case UpgradeKind.SecondDriveThru: return Cat.Calisanlar;
                case UpgradeKind.StationSlot:
                case UpgradeKind.MachineSpeed:
                case UpgradeKind.OutputTray:
                case UpgradeKind.FieldRegrow:
                case UpgradeKind.FieldSize: return Cat.Makineler;
                default: return Cat.Market;
            }
        }

        static string IconOf(Upgrade u)
        {
            switch (CatOf(u))
            {
                case Cat.Karakter: return "player";
                case Cat.Calisanlar:
                    if (u.kind == UpgradeKind.SecondCheckout) return "calisan_kasiyer";
                    if (u.kind == UpgradeKind.SecondDriveThru) return "calisan_gorevli";
                    return u.workerTemplate ? WorkerIcon(u.workerTemplate.role) : "calisan_bakici";
                case Cat.Market:
                    switch (u.kind)
                    {
                        case UpgradeKind.ShelfCapacity:
                        case UpgradeKind.ProductValue: return "raf_" + u.shelfType.ToString().ToLowerInvariant();
                        case UpgradeKind.SecondCheckout: return "calisan_kasiyer";
                        case UpgradeKind.SecondDriveThru: return "drive_thru";
                        case UpgradeKind.DriveThruRate: return "drive_thru";
                        default: return "tabela";
                    }
            }
            if (u.kind == UpgradeKind.FieldRegrow || u.kind == UpgradeKind.FieldSize) return u.field ? "tarla_" + u.field.produces.ToString().ToLowerInvariant() : "tabela";
            if (!u.station) return "makine_yogurt";
            string n = u.station.name.ToLowerInvariant();
            if (n.StartsWith("inek")) return "inek_1";
            if (n.StartsWith("kumes")) return "kumes_1";
            return n;
        }

        static string WorkerIcon(Worker.Role r)
        {
            switch (r)
            {
                case Worker.Role.Cashier: return "calisan_kasiyer";
                case Worker.Role.DriveThru: return "calisan_gorevli";
                case Worker.Role.Stocker: return "calisan_depocu";
                default: return "calisan_bakici";
            }
        }

        static Sprite SymbolOf(Upgrade u)
        {
            switch (u.kind)
            {
                case UpgradeKind.PlayerSpeed:
                case UpgradeKind.PlayerFlow:
                case UpgradeKind.WorkerSpeed:
                case UpgradeKind.CashierSpeed:
                case UpgradeKind.MachineSpeed:
                case UpgradeKind.FieldRegrow:
                case UpgradeKind.CustomerAttraction:
                case UpgradeKind.DriveThruRate: return UIKit.Bolt;
                case UpgradeKind.ProductValue: return UIKit.Coin;
                case UpgradeKind.WorkerCount:
                case UpgradeKind.SecondCheckout:
                case UpgradeKind.SecondDriveThru: return UIKit.Plus;
                case UpgradeKind.OfflineCapacity: return UIKit.Clock;
                default: return UIKit.Boxes;
            }
        }

        static Color SymbolColor(Upgrade u)
        {
            switch (u.kind)
            {
                case UpgradeKind.PlayerSpeed:
                case UpgradeKind.PlayerFlow:
                case UpgradeKind.WorkerSpeed:
                case UpgradeKind.CashierSpeed:
                case UpgradeKind.MachineSpeed:
                case UpgradeKind.FieldRegrow:
                case UpgradeKind.CustomerAttraction:
                case UpgradeKind.DriveThruRate: return UIKit.Hex("FFB020");
                case UpgradeKind.ProductValue: return UIKit.Hex("2FB36B");
                case UpgradeKind.WorkerCount:
                case UpgradeKind.SecondCheckout:
                case UpgradeKind.SecondDriveThru: return UIKit.Hex("FF6B8B");
                case UpgradeKind.OfflineCapacity: return UIKit.Hex("5C7CFA");
                default: return UIKit.Hex("C98A4B");
            }
        }

        // ---------------- kart ----------------

        class Card
        {
            public Upgrade up; public UnlockZone zone;
            public Text title, desc, cost;
            public Chunk btn; public Image coin;
            public readonly List<Image> pips = new List<Image>();
        }

        readonly List<Card> cards = new List<Card>();
        readonly List<Object> shownKeys = new List<Object>();

        GameObject panel;
        RectTransform tablet, homeView, listView, listContent, hudBadge, settingsView, mapView, mapBadge;
        Chunk banner, backBtn, hudBtn, gearBtn, mapBtn, wsBtn;
        RectTransform wsBadge;
        Text bannerText, moneyText;
        ScrollRect scroll;
        CatInfo current;
        float refreshTimer, badgeTimer;
        bool anyAffordable;

        // ---------------- rehber (tutorial) için erişim ----------------

        public static OfficeUI I { get; private set; }
        public static RectTransform HudButton => hudButtonRect;
        public bool HomeShown => IsOpen && homeView.gameObject.activeInHierarchy;
        public bool KarakterShown => IsOpen && listView.gameObject.activeInHierarchy && current != null && current.cat == Cat.Karakter;
        public bool OtherListShown => IsOpen && (settingsView.gameObject.activeInHierarchy || mapView.gameObject.activeInHierarchy ||
                                                 (listView.gameObject.activeInHierarchy && current != null && current.cat != Cat.Karakter));
        public RectTransform KarakterBubble => cats[0].bubble.Rect;
        public RectTransform BackButton => backBtn.Rect;
        public RectTransform BuyButtonFor(Upgrade u)
        {
            foreach (var c in cards) if (c.up == u && c.btn != null) return c.btn.Rect;
            return null;
        }
        public void CloseTablet() { if (IsOpen) Close(); }

        void Awake()
        {
            I = this;
            BuildUI();
            panel.SetActive(false);
            EnsureEventSystem();
        }

        public void Open()
        {
            FreezeWorld(true);
            panel.SetActive(true);
            IsOpen = true;
            AudioFx.Play(Sfx.Popup, 0.8f);
            Pop(tablet, 0.35f);
            ShowHome();
        }

        public void Close()
        {
            if (panel) panel.SetActive(false);
            IsOpen = false;
            FreezeWorld(false);
        }

        // ---------------- performans: tablet açıkken dünya çizilmesin ----------------
        // Tablet ekranı neredeyse tamamen kaplıyor; arkadaki 3D dünyayı her karede çizmek telefonda FPS'i yarıya düşürüyordu.
        // Açılırken son görüntü yarım çözünürlükte bir dokuya alınır, kamera kapatılır; kapanınca geri açılır.
        RawImage snapshot;
        RenderTexture snapRt;
        Camera frozenCam;

        void FreezeWorld(bool on)
        {
            if (on)
            {
                var cam = Camera.main;
                if (cam == null || frozenCam != null) return;
                int w = Mathf.Max(64, Screen.width / 2), h = Mathf.Max(64, Screen.height / 2);
                if (snapRt == null || snapRt.width != w || snapRt.height != h)
                {
                    if (snapRt) snapRt.Release();
                    snapRt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);   // 16 bit derinlik telefonda zemin/çim çizgilenmesi (z-fighting) yapıyordu
                }
                var prev = cam.targetTexture;
                cam.targetTexture = snapRt;
                cam.Render();
                cam.targetTexture = prev;
                cam.enabled = false;
                frozenCam = cam;
                snapshot.texture = snapRt;
                snapshot.gameObject.SetActive(true);
            }
            else
            {
                if (frozenCam) frozenCam.enabled = true;
                frozenCam = null;
                if (snapshot) snapshot.gameObject.SetActive(false);
            }
        }

        void OnDestroy() { IsOpen = false; if (frozenCam) frozenCam.enabled = true; if (snapRt) snapRt.Release(); }

        void Update()
        {
            badgeTimer -= Time.unscaledDeltaTime;
            if (badgeTimer <= 0f) { badgeTimer = 0.4f; anyAffordable = AnyAffordable(null); }
            if (hudBadge)
            {
                hudBadge.gameObject.SetActive(anyAffordable);
                hudBadge.localScale = Vector3.one * (1f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 0.14f);
            }
            if (mapBadge && badgeTimer >= 0.39f) mapBadge.gameObject.SetActive(AnyBranchFull());
            if (wsBadge && badgeTimer >= 0.39f) wsBadge.gameObject.SetActive(Wholesale.NeedsRestock);
            if (mapBadge && mapBadge.gameObject.activeSelf) mapBadge.localScale = Vector3.one * (1f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 0.15f);
            if (hudBtn != null) hudBtn.Rect.localEulerAngles = new Vector3(0, 0, anyAffordable && !IsOpen ? Mathf.Sin(Time.unscaledTime * 6f) * 3f : 0f);

            if (!IsOpen) return;
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.2f;
            if (moneyText && GameManager.I) moneyText.text = UIKit.Money(GameManager.I.Money);
            if (settingsView.gameObject.activeSelf) return;
            if (mapView.gameObject.activeSelf) { RefreshMap(); return; }
            if (current == null) RefreshHome();
            else
            {
                if (!SameEntries()) RebuildList();
                RefreshCards();
            }
        }

        // ---------------- veri ----------------

        List<Object> Entries(Cat c)
        {
            var list = new List<Object>();
            if (c == Cat.Market)
                foreach (var z in UnlockZone.OfficeItems) if (!z.isWorker && ZoneVisible(z)) list.Add(z);
            if (c == Cat.Calisanlar)
            {
                // işe alınmamış çalışanlar üstte: şartı sağlananlar önce, sonra kilitliler
                var ws = new List<UnlockZone>();
                foreach (var z in UnlockZone.OfficeItems) if (z.isWorker) ws.Add(z);
                ws.Sort((a, b) => a.RequirementsMet != b.RequirementsMet ? (a.RequirementsMet ? -1 : 1) : a.sortOrder.CompareTo(b.sortOrder));
                list.AddRange(ws);
            }
            var ups = new List<Upgrade>();
            foreach (var u in Upgrade.All) if (u.Available && CatOf(u) == c) ups.Add(u);
            ups.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));
            list.AddRange(ups);
            return list;
        }

        bool AnyAffordable(CatInfo only)
        {
            if (GameManager.I == null) return false;
            int money = GameManager.I.Money;
            foreach (var z in UnlockZone.OfficeItems)
            {
                if (!ZoneVisible(z) || money < z.Remaining) continue;
                if (only == null || only.cat == (z.isWorker ? Cat.Calisanlar : Cat.Market)) return true;
            }
            foreach (var u in Upgrade.All)
                if (u.Available && !u.IsMaxed && money >= u.Cost && (only == null || CatOf(u) == only.cat)) return true;
            return false;
        }

        /// <summary>Yeni şube kartı sadece açılabilecek bir sonraki bölge varsa görünür.</summary>
        static bool ZoneVisible(UnlockZone z) => z.RequirementsMet && (!z.opensNewBranch || BranchManager.CanOpenNext);

        bool SameEntries()
        {
            var now = Entries(current.cat);
            if (now.Count != shownKeys.Count) return false;
            for (int i = 0; i < now.Count; i++) if (now[i] != shownKeys[i]) return false;
            return true;
        }

        // ---------------- gezinme ----------------

        void ShowHome()
        {
            current = null;
            homeView.gameObject.SetActive(true);
            listView.gameObject.SetActive(false);
            settingsView.gameObject.SetActive(false);
            mapView.gameObject.SetActive(false);
            backBtn.Root.gameObject.SetActive(false);
            gearBtn.Root.gameObject.SetActive(true);
            mapBtn.Root.gameObject.SetActive(true);
            wsBtn.Root.gameObject.SetActive(Wholesale.Available);
            SetBanner("YÜKSELTMELER", UIKit.Hex("FF8A3D"), UIKit.Hex("D25E17"));
            refreshTimer = 0f;
            RefreshHome();
            Pop(homeView, 0.25f);
        }

        void ShowCategory(CatInfo c)
        {
            if (Entries(c.cat).Count == 0) { Pop(c.bubble.Rect, 0.2f); return; }
            current = c;
            homeView.gameObject.SetActive(false);
            listView.gameObject.SetActive(true);
            settingsView.gameObject.SetActive(false);
            mapView.gameObject.SetActive(false);
            backBtn.Root.gameObject.SetActive(true);
            gearBtn.Root.gameObject.SetActive(false);
            mapBtn.Root.gameObject.SetActive(false);
            wsBtn.Root.gameObject.SetActive(false);
            SetBanner(c.name, c.col, c.lip);
            RebuildList();
            Pop(listView, 0.25f);
        }

        /// <summary>Görünümü "pop" ile açar; yarıda kalmış eski animasyonu temizler (yoksa küçük kalabilir).</summary>
        static void Pop(RectTransform rt, float duration)
        {
            foreach (var old in rt.GetComponents<PopAnim>()) DestroyImmediate(old);
            rt.localScale = Vector3.one;
            rt.gameObject.AddComponent<PopAnim>().duration = duration;
        }

        void SetBanner(string text, Color col, Color lip)
        {
            bannerText.text = text;
            UIKit.SetChunkColor(banner, col, lip);
        }

        void RefreshHome()
        {
            foreach (var c in cats)
            {
                bool has = Entries(c.cat).Count > 0;
                UIKit.SetChunkColor(c.bubble, has ? c.col : UIKit.Gray, has ? c.lip : UIKit.GrayLip);
                UIKit.SetChunkColor(c.pill, has ? c.col : UIKit.Gray, has ? c.lip : UIKit.GrayLip);
                c.iconImg.color = has ? Color.white : new Color(1, 1, 1, 0.35f);
                c.lockImg.gameObject.SetActive(!has);
                bool afford = has && AnyAffordable(c);
                c.badge.gameObject.SetActive(afford);
                if (afford) c.badge.localScale = Vector3.one * (1f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 0.12f);
            }
        }

        void RebuildList()
        {
            foreach (Transform child in listContent) Destroy(child.gameObject);
            cards.Clear();
            shownKeys.Clear();
            foreach (var e in Entries(current.cat))
            {
                shownKeys.Add(e);
                cards.Add(CreateCard(e as Upgrade, e as UnlockZone));
            }
            RefreshCards();
            Canvas.ForceUpdateCanvases();
            if (scroll) scroll.verticalNormalizedPosition = 1f;
        }

        void RefreshCards()
        {
            int money = GameManager.I ? GameManager.I.Money : 0;
            foreach (var c in cards)
            {
                if (c.up != null)
                {
                    var parts = c.up.title.Split('·');
                    c.title.text = CatOf(c.up) == Cat.Makineler || parts.Length < 2 ? parts[0].Trim()
                                 : parts[0].Trim() + " <color=#B9A98F>•</color> " + parts[1].Trim();
                    c.desc.text = c.up.Describe().Replace("  →  ", "  <color=#2F9E3A>→</color>  ").Replace("  (maks)", "");
                    for (int i = 0; i < c.pips.Count; i++) c.pips[i].color = i < c.up.Level ? UIKit.Green : UIKit.Hex("E3D9C6");
                    if (c.up.IsMaxed) SetBuy(c, "MAKS", UIKit.Gold, UIKit.GoldLip, false, false);
                    else SetBuy(c, UIKit.Money(c.up.Cost), money >= c.up.Cost ? UIKit.Green : UIKit.Gray, money >= c.up.Cost ? UIKit.GreenLip : UIKit.GrayLip, money >= c.up.Cost, true);
                }
                else if (c.zone != null)
                {
                    c.title.text = c.zone.title.Replace('\n', ' ');
                    int cost = c.zone.Remaining;
                    if (!c.zone.RequirementsMet)
                    {
                        // şartı sağlanmamış çalışan: kilitli göster, nedenini yaz
                        c.desc.text = "<color=#D4701C>" + (string.IsNullOrEmpty(c.zone.lockText) ? "Henüz alınamaz" : c.zone.lockText) + "</color>";
                        SetBuy(c, "KİLİTLİ", UIKit.Gray, UIKit.GrayLip, false, false);
                    }
                    else
                    {
                        c.desc.text = c.zone.description;
                        SetBuy(c, UIKit.Money(cost), money >= cost ? UIKit.Green : UIKit.Gray, money >= cost ? UIKit.GreenLip : UIKit.GrayLip, money >= cost, true);
                    }
                }
            }
        }

        static void SetBuy(Card c, string text, Color fill, Color lip, bool interactable, bool showCoin)
        {
            c.cost.text = text;
            UIKit.SetChunkColor(c.btn, fill, lip);
            c.btn.Button.interactable = interactable;
            c.coin.gameObject.SetActive(showCoin);
            c.cost.rectTransform.offsetMin = new Vector2(showCoin ? 70 : 0, c.cost.rectTransform.offsetMin.y);
        }

        void OnBuy(Card c)
        {
            bool ok = c.up != null ? c.up.TryBuy() : (c.zone != null && c.zone.TryPurchase());
            if (ok)
            {
                Pop(c.btn.Rect, 0.25f);
                var punch = c.btn.Rect.parent.GetComponent<UIPunch>();
                if (punch) punch.Punch(0.05f);
            }
            refreshTimer = 0f;
            badgeTimer = 0f;
        }

        // ---------------- arayüz kurulumu ----------------

        void BuildUI()
        {
            var canvasGo = new GameObject("UpgradeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var cs = canvasGo.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            BuildHudButton(canvasGo.transform);
            // donmuş dünya görüntüsü (tabletin arkasında)
            var snapGo = new GameObject("WorldSnapshot", typeof(RectTransform), typeof(RawImage));
            snapGo.transform.SetParent(canvasGo.transform, false);
            snapshot = snapGo.GetComponent<RawImage>();
            snapshot.raycastTarget = false;
            UIKit.Stretch(snapshot.rectTransform);
            snapGo.SetActive(false);
            BuildTablet(canvasGo.transform);
            // her parça kendi tuvalinde: HUD butonunun sallanması koca tableti her karede yeniden dizdirmesin
            Isolate(hudBtn.Root.gameObject);
            snapGo.AddComponent<Canvas>();   // iç içe tuvaller sırayı korusun diye bu da kendi tuvalinde (yoksa karartmanın üstüne çiziliyor)
            Isolate(panel);
        }

        static void Isolate(GameObject go)
        {
            if (go.GetComponent<Canvas>() == null) go.AddComponent<Canvas>();
            if (go.GetComponent<GraphicRaycaster>() == null) go.AddComponent<GraphicRaycaster>();
        }

        /// <summary>Para göstergesinin altında: yuvarlak mavi buton, içinde tablet, köşede yeşil ok rozeti.</summary>
        void BuildHudButton(Transform parent)
        {
            hudBtn = UIKit.MakeButton("UpgradeButton", parent, UIKit.Hex("4DA6FF"), UIKit.Hex("2A78CF"), 56f, () => { if (IsOpen) Close(); else Open(); }, 5f, 9f);
            UIKit.Place(hudBtn.Rect, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-82, -180), new Vector2(112, 116));
            hudButtonRect = hudBtn.Rect;
            var icon = UIKit.Img("Tablet", hudBtn.Face.transform, UIKit.Tablet, Color.white);
            UIKit.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(52, 66));
            icon.rectTransform.localEulerAngles = new Vector3(0, 0, -8);

            var badge = UIKit.Img("Badge", hudBtn.Rect, UIKit.Circle, UIKit.Ink);
            UIKit.Place(badge.rectTransform, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-8, -10), new Vector2(48, 48));
            var bIn = UIKit.Img("Fill", badge.transform, UIKit.Circle, UIKit.Green);
            UIKit.Stretch(bIn.rectTransform, 6, 6, 6, 6);
            var arrow = UIKit.Img("Arrow", bIn.transform, UIKit.ArrowUp, Color.white);
            UIKit.Stretch(arrow.rectTransform, 8, 8, 8, 8);
            hudBadge = badge.rectTransform;
        }

        void BuildTablet(Transform parent)
        {
            // karartma (dokununca kapanır)
            var dim = UIKit.Img("UpgradeScreen", parent, null, new Color(0.08f, 0.06f, 0.12f, 0.6f));
            UIKit.Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            dim.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            panel = dim.gameObject;

            // tablet gövdesi
            tablet = UIKit.Node("Tablet", dim.transform);
            UIKit.Place(tablet, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -36), new Vector2(860, 1470));   // telefonda geniş duruyordu
            var body = UIKit.MakeChunk("Body", tablet, UIKit.Hex("343B4A"), UIKit.Hex("1D2129"), 100f, 8f, 20f, false);
            UIKit.Stretch(body.Rect);
            body.Root.raycastTarget = true;
            body.Root.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;   // tablete dokunmak kapatmasın
            var cam = UIKit.Img("Camera", body.Face.transform, UIKit.Circle, UIKit.Hex("596275"));
            UIKit.Place(cam.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -22), new Vector2(16, 16));
            var side = UIKit.Img("SideBtn", tablet, UIKit.Round, UIKit.Hex("1D2129"), 6f);
            UIKit.Place(side.rectTransform, new Vector2(1, 1), new Vector2(0, 1), new Vector2(-2, -260), new Vector2(14, 120));
            side.transform.SetAsFirstSibling();

            // ekran (yuvarlak maske + yumuşak gökyüzü geçişi)
            var screenMask = UIKit.Img("Screen", body.Face.transform, UIKit.Round, Color.white, UIKit_RB(64f));
            UIKit.Stretch(screenMask.rectTransform, 30, 30, 30, 46);
            screenMask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var grad = UIKit.Img("Sky", screenMask.transform, UIKit.Gradient(UIKit.Hex("BFEAFF"), UIKit.Hex("FFF3D6")), Color.white);
            UIKit.Stretch(grad.rectTransform);
            // hafif desen: yumuşak daireler
            // (dekoratif büyük yarı saydam daireler telefonda ekran kartını yoruyordu; kaldırıldı)
            var screen = screenMask.rectTransform;

            // üst satır: geri butonu + para
            backBtn = UIKit.MakeButton("Back", screen, UIKit.Yellow, UIKit.YellowLip, 56f, ShowHome, 6f, 10f);
            UIKit.Place(backBtn.Rect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(26, -96), new Vector2(112, 116));
            var ba = UIKit.Img("Arrow", backBtn.Face.transform, UIKit.ArrowLeft, UIKit.Ink);
            UIKit.Stretch(ba.rectTransform, 22, 20, 22, 20);

            // ayarlar (dişli) — ana sayfada geri butonunun yerinde
            gearBtn = UIKit.MakeButton("Settings", screen, UIKit.Hex("8C9BB0"), UIKit.Hex("5F6E84"), 56f, ShowSettings, 6f, 10f);
            UIKit.Place(gearBtn.Rect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(26, -96), new Vector2(112, 116));
            var gi = UIKit.Img("Gear", gearBtn.Face.transform, UIKit.Gear, Color.white);
            UIKit.Stretch(gi.rectTransform, 18, 16, 18, 16);

            // şubeler haritası
            mapBtn = UIKit.MakeButton("Map", screen, UIKit.Hex("2EC4B6"), UIKit.Hex("1B9488"), 56f, ShowMap, 6f, 10f);
            UIKit.Place(mapBtn.Rect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(152, -96), new Vector2(112, 116));
            var mi = UIKit.Img("Pin", mapBtn.Face.transform, UIKit.Pin, Color.white);
            UIKit.Stretch(mi.rectTransform, 20, 16, 20, 18);
            var mb = UIKit.Img("Alert", mapBtn.Rect, UIKit.Circle, UIKit.Ink);
            UIKit.Place(mb.rectTransform, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-10, -10), new Vector2(50, 50));
            var mbf = UIKit.Img("Fill", mb.transform, UIKit.Circle, UIKit.Red);
            UIKit.Stretch(mbf.rectTransform, 5, 5, 5, 5);
            UIKit.Stretch(UIKit.Label("T", mbf.transform, "!", 34, Color.white).rectTransform, 0, 2, 0, 0);
            mapBadge = mb.rectTransform;

            // toptancı (kamyonet) — bakkal rafı açılınca görünür
            wsBtn = UIKit.MakeButton("Toptanci", screen, UIKit.Hex("FF9F45"), UIKit.Hex("D4701C"), 56f, () => { Close(); if (Wholesale.I != null) Wholesale.I.Open(); }, 6f, 10f);
            UIKit.Place(wsBtn.Rect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(278, -96), new Vector2(112, 116));
            var wi = UIKit.Img("Van", wsBtn.Face.transform, UIKit.Icon("drive_thru"), Color.white);
            wi.preserveAspect = true;
            UIKit.Stretch(wi.rectTransform, 12, 14, 12, 14);
            var wb = UIKit.Img("Alert", wsBtn.Rect, UIKit.Circle, UIKit.Ink);
            UIKit.Place(wb.rectTransform, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-8, -10), new Vector2(44, 44));
            var wbIn = UIKit.Img("Fill", wb.transform, UIKit.Circle, UIKit.Red);
            UIKit.Stretch(wbIn.rectTransform, 5, 5, 5, 5);
            UIKit.Stretch(UIKit.Label("T", wbIn.transform, "!", 28, Color.white).rectTransform, 0, 2, 0, 0);
            wsBadge = wb.rectTransform;
            wsBtn.Root.gameObject.SetActive(false);

            var money = UIKit.Node("Money", screen);
            UIKit.Place(money, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-26, -104), new Vector2(290, 96));
            var mp = UIKit.MakeChunk("Pill", money, UIKit.Cream, UIKit.CreamLip, 48f, 5f, 9f);
            UIKit.Stretch(mp.Rect, 40, 0, 0, 0);
            moneyText = UIKit.Label("Amount", mp.Face.transform, "0", 48, UIKit.TextDark, TextAnchor.MiddleRight);
            UIKit.Stretch(moneyText.rectTransform, 64, -4, 22, 0);
            var coin = UIKit.Img("Coin", money, UIKit.Coin, Color.white);
            UIKit.Place(coin.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(50, 0), new Vector2(108, 108));
            var lira = UIKit.Label("Lira", coin.transform, "₺", 52, UIKit.Hex("B5700E"));
            UIKit.Stretch(lira.rectTransform, 0, 4, 0, 0);

            // görünümler
            homeView = UIKit.Node("Home", screen);
            UIKit.Stretch(homeView, 0, 0, 0, 230);
            BuildHome();

            listView = UIKit.Node("List", screen);
            UIKit.Stretch(listView, 0, 0, 0, 230);
            BuildList();

            settingsView = UIKit.Node("Settings", screen);
            UIKit.Stretch(settingsView, 0, 0, 0, 230);
            BuildSettings();

            mapView = UIKit.Node("Map", screen);
            UIKit.Stretch(mapView, 0, 0, 0, 230);

            // başlık şeridi (tabletin üst kenarına oturur)
            banner = UIKit.MakeChunk("Banner", tablet, UIKit.Hex("FF8A3D"), UIKit.Hex("D25E17"), 62f, 8f, 14f);
            UIKit.Place(banner.Rect, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(620, 132));
            bannerText = UIKit.Label("Title", banner.Face.transform, "YÜKSELTMELER", 62, Color.white, TextAnchor.MiddleCenter, true);
            UIKit.Stretch(bannerText.rectTransform, 10, -4, 10, 0);

            // kapat (sağ üst köşede taşan kırmızı yuvarlak)
            var close = UIKit.MakeButton("Close", tablet, UIKit.Red, UIKit.RedLip, 56f, Close, 7f, 11f);
            UIKit.Place(close.Rect, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-30, -10), new Vector2(116, 120));
            var x = UIKit.Img("X", close.Face.transform, UIKit.Cross, Color.white);
            UIKit.Stretch(x.rectTransform, 20, 18, 20, 18);
        }

        static float UIKit_RB(float radius) => 62f / radius;

        static void AddBlob(Transform parent, Vector2 anchor, float size, float alpha)
        {
            var b = UIKit.Img("Blob", parent, UIKit.Circle, new Color(1, 1, 1, alpha));
            UIKit.Place(b.rectTransform, anchor, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
        }

        void BuildHome()
        {
            var hint = UIKit.Label("Hint", homeView, "Neyi geliştirmek istersin?", 40, UIKit.TextSoft, TextAnchor.MiddleCenter, false, true);
            UIKit.Place(hint.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(740, 60));

            for (int i = 0; i < cats.Length; i++)
            {
                var c = cats[i];
                int col = i % 2, row = i / 2;
                var cell = UIKit.Node(c.name, homeView);
                UIKit.Place(cell, new Vector2(col == 0 ? 0.27f : 0.73f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -90 - row * 500), new Vector2(350, 480));

                var cc = c;
                c.bubble = UIKit.MakeButton("Bubble", cell, c.col, c.lip, 160f, () => ShowCategory(cc), 8f, 16f);
                UIKit.Place(c.bubble.Rect, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(296, 312));
                c.inner = UIKit.Img("Inner", c.bubble.Face.transform, UIKit.Circle, new Color(1, 1, 1, 0.92f));
                UIKit.Stretch(c.inner.rectTransform, 26, 26, 26, 26);
                c.iconImg = UIKit.Img("Icon", c.inner.transform, UIKit.Icon(c.icon), Color.white);
                c.iconImg.preserveAspect = true;
                UIKit.Stretch(c.iconImg.rectTransform, 8, 8, 8, 8);
                c.lockImg = UIKit.Img("Lock", c.bubble.Face.transform, UIKit.Lock, UIKit.Hex("6B7280"));
                UIKit.Place(c.lockImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96, 96));

                c.pill = UIKit.MakeChunk("Name", cell, c.col, c.lip, 46f, 6f, 10f);
                UIKit.Place(c.pill.Rect, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -296), new Vector2(316, 96));
                c.pillText = UIKit.Label("Text", c.pill.Face.transform, c.name, 42, Color.white, TextAnchor.MiddleCenter, true);
                UIKit.Stretch(c.pillText.rectTransform, 4, -4, 4, 0);

                var badge = UIKit.Img("Badge", cell, UIKit.Circle, UIKit.Ink);
                UIKit.Place(badge.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(112, -40), new Vector2(80, 80));
                var bIn = UIKit.Img("Fill", badge.transform, UIKit.Circle, UIKit.Green);
                UIKit.Stretch(bIn.rectTransform, 7, 7, 7, 7);
                var arrow = UIKit.Img("Arrow", bIn.transform, UIKit.ArrowUp, Color.white);
                UIKit.Stretch(arrow.rectTransform, 10, 10, 10, 10);
                c.badge = badge.rectTransform;
            }
        }

        void BuildList()
        {
            var viewport = UIKit.Img("Viewport", listView, null, new Color(0, 0, 0, 0));
            viewport.raycastTarget = true;
            UIKit.Stretch(viewport.rectTransform, 0, 10, 0, 0);
            viewport.gameObject.AddComponent<RectMask2D>();

            listContent = UIKit.Node("Content", viewport.transform);
            listContent.anchorMin = new Vector2(0, 1); listContent.anchorMax = new Vector2(1, 1);
            listContent.pivot = new Vector2(0.5f, 1);
            listContent.sizeDelta = Vector2.zero;
            var vlg = listContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 22;
            vlg.padding = new RectOffset(26, 26, 8, 30);
            vlg.childControlHeight = true; vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;
            listContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = listContent;
            scroll.viewport = viewport.rectTransform;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;
        }

        Card CreateCard(Upgrade up, UnlockZone zone)
        {
            var c = new Card { up = up, zone = zone };
            var holder = UIKit.Node("Card", listContent);
            holder.gameObject.AddComponent<LayoutElement>().preferredHeight = zone != null ? 270 : 236;
            holder.gameObject.AddComponent<UIPunch>();

            var card = UIKit.MakeChunk("Bg", holder, Color.white, UIKit.CreamLip, 50f, 6f, 12f, false);
            UIKit.Stretch(card.Rect);
            var face = card.Face.transform;
            var catCol = current.col; var catLip = current.lip;

            // ikon diski
            var disc = UIKit.MakeChunk("Disc", face, catCol, catLip, 86f, 6f, 8f, false);
            UIKit.Place(disc.Rect, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(172, 180));
            var inner = UIKit.Img("Inner", disc.Face.transform, UIKit.Circle, new Color(1, 1, 1, 0.94f));
            UIKit.Stretch(inner.rectTransform, 12, 12, 12, 12);
            var icon = UIKit.Img("Icon", inner.transform, UIKit.Icon(up != null ? IconOf(up) : (zone != null && !string.IsNullOrEmpty(zone.icon) ? zone.icon : "tabela")), Color.white);
            icon.preserveAspect = true;
            UIKit.Stretch(icon.rectTransform, 4, 4, 4, 4);

            // özellik rozeti
            var sb = UIKit.Img("Stat", disc.Rect, UIKit.Circle, UIKit.Ink);
            UIKit.Place(sb.rectTransform, new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-16, 22), new Vector2(70, 70));
            var sbIn = UIKit.Img("Fill", sb.transform, UIKit.Circle, up != null ? SymbolColor(up) : UIKit.Hex("4CC97A"));
            UIKit.Stretch(sbIn.rectTransform, 6, 6, 6, 6);
            var sym = UIKit.Img("Sym", sbIn.transform, up != null ? SymbolOf(up) : UIKit.Plus, Color.white);
            UIKit.Stretch(sym.rectTransform, 7, 7, 7, 7);

            // yazılar
            c.title = UIKit.Label("Title", face, "", 44, UIKit.TextDark, TextAnchor.UpperLeft);
            FitText(c.title, 24, 44);
            var trt = c.title.rectTransform;
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1); trt.pivot = new Vector2(0, 1);
            trt.offsetMin = new Vector2(214, -70); trt.offsetMax = new Vector2(-262, -14);

            c.desc = UIKit.Label("Desc", face, "", 32, UIKit.TextSoft, TextAnchor.UpperLeft, false, true);
            var drt = c.desc.rectTransform;
            drt.anchorMin = new Vector2(0, 0); drt.anchorMax = new Vector2(1, 1);
            if (zone != null) { drt.offsetMin = new Vector2(214, 16); drt.offsetMax = new Vector2(-262, -74); FitText(c.desc, 22, 32); }
            else
            {
                drt.anchorMin = new Vector2(0, 1); drt.anchorMax = new Vector2(1, 1); drt.pivot = new Vector2(0, 1);
                drt.offsetMin = new Vector2(214, -170); drt.offsetMax = new Vector2(-262, -74);   // iki satıra kadar
                FitText(c.desc, 22, 32);
            }

            // seviye noktaları
            if (up != null)
            {
                var pips = UIKit.Node("Pips", face);
                pips.anchorMin = pips.anchorMax = new Vector2(0, 0); pips.pivot = new Vector2(0, 0);
                pips.anchoredPosition = new Vector2(214, 20); pips.sizeDelta = new Vector2(400, 30);
                for (int i = 0; i < up.maxLevel; i++)
                {
                    var o = UIKit.Img("Pip", pips, UIKit.Round, UIKit.Ink, 62f / 15f);
                    UIKit.Place(o.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(i * 44, 0), new Vector2(38, 26));
                    var f = UIKit.Img("Fill", o.transform, UIKit.Round, UIKit.Green, 62f / 11f);
                    UIKit.Stretch(f.rectTransform, 4, 4, 4, 4);
                    c.pips.Add(f);
                }
            }

            // satın al butonu
            var cc = c;
            c.btn = UIKit.MakeButton("Buy", face, UIKit.Green, UIKit.GreenLip, 50f, () => OnBuy(cc), 6f, 12f);
            UIKit.Place(c.btn.Rect, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 4), new Vector2(206, 104));   // kart kenarlarından pay kalsın
            c.coin = UIKit.Img("Coin", c.btn.Face.transform, UIKit.Coin, Color.white);
            UIKit.Place(c.coin.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(54, 54));
            c.cost = UIKit.Label("Cost", c.btn.Face.transform, "", 40, Color.white, TextAnchor.MiddleCenter, true);
            UIKit.Stretch(c.cost.rectTransform, 60, -4, 8, 0);
            return c;
        }

        static void FitText(Text t, int min, int max)
        {
            t.resizeTextForBestFit = true; t.resizeTextMinSize = min; t.resizeTextMaxSize = max;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
        }

        // ---------------- şubeler (harita) ----------------

        class BranchCard { public int index; public Text status; public Image fill; public RectTransform fillRt; }
        readonly List<BranchCard> branchCards = new List<BranchCard>();

        static bool AnyBranchFull()
        {
            for (int b = 1; b <= BranchManager.Owned; b++)
            {
                if (b == BranchManager.Branch) continue;
                OfflineEarnings.Estimate(b, out float fill);
                if (fill >= 0.9f) return true;
            }
            return false;
        }

        void ShowMap()
        {
            current = null;
            homeView.gameObject.SetActive(false);
            listView.gameObject.SetActive(false);
            settingsView.gameObject.SetActive(false);
            mapView.gameObject.SetActive(true);
            backBtn.Root.gameObject.SetActive(true);
            gearBtn.Root.gameObject.SetActive(false);
            mapBtn.Root.gameObject.SetActive(false);
            wsBtn.Root.gameObject.SetActive(false);
            SetBanner("ŞUBELER", UIKit.Hex("2EC4B6"), UIKit.Hex("1B9488"));
            BuildMap();
            Pop(mapView, 0.25f);
        }

        void BuildMap()
        {
            foreach (Transform c in mapView) Destroy(c.gameObject);
            branchCards.Clear();
            var themes = BranchManager.Themes;
            float y = -8f;
            for (int i = 0; i < themes.Length; i++)
            {
                var th = themes[i];
                int b = i + 1;
                bool owned = b <= BranchManager.Owned, here = b == BranchManager.Branch;
                bool nextOpen = b == BranchManager.Owned + 1 && th.ready;

                var holder = UIKit.Node(th.shortName, mapView);
                holder.anchorMin = new Vector2(0, 1); holder.anchorMax = new Vector2(1, 1); holder.pivot = new Vector2(0.5f, 1);
                holder.offsetMin = new Vector2(26, y - 214); holder.offsetMax = new Vector2(-26, y);
                y -= 236f;

                var card = UIKit.MakeChunk("Bg", holder, owned ? Color.white : UIKit.Hex("F1ECE3"), UIKit.CreamLip, 50f, 6f, 12f, false);
                UIKit.Stretch(card.Rect);
                var face = card.Face.transform;

                Color col = owned || th.ready ? th.grass : UIKit.Gray;
                var disc = UIKit.MakeChunk("Disc", face, col, UIKit.Darker(col), 72f, 6f, 8f, false);
                UIKit.Place(disc.Rect, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(144, 150));
                var inner = UIKit.Img("Inner", disc.Face.transform, UIKit.Circle, new Color(1, 1, 1, owned ? 0.94f : 0.6f));
                UIKit.Stretch(inner.rectTransform, 10, 10, 10, 10);
                var pic = UIKit.Img("Pic", inner.transform, owned || nextOpen ? UIKit.Icon("tabela") : UIKit.Lock, owned || nextOpen ? Color.white : UIKit.Hex("8A92A0"));
                pic.preserveAspect = true;
                UIKit.Stretch(pic.rectTransform, owned || nextOpen ? 4 : 26, owned || nextOpen ? 4 : 26, owned || nextOpen ? 4 : 26, owned || nextOpen ? 4 : 26);

                var t = UIKit.Label("Title", face, th.name, 40, owned ? UIKit.TextDark : UIKit.TextSoft, TextAnchor.UpperLeft);
                t.resizeTextForBestFit = true; t.resizeTextMinSize = 26; t.resizeTextMaxSize = 40;
                t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
                var trt = t.rectTransform; trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1); trt.pivot = new Vector2(0, 1);
                trt.offsetMin = new Vector2(180, -66); trt.offsetMax = new Vector2(-236, -16);

                var mult = UIKit.MakeChunk("Mult", face, UIKit.Hex("E6F7E1"), UIKit.Hex("BFE3B5"), 24f, 4f, 5f, false);
                UIKit.Place(mult.Rect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(180, -64), new Vector2(156, 40));
                UIKit.Stretch(UIKit.Label("T", mult.Face.transform, "Gelir " + BranchManager.MultText(th.mult), 24, UIKit.Hex("2F7D32"), TextAnchor.MiddleCenter, false, true).rectTransform, 0, -2, 0, 0);

                var bc = new BranchCard { index = b };
                bc.status = UIKit.Label("Status", face, "", 28, UIKit.TextSoft, TextAnchor.UpperLeft, false, true);
                bc.status.resizeTextForBestFit = true; bc.status.resizeTextMinSize = 20; bc.status.resizeTextMaxSize = 28;
                bc.status.horizontalOverflow = HorizontalWrapMode.Wrap; bc.status.verticalOverflow = VerticalWrapMode.Truncate;
                var srt = bc.status.rectTransform; srt.anchorMin = new Vector2(0, 0); srt.anchorMax = new Vector2(1, 0); srt.pivot = new Vector2(0, 0);
                srt.offsetMin = new Vector2(180, 40); srt.offsetMax = new Vector2(-236, 76);

                if (owned && !here)
                {
                    var bar = UIKit.Img("Bar", face, UIKit.Round, UIKit.Ink, 62f / 11f);
                    var brt = bar.rectTransform; brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0); brt.pivot = new Vector2(0, 0);
                    brt.offsetMin = new Vector2(180, 12); brt.offsetMax = new Vector2(-250, 32);
                    var bg = UIKit.Img("Bg", bar.transform, UIKit.Round, UIKit.Hex("E9DFCB"), 62f / 7f);
                    UIKit.Stretch(bg.rectTransform, 4, 4, 4, 4);
                    bc.fill = UIKit.Img("Fill", bar.transform, UIKit.Round, UIKit.Green, 62f / 7f);
                    bc.fillRt = bc.fill.rectTransform;
                    bc.fillRt.anchorMin = Vector2.zero; bc.fillRt.anchorMax = new Vector2(0, 1);
                    bc.fillRt.offsetMin = new Vector2(4, 4); bc.fillRt.offsetMax = new Vector2(-4, -4);
                }

                // sağ taraf
                if (here)
                {
                    var tag = UIKit.MakeChunk("Here", face, UIKit.Gold, UIKit.GoldLip, 46f, 6f, 10f);
                    UIKit.Place(tag.Rect, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-18, 0), new Vector2(210, 104));
                    UIKit.Stretch(UIKit.Label("T", tag.Face.transform, "BURADASIN", 30, Color.white, TextAnchor.MiddleCenter, true).rectTransform, 0, -3, 0, 0);
                }
                else if (owned)
                {
                    int target = b;
                    var go = UIKit.MakeButton("Go", face, UIKit.Green, UIKit.GreenLip, 50f, () => { Close(); BranchManager.TravelTo(target); }, 6f, 12f);
                    UIKit.Place(go.Rect, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-18, 0), new Vector2(210, 116));
                    UIKit.Stretch(UIKit.Label("T", go.Face.transform, "GİT", 48, Color.white, TextAnchor.MiddleCenter, true).rectTransform, 0, -4, 0, 0);
                }
                else
                {
                    var tag = UIKit.MakeChunk("Locked", face, UIKit.Gray, UIKit.GrayLip, 46f, 6f, 10f);
                    UIKit.Place(tag.Rect, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-18, 0), new Vector2(210, 104));
                    UIKit.Stretch(UIKit.Label("T", tag.Face.transform, th.ready ? "KİLİTLİ" : "YAKINDA", 32, Color.white, TextAnchor.MiddleCenter, true).rectTransform, 0, -3, 0, 0);
                }
                branchCards.Add(bc);
            }
            RefreshMap();
        }

        void RefreshMap()
        {
            foreach (var bc in branchCards)
            {
                int b = bc.index;
                var th = BranchManager.Themes[b - 1];
                if (b == BranchManager.Branch) bc.status.text = "Şu an bu marketin içindesin";
                else if (b <= BranchManager.Owned)
                {
                    int amount = OfflineEarnings.Estimate(b, out float fill);
                    bc.status.text = amount > 0
                        ? "Kasada birikti: <color=#2F9E3A><b>+" + UIKit.Money(amount) + "</b></color>" + (fill >= 0.999f ? "  <color=#C9343B><b>DOLU!</b></color>" : "")
                        : "Kasiyer işe alınınca sen yokken kazanır";
                    if (bc.fillRt)
                    {
                        bc.fillRt.anchorMax = new Vector2(Mathf.Clamp01(fill), 1);
                        bc.fill.color = fill >= 0.9f ? UIKit.Red : UIKit.Green;
                    }
                }
                else if (b == BranchManager.Owned + 1 && th.ready) bc.status.text = "Market bölümündeki <b>YENİ ŞUBE AÇ</b> ile";
                else if (th.ready) bc.status.text = "Önce önceki şubeyi aç";
                else bc.status.text = "Bu bölge yakında geliyor";
            }
        }

        // ---------------- ayarlar ----------------

        class Switch
        {
            public Chunk pill; public RectTransform knob; public Text onText, offText;
            public System.Func<bool> get; public System.Action<bool> set; public bool shown;
        }

        readonly List<Switch> switches = new List<Switch>();
        Slider musicSlider;

        void ShowSettings()
        {
            current = null;
            homeView.gameObject.SetActive(false);
            listView.gameObject.SetActive(false);
            settingsView.gameObject.SetActive(true);
            mapView.gameObject.SetActive(false);
            backBtn.Root.gameObject.SetActive(true);
            gearBtn.Root.gameObject.SetActive(false);
            mapBtn.Root.gameObject.SetActive(false);
            wsBtn.Root.gameObject.SetActive(false);
            SetBanner("AYARLAR", UIKit.Hex("8C9BB0"), UIKit.Hex("5F6E84"));
            foreach (var s in switches) ApplySwitch(s, true);
            if (musicSlider) musicSlider.SetValueWithoutNotify(AudioFx.MusicLevel);
            Pop(settingsView, 0.25f);
        }

        void LateUpdate()
        {
            // anahtar topuzlarını yumuşakça kaydır
            if (!IsOpen || settingsView == null || !settingsView.gameObject.activeInHierarchy) return;
            foreach (var s in switches)
            {
                float target = s.get() ? 140f : 46f;
                var p = s.knob.anchoredPosition;
                p.x = Mathf.Lerp(p.x, target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
                s.knob.anchoredPosition = p;
            }
            if (musicSlider) musicSlider.interactable = AudioFx.MusicOn;
        }

        void BuildSettings()
        {
            float y = -8f;
            var music = SettingRow("Müzik", "Arka plandaki çiftlik müziği", UIKit.MusicNote, null, UIKit.Hex("A77BFF"), UIKit.Hex("7A4FD6"), ref y, 270f,
                () => AudioFx.MusicOn, v => AudioFx.MusicOn = v);
            BuildVolumeSlider(music);
            SettingRow("Ortam sesleri", "İnek ve tavuk sesleri", null, UIKit.Icon("inek_1"), UIKit.Hex("4CC97A"), UIKit.Hex("2B9A55"), ref y, 170f,
                () => AudioFx.AmbientOn, v => AudioFx.AmbientOn = v);
            SettingRow("Ses efektleri", "Para, buton, satış sesleri", UIKit.Speaker, null, UIKit.Hex("4DA6FF"), UIKit.Hex("2A78CF"), ref y, 170f,
                () => AudioFx.SfxOn, v => AudioFx.SfxOn = v);
            SettingRow("Titreşim", "Para, kilit ve araba uyarısı", VibrateIcon(), null, UIKit.Hex("FF5A8A"), UIKit.Hex("C93465"), ref y, 170f,
                () => Haptics.Enabled, v => { Haptics.Enabled = v; if (v) Haptics.Medium(); });
            SettingRow("FPS göstergesi", "Sol altta kare hızı (test)", UIKit.Gear, null, UIKit.Hex("FF8A3D"), UIKit.Hex("D25E17"), ref y, 170f,
                () => FpsCounter.Visible, v => FpsCounter.Visible = v);

            var ver = UIKit.Label("Version", settingsView, "Çiftlik Market", 30, UIKit.TextSoft, TextAnchor.MiddleCenter, false, true);
            UIKit.Place(ver.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, y - 20f), new Vector2(600, 50));
        }

        static Sprite vibrateIcon;

        /// <summary>Titreşen telefon simgesi (koddan çizilir): yuvarlak köşeli telefon + iki yanda dalga çizgileri.</summary>
        static Sprite VibrateIcon()
        {
            if (vibrateIcon != null) return vibrateIcon;
            const int N = 128;
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N - 0.5f, v = (y + 0.5f) / N - 0.5f;
                    float a = 0f;
                    // telefon gövdesi (içi boş çerçeve)
                    float bx = Mathf.Abs(u) - 0.17f, by = Mathf.Abs(v) - 0.33f;
                    float r = 0.07f;
                    float outer = Mathf.Sqrt(Mathf.Max(bx + r, 0f) * Mathf.Max(bx + r, 0f) + Mathf.Max(by + r, 0f) * Mathf.Max(by + r, 0f)) + Mathf.Min(Mathf.Max(bx + r, by + r), 0f) - r;
                    float frame = Mathf.Clamp01((-outer) * N) * Mathf.Clamp01((outer + 0.07f) * N);
                    a = Mathf.Max(a, frame);
                    // ekran altındaki düğme
                    float bd = Mathf.Sqrt(u * u + (v + 0.24f) * (v + 0.24f));
                    a = Mathf.Max(a, Mathf.Clamp01((0.035f - bd) * N));
                    // titreşim dalgaları (iki yanda iki kısa yay)
                    for (int side = -1; side <= 1; side += 2)
                        for (int k = 0; k < 2; k++)
                        {
                            float cx = side * 0.06f;
                            float rad = 0.30f + k * 0.10f;
                            float d = Mathf.Sqrt((u - cx) * (u - cx) + v * v);
                            bool inArc = side * u > 0.22f && Mathf.Abs(v) < 0.16f + k * 0.04f;
                            if (inArc) a = Mathf.Max(a, Mathf.Clamp01((0.028f - Mathf.Abs(d - rad)) * N));
                        }
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            t.SetPixels32(px);
            t.Apply(false, true);
            vibrateIcon = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
            return vibrateIcon;
        }

        RectTransform SettingRow(string title, string sub, Sprite symbol, Sprite picture, Color col, Color lip, ref float y, float height,
                                 System.Func<bool> get, System.Action<bool> set)
        {
            var holder = UIKit.Node(title, settingsView);
            holder.anchorMin = new Vector2(0, 1); holder.anchorMax = new Vector2(1, 1); holder.pivot = new Vector2(0.5f, 1);
            holder.offsetMin = new Vector2(26, y - height); holder.offsetMax = new Vector2(-26, y);
            y -= height + 22f;

            var card = UIKit.MakeChunk("Bg", holder, Color.white, UIKit.CreamLip, 50f, 6f, 12f, false);
            UIKit.Stretch(card.Rect);
            var face = card.Face.transform;

            var disc = UIKit.MakeChunk("Disc", face, col, lip, 62f, 6f, 8f, false);
            bool compact = height < 200f;   // 6 satır sayfaya sığsın diye alçak satırlar
            float midY = compact ? -height * 0.5f - 3f : -86f;
            UIKit.Place(disc.Rect, new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(20, midY), compact ? new Vector2(106, 112) : new Vector2(124, 130));
            if (picture != null)
            {
                var inner = UIKit.Img("Inner", disc.Face.transform, UIKit.Circle, new Color(1, 1, 1, 0.94f));
                UIKit.Stretch(inner.rectTransform, 8, 8, 8, 8);
                var pic = UIKit.Img("Pic", inner.transform, picture, Color.white);
                pic.preserveAspect = true;
                UIKit.Stretch(pic.rectTransform, 4, 4, 4, 4);
            }
            else
            {
                var ic = UIKit.Img("Icon", disc.Face.transform, symbol, Color.white);
                UIKit.Stretch(ic.rectTransform, 22, 20, 22, 22);
            }

            var t = UIKit.Label("Title", face, title, 44, UIKit.TextDark, TextAnchor.LowerLeft);
            var trt = t.rectTransform; trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1); trt.pivot = new Vector2(0, 1);
            trt.offsetMin = new Vector2(compact ? 148 : 166, compact ? -76 : -86); trt.offsetMax = new Vector2(-230, compact ? -14 : -22);
            var s = UIKit.Label("Sub", face, sub, 28, UIKit.TextSoft, TextAnchor.UpperLeft, false, true);
            var srt = s.rectTransform; srt.anchorMin = new Vector2(0, 1); srt.anchorMax = new Vector2(1, 1); srt.pivot = new Vector2(0, 1);
            srt.offsetMin = new Vector2(compact ? 148 : 166, compact ? -128 : -136); srt.offsetMax = new Vector2(-230, compact ? -78 : -88);

            // anahtar
            var sw = new Switch { get = get, set = set };
            sw.pill = UIKit.MakeButton("Switch", face, UIKit.Green, UIKit.GreenLip, 44f, () =>
            {
                sw.set(!sw.get());
                ApplySwitch(sw, false);
            }, 6f, 10f);
            UIKit.Place(sw.pill.Rect, new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-22, midY), new Vector2(190, 92));
            sw.onText = UIKit.Label("On", sw.pill.Face.transform, "AÇIK", 32, Color.white, TextAnchor.MiddleLeft, true);
            UIKit.Stretch(sw.onText.rectTransform, 22, -3, 0, 0);
            sw.offText = UIKit.Label("Off", sw.pill.Face.transform, "KAPALI", 28, Color.white, TextAnchor.MiddleRight, true);
            UIKit.Stretch(sw.offText.rectTransform, 0, -3, 14, 0);
            var knobO = UIKit.Img("Knob", sw.pill.Face.transform, UIKit.Circle, UIKit.Ink);
            UIKit.Place(knobO.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(get() ? 140 : 46, 0), new Vector2(66, 66));
            var knobI = UIKit.Img("Fill", knobO.transform, UIKit.Circle, Color.white);
            UIKit.Stretch(knobI.rectTransform, 5, 5, 5, 5);
            sw.knob = knobO.rectTransform;
            switches.Add(sw);
            ApplySwitch(sw, true);
            return (RectTransform)face;
        }

        void ApplySwitch(Switch s, bool instant)
        {
            bool on = s.get();
            UIKit.SetChunkColor(s.pill, on ? UIKit.Green : UIKit.Gray, on ? UIKit.GreenLip : UIKit.GrayLip);
            s.onText.gameObject.SetActive(on);
            s.offText.gameObject.SetActive(!on);
            if (instant) s.knob.anchoredPosition = new Vector2(on ? 140 : 46, 0);
        }

        void BuildVolumeSlider(RectTransform face)
        {
            var root = UIKit.Node("Volume", face);
            root.anchorMin = new Vector2(0, 0); root.anchorMax = new Vector2(1, 0); root.pivot = new Vector2(0.5f, 0);
            root.offsetMin = new Vector2(40, 30); root.offsetMax = new Vector2(-40, 100);

            var lo = UIKit.Img("Lo", root, UIKit.Speaker, UIKit.TextSoft);
            UIKit.Place(lo.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(50, 50));

            var track = UIKit.Img("Track", root, UIKit.Round, UIKit.Ink, 62f / 17f);
            track.raycastTarget = true;
            var trt = track.rectTransform; trt.anchorMin = new Vector2(0, 0.5f); trt.anchorMax = new Vector2(1, 0.5f);
            trt.offsetMin = new Vector2(76, -17); trt.offsetMax = new Vector2(-10, 17);
            var bg = UIKit.Img("Bg", track.transform, UIKit.Round, UIKit.Hex("E9DFCB"), 62f / 12f);
            UIKit.Stretch(bg.rectTransform, 5, 5, 5, 5);

            var fillArea = UIKit.Node("FillArea", track.transform);
            UIKit.Stretch(fillArea, 5, 5, 5, 5);
            var fill = UIKit.Img("Fill", fillArea, UIKit.Round, UIKit.Hex("A77BFF"), 62f / 12f);
            UIKit.Stretch(fill.rectTransform);

            var handleArea = UIKit.Node("HandleArea", track.transform);
            UIKit.Stretch(handleArea, 24, 0, 24, 0);
            var handle = UIKit.Img("Handle", handleArea, UIKit.Circle, UIKit.Ink);
            handle.raycastTarget = true;
            handle.rectTransform.sizeDelta = new Vector2(62, 0);
            var hIn = UIKit.Img("Fill", handle.transform, UIKit.Circle, Color.white);
            UIKit.Stretch(hIn.rectTransform, 6, 6, 6, 6);
            var hrt = handle.rectTransform; hrt.anchorMin = new Vector2(0, -0.45f); hrt.anchorMax = new Vector2(0, 1.45f);

            musicSlider = track.gameObject.AddComponent<Slider>();
            musicSlider.fillRect = fill.rectTransform;
            musicSlider.handleRect = hrt;
            musicSlider.targetGraphic = handle;
            musicSlider.direction = Slider.Direction.LeftToRight;
            musicSlider.minValue = 0f; musicSlider.maxValue = 1f;
            musicSlider.transition = Selectable.Transition.None;
            musicSlider.SetValueWithoutNotify(AudioFx.MusicLevel);
            musicSlider.onValueChanged.AddListener(v => AudioFx.MusicLevel = v);
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(es);
        }
    }
}

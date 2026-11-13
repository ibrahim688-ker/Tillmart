using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace CiftlikMarket
{
    /// <summary>
    /// Toptancı sistemi (GDD 3.13): bakkal ürünleri (çay, şeker, makarna, pirinç, yağ, çikolata) çiftlikte
    /// üretilmez, tabletten toptancıdan sipariş edilir. Üç toptancı fiyat / teslim süresi / en az sipariş
    /// koşullarıyla ayrışır. Sipariş verilince kamyonet marketin arkasına gelir, ürünleri teslim alanına
    /// bırakır; oyuncu ya da depocu raflara taşır. Bekleyen siparişler kaydedilir (oyun kapalıyken gelen
    /// sipariş açılınca teslim alanında hazır olur).
    /// </summary>
    public class Wholesale : MonoBehaviour
    {
        public static Wholesale I;
        public static bool IsOpen { get; private set; }

        public static readonly ItemType[] Goods = { ItemType.Makarna, ItemType.Seker, ItemType.Cikolata, ItemType.Pirinc, ItemType.Cay, ItemType.Yag };
        public const int CaseSize = 6;   // bir koli = 6 adet

        public static bool IsWholesale(ItemType t)
        {
            foreach (var g in Goods) if (g == t) return true;
            return false;
        }

        class Supplier
        {
            public string name, timeText, lockText; public float factor, seconds; public int minCases, maxCases;
            public Color col, lip;
        }

        static readonly Supplier[] Suppliers =
        {
            new Supplier { name = "MAHALLE\nDEPOSU",    factor = 0.80f, seconds = 8f,   minCases = 1,  maxCases = 4,  timeText = "Hemen",  lockText = "",                          col = UIKit.Hex("4CC97A"), lip = UIKit.Hex("2B9A55") },
            new Supplier { name = "ŞEHİR\nTOPTANCISI",  factor = 0.60f, seconds = 60f,  minCases = 4,  maxCases = 12, timeText = "1 dk",   lockText = "2 bakkal rafı açılınca",     col = UIKit.Hex("4DA6FF"), lip = UIKit.Hex("2A78CF") },
            new Supplier { name = "HAL /\nFABRİKA",     factor = 0.45f, seconds = 150f, minCases = 10, maxCases = 30, timeText = "2,5 dk", lockText = "Değirmen bölgesi açılınca", col = UIKit.Hex("A77BFF"), lip = UIKit.Hex("7A4FD6") },
        };

        class Order { public int supplier; public long arriveTicks; public int[] cases = new int[Goods.Length]; public bool delivering; }
        readonly List<Order> orders = new List<Order>();

        int selected;
        readonly int[] cart = new int[Goods.Length];
        bool vanBusy;

        // ---------- açılma koşulları ----------

        /// <summary>Herhangi bir bakkal rafı açık mı? (tablet butonu buna göre görünür)</summary>
        public static bool Available
        {
            get
            {
                foreach (var s in Shelf.All) if (IsWholesale(s.type)) return true;
                return false;
            }
        }

        static int OpenShelfTypes()
        {
            int n = 0;
            foreach (var g in Goods)
                foreach (var s in Shelf.All) if (s.type == g) { n++; break; }
            return n;
        }

        static bool ShelfOpen(ItemType t)
        {
            foreach (var s in Shelf.All) if (s.type == t) return true;
            return false;
        }

        static bool SupplierUnlocked(int i)
        {
            if (i == 0) return true;
            if (i == 1) return OpenShelfTypes() >= 2;
            return GameManager.I != null && GameManager.I.IsUnlocked("genisleme_3");
        }

        /// <summary>Raflardan biri azaldı ve yolda sipariş yok mu? (tablet butonunda uyarı)</summary>
        public static bool NeedsRestock
        {
            get
            {
                if (I == null || I.orders.Count > 0) return false;
                int dock = DeliveryDock.I != null ? DeliveryDock.I.Count : 0;
                if (dock > 0) return false;
                foreach (var s in Shelf.All)
                    if (IsWholesale(s.type) && s.container.Count <= s.container.capacity / 3) return true;
                return false;
            }
        }

        public static int UnitCost(int supplier, ItemType t)
        {
            var d = GameConfig.I != null ? GameConfig.I.Def(t) : null;
            int p = d != null ? d.price : 5;
            return Mathf.Max(1, Mathf.RoundToInt(p * Suppliers[supplier].factor));
        }

        int CartCases() { int n = 0; foreach (var c in cart) n += c; return n; }
        int CartCost() { int t = 0; for (int i = 0; i < Goods.Length; i++) t += cart[i] * CaseSize * UnitCost(selected, Goods[i]); return t; }
        Order PendingOf(int supplier) { foreach (var o in orders) if (o.supplier == supplier) return o; return null; }

        // ---------- yaşam döngüsü ----------

        void Awake()
        {
            I = this;
            BuildUI();
            panel.SetActive(false);
            LoadOrders();
        }

        void Update()
        {
            long now = DateTime.UtcNow.Ticks;
            if (!vanBusy)
                foreach (var o in orders)
                    if (!o.delivering && o.arriveTicks <= now && DeliveryDock.I != null && DeliveryDock.I.isActiveAndEnabled)
                    {
                        // oyun kapalıyken çoktan gelmişse kamyonet beklemeden teslim alanında hazır olsun
                        bool instant = Time.timeSinceLevelLoad < 3f;
                        StartCoroutine(Deliver(o, instant));
                        break;
                    }
            if (IsOpen)
            {
                refreshTimer -= Time.unscaledDeltaTime;
                if (refreshTimer <= 0f) { refreshTimer = 0.5f; Refresh(); }
            }
        }

        float refreshTimer;

        IEnumerator Deliver(Order o, bool instant)
        {
            vanBusy = true;
            o.delivering = true;
            var dock = DeliveryDock.I;
            GameObject van = null;
            // kendi kamyon yolundan (kuzeyden) gelir, çeyrek daire sapaktan depo kepenginin önüne döner
            Vector3 stop = new Vector3(dock.transform.position.x, 0f, ServiceRoadZ);
            var path = TruckPath(stop);
            Vector3 start = path[0];
            if (!instant)
            {
                van = MakeVan();
                if (van != null)
                {
                    van.transform.position = start;
                    yield return DrivePath(van.transform, path, 3.6f, false);
                    AudioFx.Play(Sfx.Horn, 0.35f);
                }
            }
            var cfg = GameConfig.I;
            Vector3 from = stop + Vector3.up * 1.6f;
            for (int i = 0; i < Goods.Length; i++)
                for (int k = 0; k < o.cases[i] * CaseSize; k++)
                {
                    var it = cfg.Spawn(Goods[i], instant ? dock.pile.transform.position : from);
                    dock.Receive(it, !instant);
                    if (!instant) yield return new WaitForSeconds(0.04f);
                }
            orders.Remove(o);
            SaveOrders();
            if (GameManager.I != null) GameManager.I.Save();
            if (!instant) { Haptics.Light(); AudioFx.Play(Sfx.Drop, 0.6f); }
            if (van != null)
            {
                yield return new WaitForSeconds(0.4f);
                path.Reverse();
                yield return DrivePath(van.transform, path, 4.2f, true);   // geri geri aynı yoldan çıkar
                Destroy(van);
            }
            vanBusy = false;
        }

        const float TruckRoadX = -0.3f;      // kamyonun kendi yolu (ana yola ve drive-thru kuyruğuna değmez)
        const float ServiceRoadZ = 27.7f;    // depo odasının arkasındaki servis yolu

        const float TurnRadius = 3f;         // sapağın çeyrek daire yarıçapı (yol ortası)

        /// <summary>Kamyon yolu: kuzeyden iner, çeyrek daireyle batıya döner, kepengin önünde biter.</summary>
        static List<Vector3> TruckPath(Vector3 stop)
        {
            var pts = new List<Vector3> { new Vector3(TruckRoadX, 0f, ServiceRoadZ + 40f), new Vector3(TruckRoadX, 0f, ServiceRoadZ + TurnRadius) };
            float cx = TruckRoadX - TurnRadius, cz = ServiceRoadZ + TurnRadius;
            for (int i = 1; i <= 8; i++)
            {
                float a = -Mathf.PI * 0.5f * i / 8f;
                pts.Add(new Vector3(cx + TurnRadius * Mathf.Cos(a), 0f, cz + TurnRadius * Mathf.Sin(a)));
            }
            pts.Add(stop);
            return pts;
        }

        /// <summary>Noktalar boyunca yumuşak hızlanıp yavaşlayarak sürer; burun gidiş yönüne (geri viteste tersine) bakar.</summary>
        static IEnumerator DrivePath(Transform t, List<Vector3> pts, float dur, bool reverse)
        {
            var cum = new float[pts.Count];
            for (int i = 1; i < pts.Count; i++) cum[i] = cum[i - 1] + Vector3.Distance(pts[i - 1], pts[i]);
            float total = Mathf.Max(0.01f, cum[pts.Count - 1]);
            float k = 0f;
            int seg = 1;
            while (true)
            {
                k = Mathf.Min(1f, k + Time.deltaTime / dur);
                float d = k * k * (3f - 2f * k) * total;
                while (seg < pts.Count - 1 && cum[seg] < d) seg++;
                float span = Mathf.Max(0.0001f, cum[seg] - cum[seg - 1]);
                t.position = Vector3.Lerp(pts[seg - 1], pts[seg], Mathf.Clamp01((d - cum[seg - 1]) / span));
                var dir = pts[seg] - pts[seg - 1]; dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f) t.rotation = Quaternion.LookRotation(reverse ? -dir : dir);
                if (k >= 1f) break;
                yield return null;
            }
        }

        GameObject MakeVan()
        {
            var dt = FindAnyObjectByType<DriveThru>(FindObjectsInactive.Include);
            if (dt == null) return null;
            var pf = dt.vanPrefab != null ? dt.vanPrefab : dt.carPrefab;
            if (pf == null) return null;
            var go = Instantiate(pf);
            var car = go.GetComponent<DriveThruCar>();
            if (car != null)
            {
                if (car.barRoot) car.barRoot.SetActive(false);
                if (car.label) car.label.gameObject.SetActive(false);
                if (car.body)
                {
                    var mpb = new MaterialPropertyBlock();
                    mpb.SetColor("_BaseColor", UIKit.Hex("F2F2F2"));
                    car.body.SetPropertyBlock(mpb);
                }
                Destroy(car);
            }
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            return go;
        }

        // ---------- kayıt ----------

        void SaveOrders()
        {
            var sb = new StringBuilder();
            foreach (var o in orders)
            {
                if (sb.Length > 0) sb.Append(';');
                sb.Append(o.supplier).Append(':').Append(o.arriveTicks).Append(':');
                for (int i = 0; i < Goods.Length; i++) { if (i > 0) sb.Append(','); sb.Append(o.cases[i]); }
            }
            PlayerPrefs.SetString(SaveKeys.P("cm_ws_orders"), sb.ToString());
        }

        void LoadOrders()
        {
            orders.Clear();
            var s = PlayerPrefs.GetString(SaveKeys.P("cm_ws_orders"), "");
            if (s.Length == 0) return;
            foreach (var part in s.Split(';'))
            {
                var f = part.Split(':');
                if (f.Length < 3) continue;
                var o = new Order();
                int.TryParse(f[0], out o.supplier);
                long.TryParse(f[1], out o.arriveTicks);
                var c = f[2].Split(',');
                for (int i = 0; i < Goods.Length && i < c.Length; i++) int.TryParse(c[i], out o.cases[i]);
                orders.Add(o);
            }
        }

        // ---------- sipariş ----------

        void PlaceOrder()
        {
            var sp = Suppliers[selected];
            int cases = CartCases(), cost = CartCost();
            if (!SupplierUnlocked(selected) || PendingOf(selected) != null || cases < sp.minCases || cases > sp.maxCases) return;
            if (GameManager.I.Money < cost) return;
            GameManager.I.Spend(cost);
            var o = new Order { supplier = selected, arriveTicks = DateTime.UtcNow.AddSeconds(sp.seconds).Ticks };
            for (int i = 0; i < Goods.Length; i++) { o.cases[i] = cart[i]; cart[i] = 0; }
            orders.Add(o);
            SaveOrders();
            GameManager.I.Save();
            AudioFx.Play(Sfx.Cash, 0.7f);
            Haptics.Medium();
            Refresh();
        }

        void Step(int i, int d)
        {
            var sp = Suppliers[selected];
            if (d > 0 && CartCases() >= sp.maxCases) return;
            cart[i] = Mathf.Max(0, cart[i] + d);
            Refresh();
        }

        // ---------- arayüz ----------

        GameObject panel;
        class SupCard { public Chunk chunk; public Text name, info, status; public Image lockImg; }
        class Row { public GameObject go; public Image icon; public Text name, sub, count; public Chunk minus, plus; }
        readonly SupCard[] supCards = new SupCard[3];
        readonly Row[] rows = new Row[Goods.Length];
        Text totalText, noteText, emptyText;
        Chunk orderBtn;
        Text orderText;

        public void Open()
        {
            if (IsOpen) return;
            if (!SupplierUnlocked(selected)) selected = 0;
            panel.SetActive(true);
            IsOpen = true;
            Refresh();
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

        static string Clock(double sec)
        {
            int s = Mathf.Max(0, Mathf.CeilToInt((float)sec));
            return (s / 60) + ":" + (s % 60).ToString("00");
        }

        void Refresh()
        {
            long now = DateTime.UtcNow.Ticks;
            for (int i = 0; i < 3; i++)
            {
                var c = supCards[i]; var sp = Suppliers[i];
                bool open = SupplierUnlocked(i);
                bool sel = i == selected;
                UIKit.SetChunkColor(c.chunk, open ? (sel ? sp.col : Color.white) : UIKit.Gray, open ? sp.lip : UIKit.GrayLip);
                c.name.color = open && sel ? Color.white : UIKit.TextDark;
                c.info.color = open && sel ? Color.white : UIKit.TextSoft;
                c.lockImg.gameObject.SetActive(!open);
                c.info.text = open
                    ? "Fiyat <b>%" + Mathf.RoundToInt(sp.factor * 100) + "</b>\nTeslim <b>" + sp.timeText + "</b>\nEn az <b>" + sp.minCases + " koli</b>"
                    : sp.lockText;
                var p = PendingOf(i);
                c.status.text = p == null ? "" : (p.delivering ? "Geldi!" : "Yolda " + Clock(new TimeSpan(Math.Max(0, p.arriveTicks - now)).TotalSeconds));
            }

            int shown = 0;
            for (int i = 0; i < Goods.Length; i++)
            {
                var r = rows[i]; var t = Goods[i];
                bool open = ShelfOpen(t);
                r.go.SetActive(open);
                if (!open) continue;
                shown++;
                int stock = 0, cap = 0;
                foreach (var s in Shelf.All) if (s.type == t) { stock += s.container.Count; cap += s.container.capacity; }
                int dock = DeliveryDock.I != null ? DeliveryDock.I.CountOf(t) : 0;
                r.name.text = GameConfig.I.Name(t);
                r.sub.text = "Koli (" + CaseSize + " adet): <b>" + UIKit.Money(UnitCost(selected, t) * CaseSize) + " ₺</b>   ·   Rafta " + stock + "/" + cap + (dock > 0 ? "  ·  Depoda " + dock : "");
                r.count.text = cart[i].ToString();
            }
            emptyText.gameObject.SetActive(shown == 0);

            var spSel = Suppliers[selected];
            int cases = CartCases(), cost = CartCost();
            bool busy = PendingOf(selected) != null;
            bool canOrder = SupplierUnlocked(selected) && !busy && cases >= spSel.minCases && cases <= spSel.maxCases && GameManager.I != null && GameManager.I.Money >= cost;
            totalText.text = cases + " koli  ·  <b>" + UIKit.Money(cost) + " ₺</b>";
            noteText.text = busy ? "Bu toptancıdan sipariş yolda; gelince yenisini verebilirsin."
                          : cases < spSel.minCases ? "Bu toptancıdan en az " + spSel.minCases + " koli alınır."
                          : cases > spSel.maxCases ? "En fazla " + spSel.maxCases + " koli."
                          : GameManager.I != null && GameManager.I.Money < cost ? "Paran yetmiyor."
                          : "Ürünler marketin arkasındaki depoya gelir.";
            orderBtn.Button.interactable = canOrder;
            UIKit.SetChunkColor(orderBtn, canOrder ? UIKit.Green : UIKit.Gray, canOrder ? UIKit.GreenLip : UIKit.GrayLip);
        }

        void BuildUI()
        {
            var canvas = UIKit.MakeCanvas("WholesaleCanvas", transform, 46);
            var p = UIKit.MakePopup(canvas.transform, "Dim", "TOPTANCI", UIKit.Hex("FF9F45"), UIKit.Hex("D4701C"), new Vector2(960, 1580), 0f);
            panel = p.Dim.gameObject;
            p.Card.name = "Card";
            p.Dim.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            DailyReward.MakeCloseButton(p.Card, Close);

            // toptancı kartları
            for (int i = 0; i < 3; i++)
            {
                var c = new SupCard();
                int idx = i;
                c.chunk = UIKit.MakeButton("Sup" + i, p.Body, Color.white, UIKit.CreamLip, 40f, () => { if (SupplierUnlocked(idx)) { selected = idx; Refresh(); } }, 6f, 10f);
                UIKit.Place(c.chunk.Rect, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2((i - 1) * 290f, -96), new Vector2(276, 330));
                var face = c.chunk.Face.transform;
                var icon = UIKit.Img("Van", face, UIKit.Icon("drive_thru"), Color.white);
                icon.preserveAspect = true;
                UIKit.Place(icon.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(110, 84));
                c.name = UIKit.Label("Name", face, Suppliers[i].name, 30, UIKit.TextDark, TextAnchor.UpperCenter);
                UIKit.Place(c.name.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -92), new Vector2(260, 76));
                c.info = UIKit.Label("Info", face, "", 25, UIKit.TextSoft, TextAnchor.UpperCenter, false, true);
                UIKit.Place(c.info.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(256, 110));
                c.status = UIKit.Label("Status", c.chunk.Rect, "", 26, UIKit.Hex("C9343B"), TextAnchor.MiddleCenter, false);
                UIKit.Place(c.status.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(276, 40));
                c.lockImg = UIKit.Img("Lock", c.chunk.Rect, UIKit.Lock, UIKit.Hex("5B6270"));
                UIKit.Place(c.lockImg.rectTransform, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-30, -30), new Vector2(48, 56));
                supCards[i] = c;
            }

            // ürün satırları
            for (int i = 0; i < Goods.Length; i++)
            {
                var r = new Row();
                var card = UIKit.MakeChunk("Good" + i, p.Body, Color.white, UIKit.CreamLip, 40f, 6f, 8f, false);
                UIKit.Place(card.Rect, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -478 - i * 140), new Vector2(860, 128));
                r.go = card.Root.gameObject;
                var face = card.Face.transform;
                var disc = UIKit.Img("Disc", face, UIKit.Circle, UIKit.Hex("FFF1CF"));
                UIKit.Place(disc.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(100, 100));
                r.icon = UIKit.Img("Icon", disc.transform, UIKit.Icon("urun_" + Goods[i].ToString().ToLowerInvariant()), Color.white);
                r.icon.preserveAspect = true;
                UIKit.Stretch(r.icon.rectTransform, 8, 8, 8, 8);
                r.name = UIKit.Label("Name", face, "", 36, UIKit.TextDark, TextAnchor.MiddleLeft);
                UIKit.Place(r.name.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(128, -10), new Vector2(420, 50));
                r.sub = UIKit.Label("Sub", face, "", 24, UIKit.TextSoft, TextAnchor.MiddleLeft, false, true);
                UIKit.Place(r.sub.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(128, -60), new Vector2(470, 44));
                int idx = i;
                r.minus = UIKit.MakeButton("Minus", face, UIKit.Hex("FF8A8A"), UIKit.RedLip, 40f, () => Step(idx, -1), 5f, 8f);
                UIKit.Place(r.minus.Rect, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-210, 2), new Vector2(84, 88));
                var mi = UIKit.Img("M", r.minus.Face.transform, UIKit.Round, Color.white, 62f / 7f);
                UIKit.Place(mi.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 12));
                r.count = UIKit.Label("Count", face, "0", 42, UIKit.TextDark, TextAnchor.MiddleCenter);
                UIKit.Place(r.count.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-112, 2), new Vector2(90, 70));
                r.plus = UIKit.MakeButton("Plus", face, UIKit.Green, UIKit.GreenLip, 40f, () => Step(idx, 1), 5f, 8f);
                UIKit.Place(r.plus.Rect, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 2), new Vector2(84, 88));
                var pi = UIKit.Img("P", r.plus.Face.transform, UIKit.Plus, Color.white);
                UIKit.Stretch(pi.rectTransform, 20, 20, 20, 20);
                rows[i] = r;
            }
            emptyText = UIKit.Label("Empty", p.Body, "Bakkal rafı açınca ürünler burada görünür.", 32, UIKit.TextSoft, TextAnchor.MiddleCenter, false, true);
            UIKit.Place(emptyText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -600), new Vector2(800, 120));

            // alt satır: toplam + sipariş
            totalText = UIKit.Label("Total", p.Body, "", 38, UIKit.TextDark, TextAnchor.MiddleLeft);
            UIKit.Place(totalText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(50, 150), new Vector2(460, 60));
            noteText = UIKit.Label("Note", p.Body, "", 24, UIKit.TextSoft, TextAnchor.UpperLeft, false, true);
            UIKit.Place(noteText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(50, 54), new Vector2(480, 90));
            orderBtn = UIKit.MakeButton("Order", p.Body, UIKit.Green, UIKit.GreenLip, 56f, PlaceOrder, 6f, 12f);
            UIKit.Place(orderBtn.Rect, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 50), new Vector2(330, 136));
            orderText = UIKit.Label("T", orderBtn.Face.transform, "SİPARİŞ VER", 40, Color.white, TextAnchor.MiddleCenter, true);
            UIKit.Stretch(orderText.rectTransform, 6, -4, 6, 0);
        }
    }
}

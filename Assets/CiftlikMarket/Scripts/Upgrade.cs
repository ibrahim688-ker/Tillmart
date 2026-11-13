using System.Collections.Generic;
using UnityEngine;

namespace CiftlikMarket
{
    public enum UpgradeKind { PlayerCapacity, PlayerSpeed, StationSlot, WorkerCapacity, WorkerSpeed, WorkerCount, OfflineCapacity, ShelfCapacity,
        MachineSpeed, OutputTray, FieldRegrow, CustomerAttraction, DriveThruRate, ProductValue, SecondCheckout, SecondDriveThru, FieldSize, PlayerFlow, CashierSpeed }

    /// <summary>
    /// Ofis bilgisayarından satın alınan seviyeli yükseltme. Maliyet = taban × büyüme^seviye.
    /// Seviye PlayerPrefs'e kaydedilir ve oyun açılınca yeniden uygulanır.
    /// </summary>
    public class Upgrade : MonoBehaviour
    {
        public static readonly List<Upgrade> All = new List<Upgrade>();

        public string id;
        public string title;
        public UpgradeKind kind;
        public int baseCost = 50;
        public float growth = 1.25f;
        public int maxLevel = 5;
        public int sortOrder;

        [Header("Hedefler")]
        public Converter station;       // StationSlot
        public Worker workerTemplate;   // Worker* türleri
        public ItemType shelfType;      // ShelfCapacity / ProductValue: bu ürünün bütün rafları (ikinci raflar dahil)
        public ResourceField field;     // FieldRegrow
        [Tooltip("SecondCheckout: satın alınınca açılan nesneler (ikinci kasa ve kasiyeri)")]
        public GameObject[] activate;

        const int ShelfBase = 6, ShelfStep = 6;

        public int Level { get; private set; }
        public bool IsMaxed => Level >= maxLevel;
        public int Cost => Mathf.RoundToInt(baseCost * Mathf.Pow(growth, Level));

        float checkTimer;
        bool shelvesInit;
        int baseIn = -1, baseOut = -1;   // istasyonun sahnedeki ilk kapasiteleri (makineye göre değişir)
        float baseTime = -1f;             // makinenin ilk üretim süresi / tarlanın ilk dolma süresi
        float baseInterval = -1f;
        DriveThru dtRef;         // müşteri / araba geliş aralığı

        const float SpeedStep = 0.12f;    // hız yükseltmesi: her seviye süreyi %12 kısaltır (5 seviye → ×0,4)

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Start()
        {
            Level = PlayerPrefs.GetInt(SaveKeys.P("cm_up_" + id), 0);
            Apply();
        }

        void Update()
        {
            // Bakıcı sonradan işe alınırsa (ya da kayıttan yüklenirse) ek bakıcıları oluştur;
            // raf kapasitesi sonradan açılan raflara da uygulansın
            if ((kind != UpgradeKind.WorkerCount && kind != UpgradeKind.ShelfCapacity) || Level == 0) return;
            checkTimer -= Time.deltaTime;
            if (checkTimer > 0f) return;
            checkTimer = 1f;
            Apply();
        }

        /// <summary>Şu an listede gösterilmeli mi? (ör. bakıcı işe alınmadan bakıcı yükseltmesi yok)</summary>
        public bool Available
        {
            get
            {
                switch (kind)
                {
                    case UpgradeKind.StationSlot:
                    case UpgradeKind.MachineSpeed:
                    case UpgradeKind.OutputTray: return station && station.gameObject.activeInHierarchy;
                    case UpgradeKind.FieldRegrow:
                    case UpgradeKind.FieldSize: return field && field.gameObject.activeInHierarchy;
                    case UpgradeKind.DriveThruRate:
                        if (!dtRef) dtRef = FindAnyObjectByType<DriveThru>(FindObjectsInactive.Include);
                        return dtRef && dtRef.gameObject.activeInHierarchy;
                    case UpgradeKind.SecondCheckout: return GameManager.I != null && GameManager.I.IsUnlocked("kasiyer");
                    case UpgradeKind.SecondDriveThru: return GameManager.I != null && GameManager.I.IsUnlocked("gorevli");
                    case UpgradeKind.WorkerCapacity:
                    case UpgradeKind.WorkerSpeed:
                    case UpgradeKind.WorkerCount:
                    case UpgradeKind.CashierSpeed: return workerTemplate && workerTemplate.gameObject.activeInHierarchy;
                    case UpgradeKind.OfflineCapacity: return GameManager.I != null && GameManager.I.IsUnlocked("kasiyer");
                    case UpgradeKind.ShelfCapacity:
                    case UpgradeKind.ProductValue:
                        foreach (var s in Shelf.All) if (s.type == shelfType) return true;
                        return false;
                    default: return true;
                }
            }
        }

        public bool TryBuy()
        {
            if (IsMaxed || !Available) return false;
            int c = Cost;
            if (GameManager.I.Money < c) return false;
            GameManager.I.Spend(c);
            Level++;
            AudioFx.Play(Sfx.LevelUp);
            Haptics.Medium();
            PlayerPrefs.SetInt(SaveKeys.P("cm_up_" + id), Level);
            GameManager.I.Save();
            Quests.Report(QuestType.BuyUpgrades);
            foreach (var pc in Carrier.All) if (pc.isPlayer) { Fx.Confetti(pc.transform.position, false); break; }
            Apply();
            return true;
        }

        public string Describe()
        {
            string name;
            switch (kind)
            {
                case UpgradeKind.PlayerCapacity: name = "Taşıma kapasitesi"; break;
                case UpgradeKind.PlayerSpeed: name = "Yürüme hızı"; break;
                case UpgradeKind.PlayerFlow: name = "Alma/bırakma"; break;
                case UpgradeKind.StationSlot: name = "Yem slotu"; break;
                case UpgradeKind.WorkerCapacity: name = "Taşıma kapasitesi"; break;
                case UpgradeKind.WorkerSpeed: name = "Yürüme hızı"; break;
                case UpgradeKind.CashierSpeed: name = "Ödeme süresi"; break;
                case UpgradeKind.OfflineCapacity: name = "Sen yokken birikim"; break;
                case UpgradeKind.ShelfCapacity: name = "Raf kapasitesi"; break;
                case UpgradeKind.MachineSpeed: name = "Üretim süresi"; break;
                case UpgradeKind.OutputTray: name = "Çıkış sepeti"; break;
                case UpgradeKind.FieldRegrow: name = "Dolma süresi"; break;
                case UpgradeKind.FieldSize: name = "Parsel"; break;
                case UpgradeKind.CustomerAttraction: name = AttractionName(Level + (IsMaxed ? 0 : 1)) + " · müşteri aralığı"; break;
                case UpgradeKind.DriveThruRate: name = "Araba aralığı"; break;
                case UpgradeKind.ProductValue: name = "Satış fiyatı"; break;
                case UpgradeKind.SecondCheckout: return IsMaxed ? "İkinci kasa ve kasiyeri çalışıyor" : "Sıra ikiye bölünür, kasiyeri de gelir";
                case UpgradeKind.SecondDriveThru: return IsMaxed ? "İkinci drive-thru şeridi ve görevlisi çalışıyor" : "Alttan yeni yol ve pencere: iki araba aynı anda";
                default: name = "Bakıcı sayısı"; break;
            }
            return IsMaxed ? name + ": " + Value(Level) + "  (maks)" : name + ": " + Value(Level) + "  →  " + Value(Level + 1);
        }

        string Value(int lv)
        {
            switch (kind)
            {
                case UpgradeKind.PlayerCapacity: return (4 + 2 * lv).ToString();
                case UpgradeKind.PlayerSpeed: return (4.5f + 0.3f * lv).ToString("0.0");
                case UpgradeKind.PlayerFlow: return (1f / Carrier.FlowInterval(lv)).ToString("0.#") + "\u00A0adet/sn";
                case UpgradeKind.StationSlot: return ((baseIn > 0 ? baseIn : 4) + 4 * lv).ToString();
                case UpgradeKind.WorkerCapacity: return ((workerTemplate ? workerTemplate.baseCapacity : 3) + lv).ToString();
                case UpgradeKind.WorkerSpeed: return ((workerTemplate ? workerTemplate.baseSpeed : 2.5f) + 0.5f * lv).ToString("0.0");
                case UpgradeKind.CashierSpeed: return (0.5f * Checkout.PayMul(lv)).ToString("0.0#") + "\u00A0sn";
                case UpgradeKind.OfflineCapacity: return (1 + lv) + " saat";
                case UpgradeKind.ShelfCapacity: return (ShelfBase + ShelfStep * lv).ToString();
                case UpgradeKind.MachineSpeed: return (BaseTime() * SpeedMul(lv)).ToString("0.0") + "\u00A0sn";
                case UpgradeKind.OutputTray: return ((baseOut > 0 ? baseOut : 8) + 4 * lv).ToString();
                case UpgradeKind.FieldRegrow: return (BaseTime() * SpeedMul(lv)).ToString("0.0") + "\u00A0sn";
                case UpgradeKind.FieldSize: return ResourceField.SlotsFor(lv) + "\u00A0ürün";
                case UpgradeKind.CustomerAttraction: return CustomerInterval(lv).ToString("0.0") + "\u00A0sn";
                case UpgradeKind.DriveThruRate: return CarInterval(lv).ToString("0") + "\u00A0sn";
                case UpgradeKind.ProductValue: return GameConfig.I != null ? GameConfig.I.PriceAt(shelfType, lv).ToString() : "-";
                default: return (1 + lv).ToString();
            }
        }

        static float SpeedMul(int lv) => Mathf.Max(0.4f, 1f - SpeedStep * lv);
        float CustomerInterval(int lv) => Mathf.Max(2.5f, (baseInterval > 0f ? baseInterval : 8f) - 0.9f * lv);
        float CarInterval(int lv) => Mathf.Max(25f, (baseInterval > 0f ? baseInterval : 55f) - 5f * lv);

        float BaseTime()
        {
            if (baseTime < 0f)
            {
                if (kind == UpgradeKind.MachineSpeed && station) baseTime = station.unitTime;
                else if (kind == UpgradeKind.FieldRegrow && field) baseTime = field.regrowTime;
            }
            return baseTime > 0f ? baseTime : 1f;
        }

        /// <summary>Müşteri çekiciliği seviyesine göre reklam türü (Tabela → Radyo → TV).</summary>
        public static string AttractionName(int lv) => lv <= 1 ? "Pano" : (lv <= 3 ? "Radyo" : "TV");

        void Apply()
        {
            switch (kind)
            {
                case UpgradeKind.MachineSpeed:
                    if (station) { BaseTime(); station.unitTime = baseTime * SpeedMul(Level); }
                    break;
                case UpgradeKind.OutputTray:
                    if (station)
                    {
                        if (baseOut < 0) baseOut = station.outputTray.capacity;
                        station.outputTray.capacity = baseOut + 4 * Level;
                    }
                    break;
                case UpgradeKind.FieldRegrow:
                    if (field) { BaseTime(); field.regrowTime = baseTime * SpeedMul(Level); }
                    break;
                case UpgradeKind.FieldSize:
                    if (field) field.SetSizeLevel(Level);
                    break;
                case UpgradeKind.CustomerAttraction:
                {
                    var sp = FindAnyObjectByType<CustomerSpawner>(FindObjectsInactive.Include);
                    if (sp) { if (baseInterval < 0f) baseInterval = sp.interval; sp.interval = CustomerInterval(Level); }
                    // ilk seviyede yol kenarına reklam panosu dikilir
                    if (Level > 0 && activate != null)
                        foreach (var go in activate)
                            if (go && !go.activeSelf) { go.SetActive(true); if (Time.timeSinceLevelLoad > 1f) go.AddComponent<PopAnim>(); }
                    break;
                }
                case UpgradeKind.DriveThruRate:
                {
                    foreach (var dt in FindObjectsByType<DriveThru>(FindObjectsInactive.Include, FindObjectsSortMode.None))   // iki şerit de
                    { if (baseInterval < 0f) baseInterval = dt.interval; dt.interval = CarInterval(Level); }
                    break;
                }
                case UpgradeKind.ProductValue:
                    GameConfig.ValueLevel[shelfType] = Level;
                    break;
                case UpgradeKind.SecondCheckout:
                case UpgradeKind.SecondDriveThru:
                    if (Level > 0 && activate != null)
                        foreach (var go in activate)
                            if (go && !go.activeSelf)
                            {
                                go.SetActive(true);
                                if (Time.timeSinceLevelLoad <= 1f) continue;
                                var w = go.GetComponent<Worker>();
                                if (w != null) HireIntro.Play(w);   // 2. kasiyer / 2. görevli kapıdan girer
                                else go.AddComponent<PopAnim>();
                            }
                    break;
                case UpgradeKind.PlayerCapacity:
                    foreach (var c in Carrier.All) if (c.isPlayer) c.stack.capacity = 4 + 2 * Level;
                    break;
                case UpgradeKind.PlayerFlow:
                    Carrier.PlayerFlowLevel = Level;
                    break;
                case UpgradeKind.PlayerSpeed:
                {
                    var pc = FindAnyObjectByType<PlayerController>();
                    if (pc) pc.speed = 4.5f + 0.3f * Level;
                    break;
                }
                case UpgradeKind.StationSlot:
                    if (station)
                    {
                        if (baseIn < 0) baseIn = station.inputCapacity;
                        station.inputCapacity = baseIn + 4 * Level;
                        station.inputPile.capacity = station.inputCapacity;
                        if (station.inputPile2 != null) station.inputPile2.capacity = station.inputCapacity;
                    }
                    break;
                case UpgradeKind.WorkerCapacity:
                    if (workerTemplate) Worker.SetCapacityBonus(workerTemplate.role, Level);
                    break;
                case UpgradeKind.WorkerSpeed:
                    if (workerTemplate) Worker.SetSpeedBonus(workerTemplate.role, 0.5f * Level);
                    break;
                case UpgradeKind.CashierSpeed:
                    Checkout.PaySpeedLevel = Level;
                    break;
                case UpgradeKind.WorkerCount:
                    EnsureWorkerCount(1 + Level);
                    break;
                case UpgradeKind.OfflineCapacity:
                    OfflineEarnings.CapHours = 1 + Level;
                    break;
                case UpgradeKind.ShelfCapacity:
                {
                    int cap = ShelfBase + ShelfStep * Level;
                    if (!shelvesInit)
                    {
                        // kapalı (henüz açılmamış) raflar da baştan ayarlansın
                        shelvesInit = true;
                        foreach (var s in FindObjectsByType<Shelf>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                            if (s.type == shelfType) s.container.capacity = cap;
                    }
                    foreach (var s in Shelf.All) if (s.type == shelfType) s.container.capacity = cap;
                    break;
                }
            }
        }

        void EnsureWorkerCount(int wanted)
        {
            if (!workerTemplate || !workerTemplate.gameObject.activeInHierarchy) return;
            int count = 0;
            foreach (var w in FindObjectsByType<Worker>(FindObjectsSortMode.None))
                if (w.role == workerTemplate.role) count++;
            while (count < wanted)
            {
                var offset = new Vector3(0.9f * count, 0f, -0.6f * count);
                var clone = Instantiate(workerTemplate.gameObject, workerTemplate.idlePoint + offset, workerTemplate.transform.rotation);
                clone.name = workerTemplate.name + "_" + (count + 1);
                var w = clone.GetComponent<Worker>();
                w.idlePoint = workerTemplate.idlePoint + offset;
                w.carrier.stack.items.Clear();
                foreach (Transform child in w.carrier.stack.transform) Destroy(child.gameObject);
                if (Time.timeSinceLevelLoad > 2f) HireIntro.Play(w);   // +1 bakıcı kapıdan girer
                else clone.AddComponent<PopAnim>();
                count++;
            }
        }
    }
}

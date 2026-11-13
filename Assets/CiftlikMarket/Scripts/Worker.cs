using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>
    /// Çalışan. Kasiyer kasaya geçip orada durur.
    /// Çiftlik bakıcısı her işi bitirdiğinde en acil işi seçer:
    ///  1) Elinde ürün varsa: onu alan rafa ya da hayvana götür (yoksa çöpe at).
    ///  2) Eli boşsa: yarıdan az dolu bir raf varsa kaynağından (hayvan tepsisi / tarla) ürün topla;
    ///     yoksa önü boşalan hayvan için tarladan yem topla; o da yoksa en boş rafı doldur.
    /// </summary>
    public class Worker : MonoBehaviour
    {
        public enum Role { Cashier, Farmhand, DriveThru, Stocker }
        enum Job { None, Collect, Deliver, Dump, CollectShelf, Return }

        public Role role;
        public float speed = 2.5f;
        public float baseSpeed = 2.5f;
        public int baseCapacity = 3;

        /// <summary>Çalışan yükseltmelerinden gelen bonuslar: her çalışan türü (rol) kendi yükseltmesini alır.</summary>
        static readonly float[] speedBonus = new float[4];
        static readonly int[] capBonus = new int[4];
        public static void SetSpeedBonus(Role r, float v) { speedBonus[(int)r] = v; }
        public static void SetCapacityBonus(Role r, int v) { capBonus[(int)r] = v; }
        float SpeedBonus => speedBonus[(int)role];
        int CapacityBonus => capBonus[(int)role];
        public Carrier carrier;
        public Transform model;
        public Checkout checkout;
        public DriveThru driveThru;     // DriveThru rolü için
        public Vector3 idlePoint;

        Job job;
        Station target;
        Vector3 targetPos;
        float waitTimer, bob;
        TrashBin trash;
        NavMover mover;
        Station intent;   // topladığı ürünü asıl götürmek istediği yer (raf ya da makine)

        public static readonly System.Collections.Generic.List<Worker> All = new System.Collections.Generic.List<Worker>();
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Start()
        {
            if (idlePoint == Vector3.zero) idlePoint = transform.position;
            trash = FindAnyObjectByType<TrashBin>();
        }

        // ---------------- işe giriş: dışarıdan gelip kapıdan girer ----------------
        Vector3[] arrival;
        int arrivalIdx;
        public bool Arriving => arrival != null;

        /// <summary>Yeni işe alınınca: start noktasında belirir, path boyunca yürür, sonra işine başlar.</summary>
        public void BeginArrival(Vector3 start, Vector3[] path)
        {
            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.Warp(start);
            else transform.position = start;
            if (path != null && path.Length > 0)
            {
                var d = path[0] - start; d.y = 0f;
                if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
            }
            arrival = path;
            arrivalIdx = 0;
        }

        void Update()
        {
            if (arrival != null)
            {
                if (MoveTo(arrival[arrivalIdx]))
                {
                    arrivalIdx++;
                    if (arrivalIdx >= arrival.Length) { arrival = null; Fx.Confetti(transform.position, false); }
                }
                return;
            }
            if (role == Role.Cashier)
            {
                var spot = checkout.transform.position + checkout.zoneOffset;
                if (MoveTo(spot)) Face(checkout.transform.position);
                return;
            }
            speed = baseSpeed + SpeedBonus;
            if (role == Role.DriveThru) { DriveThruUpdate(); return; }
            if (role == Role.Stocker) { StockerUpdate(); return; }
            carrier.stack.capacity = baseCapacity + CapacityBonus;
            FarmhandUpdate();
        }

        // ---------------- Bakıcı ----------------

        void FarmhandUpdate()
        {
            if (target == null || !target.isActiveAndEnabled) Think();
            if (target == null) { MoveTo(idlePoint); return; }

            if (!MoveTo(targetPos)) return;

            waitTimer += Time.deltaTime;
            switch (job)
            {
                case Job.Collect:
                    if (carrier.stack.IsFull || !HasStock(target) || waitTimer > 6f) Clear();
                    break;
                case Job.Deliver:
                    if (!Accepts(target) || waitTimer > 4f) Clear();
                    break;
                case Job.Dump:
                    if (carrier.stack.Count == 0 || waitTimer > 4f) { carrier.dumpRequest = false; Clear(); }
                    break;
            }
        }

        void Clear() { target = null; job = Job.None; if (carrier) { carrier.collectTarget = null; carrier.wantType = -1; } }

        void Think()
        {
            Clear();
            var st = carrier.stack;

            if (st.Count > 0)
            {
                // Önce toplarken hedeflediği yere götür
                if (intent != null && intent.isActiveAndEnabled && Accepts(intent)) { var it = intent; intent = null; Go(it, Job.Deliver); return; }
                intent = null;
                // Elindekini alabilecek en yakın yer (raf ya da hayvan)
                Station best = null; float bestD = float.MaxValue;
                foreach (var s in Shelf.All)
                    if (!s.container.IsFull && st.CountOf(s.type) > 0) Consider(s, ref best, ref bestD);
                foreach (var c in Converter.All)
                    if (CarriesNeeded(c, st)) Consider(c, ref best, ref bestD);
                if (best == null)
                {
                    // Şu an yer yok: aynı türü kullanan en yakın yere gidip beklesin
                    foreach (var s in Shelf.All) if (st.CountOf(s.type) > 0) Consider(s, ref best, ref bestD);
                    foreach (var c in Converter.All) if (st.CountOf(c.input) > 0 || (c.useInput2 && st.CountOf(c.input2) > 0)) Consider(c, ref best, ref bestD);
                }
                if (best != null) { Go(best, Job.Deliver); return; }
                // Hiçbir yerde kullanılmıyor: çöpe
                if (trash != null) { carrier.dumpRequest = true; Go(trash, Job.Dump); }
                return;
            }

            // Eli boş: rafları ve hayvanları değerlendir
            Shelf needShelf = null; Station shelfSource = null; float shelfRatio = 2f;
            foreach (var s in Shelf.All)
            {
                if (s.container.IsFull || Wholesale.IsWholesale(s.type)) continue;   // bakkal raflarını depocu doldurur
                var src = FindSource(s.type);
                if (src == null) continue;
                float r = (float)s.container.Count / s.container.capacity;
                if (r < shelfRatio) { shelfRatio = r; needShelf = s; shelfSource = src; }
            }

            Station feedSource = null; Converter feedTarget = null; float feedRatio = 2f;
            foreach (var c in Converter.All)
            {
                // Birinci ve (varsa) ikinci girdi ayrı ayrı değerlendirilir (fırın: un + tereyağı)
                for (int slot = 0; slot < (c.useInput2 ? 2 : 1); slot++)
                {
                    var pile = slot == 0 ? c.inputPile : c.inputPile2;
                    var type = slot == 0 ? c.input : c.input2;
                    if (pile == null || pile.Count >= c.inputCapacity) continue;
                    var f = FindSource(type); // tarla ya da başka bir istasyonun çıktısı (ör. inek sütü → makine)
                    if (f == null || f == c) continue;
                    float r = (float)pile.Count / c.inputCapacity;
                    if (r < feedRatio) { feedRatio = r; feedSource = f; feedTarget = c; }
                }
            }

            if (needShelf != null && (shelfRatio < 0.5f || feedSource == null)) { intent = needShelf; Go(shelfSource, Job.Collect); return; }
            if (feedSource != null) { intent = feedTarget; Go(feedSource, Job.Collect); return; }
            if (needShelf != null) { intent = needShelf; Go(shelfSource, Job.Collect); }
        }

        static bool CarriesNeeded(Converter c, ItemContainer st)
        {
            if (st.CountOf(c.input) > 0 && c.Needs(c.input)) return true;
            if (c.useInput2 && st.CountOf(c.input2) > 0 && c.Needs(c.input2)) return true;
            return false;
        }

        void Consider(Station s, ref Station best, ref float bestD)
        {
            float d = (s.transform.position + s.zoneOffset - transform.position).sqrMagnitude;
            if (d < bestD) { bestD = d; best = s; }
        }

        void Go(Station s, Job j)
        {
            target = s;
            job = j;
            carrier.collectTarget = j == Job.Collect ? s : null;
            if (j == Job.Collect && s is DeliveryDock && role == Role.DriveThru) carrier.wantType = (int)collectType;   // depodan sadece siparişteki ürün
            waitTimer = 0f;
            var c = s.transform.position + s.zoneOffset;
            float jx = Mathf.Max(0f, s.zoneSize.x * 0.5f - 0.35f);
            float jz = Mathf.Max(0f, s.zoneSize.y * 0.5f - 0.35f);
            targetPos = new Vector3(c.x + Random.Range(-jx, jx), 0f, c.z + Random.Range(-jz, jz));
        }

        /// <summary>Bu ürün şu an nereden toplanabilir? (hayvan/makine tepsisi veya tarla)</summary>
        static Station FindSource(ItemType t)
        {
            // toptancı ürünleri: tek kaynak depo
            if (Wholesale.IsWholesale(t))
            {
                var d = DeliveryDock.I;
                return d != null && d.isActiveAndEnabled && d.CountOf(t) > 0 ? d : null;
            }
            Station best = null; int bestCount = 0;
            foreach (var c in Converter.All)
                if (c.output == t && c.outputTray.Count > bestCount) { best = c; bestCount = c.outputTray.Count; }
            foreach (var f in ResourceField.All)
                if (f.produces == t && f.GrownCount > bestCount) { best = f; bestCount = f.GrownCount; }
            return best;
        }

        static ResourceField FindField(ItemType t)
        {
            ResourceField best = null; int bestCount = 0;
            foreach (var f in ResourceField.All)
                if (f.produces == t && f.GrownCount > bestCount) { best = f; bestCount = f.GrownCount; }
            return best;
        }

        static bool HasStock(Station s)
        {
            if (s is Converter c) return c.outputTray.Count > 0;
            if (s is ResourceField f) return f.GrownCount > 0;
            if (s is DeliveryDock d) return d.Count > 0;
            return false;
        }

        bool Accepts(Station s)
        {
            var st = carrier.stack;
            if (s is Shelf sh) return !sh.container.IsFull && st.CountOf(sh.type) > 0;
            if (s is Converter c) return st.CountOf(c.input) > 0 || (c.useInput2 && st.CountOf(c.input2) > 0);
            return false;
        }

        // ---------------- Drive-thru görevlisi ----------------
        // Camdaki arabanın siparişinin TAMAMINI tarladan / hayvan-makine tepsisinden toplar (raflara dokunmaz),
        // hepsi elindeyken cama götürür. Taşırken makineler/raflar elindekini almaz (holdItems).

        ItemType collectType;
        int collectGoal;
        Converter feedTarget;   // ürün yoksa makinenin girdisini kendisi taşır (ör. yayığa süt)
        public int driveThruCapacity = 10;

        void DriveThruUpdate()
        {
            var car = driveThru != null ? driveThru.FrontAtWindow : null;
            var st = carrier.stack;
            st.capacity = Mathf.Max(driveThruCapacity, baseCapacity + CapacityBonus);
            if (target == null || !target.isActiveAndEnabled) DtThink(car);
            // Rafa fazlalık bırakırken bile siparişe lazım olanı rafa kaptırma
            // Bırakma yalnızca hedefin alanındayken açılır (yolda başka makine/raf kapmasın)
            if (job == Job.Return && target is Shelf rs) carrier.holdItems = !InZone(rs) || st.CountOf(rs.type) <= NeedCount(car, rs.type);
            else if (job == Job.Return && target is Converter) carrier.holdItems = !InZone(target);
            else carrier.holdItems = job != Job.Dump && target != null;
            if (target == null) { MoveTo(idlePoint); return; }

            // Sipariş değişti/araba gitti: elde fazlalık varsa düşünmeyi yenile
            if ((job == Job.Collect || job == Job.Deliver) && car == null) { Clear(); return; }

            if (job == Job.Collect && st.CountOf(collectType) >= collectGoal) { Clear(); return; }
            if (!MoveTo(targetPos)) return;

            waitTimer += Time.deltaTime;
            switch (job)
            {
                case Job.Collect:
                    // ürün yetişmemişse kaynağın başında bekler; uzun sürerse başka kaynağa bakar
                    if (st.IsFull || waitTimer > 8f || (!HasStock(target) && FindSource(collectType) != null && FindSource(collectType) != target)
                        || (target is DeliveryDock dk && dk.CountOf(collectType) == 0)) Clear();
                    break;
                case Job.Deliver:
                    if (car == null || !CarryingNeeded(car) || waitTimer > 3f) Clear();
                    break;
                case Job.Return:
                    if (!Accepts(target) || (carrier.holdItems && InZone(target)) || waitTimer > 3f) Clear();
                    break;
                case Job.Dump:
                    if (st.Count == 0 || waitTimer > 4f) { carrier.dumpRequest = false; Clear(); }
                    break;
            }
        }

        void DtThink(DriveThruCar car)
        {
            Clear();
            var st = carrier.stack;
            if (car == null) { feedTarget = null; if (st.Count > 0) ReturnLeftovers(); return; }

            // Makineye girdi taşıyorsa önce onu bırak
            if (feedTarget != null)
            {
                if (feedTarget.isActiveAndEnabled && (st.CountOf(feedTarget.input) > 0 || (feedTarget.useInput2 && st.CountOf(feedTarget.input2) > 0)))
                { Go(feedTarget, Job.Return); return; }
                feedTarget = null;
            }

            // Siparişte olmayan / fazla ürünleri önce rafa bırak
            foreach (var it in st.items)
                if (st.CountOf(it.type) > NeedCount(car, it.type)) { ReturnLeftovers(car); if (target != null) return; break; }

            // Her şey elde: teslim et
            if (!CarryingLess(car) || st.IsFull) { if (CarryingNeeded(car)) Go(driveThru, Job.Deliver); return; }

            // Eksik ürün: önce stoğu olan kaynak, yoksa üreticisinin başında bekle
            Station wait = null; ItemType waitType = default(ItemType);
            foreach (var line in car.order)
            {
                if (NeedCount(car, line.type) <= st.CountOf(line.type)) continue;
                var src = FindSource(line.type);
                if (src != null) { collectType = line.type; collectGoal = NeedCount(car, line.type); Go(src, Job.Collect); return; }
                if (wait == null) { wait = FindProducer(line.type); waitType = line.type; }
            }
            if (wait != null)
            {
                // Boşta duran (girdisi olmayan) makine varsa girdisini kaynağından toplayıp makineye kendisi götürsün
                foreach (var line in car.order)
                {
                    int missing = NeedCount(car, line.type) - st.CountOf(line.type);
                    if (missing <= 0) continue;
                    if (FindProducer(line.type) is Converter cv && cv.outputTray.Count == 0 && !cv.IsWorking && TryFeed(cv, missing)) return;
                }
                collectType = waitType; collectGoal = NeedCount(car, waitType); Go(wait, Job.Collect); return;
            }
            // Üreticisi olmayan ürün (olmamalı): eldekini teslim et
            if (CarryingNeeded(car)) Go(driveThru, Job.Deliver);
        }

        bool InZone(Station s) => s.InZone(transform.position);

        bool TryFeed(Converter cv, int missing)
        {
            var st = carrier.stack;
            int free = st.capacity - st.Count;
            if (free <= 0 || missing <= 0) return false;
            int per = Mathf.Max(1, cv.inputPerOutput);
            for (int slot = 0; slot < (cv.useInput2 ? 2 : 1); slot++)
            {
                var type = slot == 0 ? cv.input : cv.input2;
                var pile = slot == 0 ? cv.inputPile : cv.inputPile2;
                int needPer = slot == 0 ? per : 1;
                if (pile == null || pile.Count >= needPer) continue;
                var src = FindSource(type);
                if (src == null || src == cv) continue;
                int want = Mathf.Min(needPer * missing, Mathf.Min(free, cv.inputCapacity - pile.Count));
                if (want <= 0) continue;
                feedTarget = cv;
                collectType = type;
                collectGoal = st.CountOf(type) + want;
                Go(src, Job.Collect);
                return true;
            }
            return false;
        }

        Station FindProducer(ItemType t)
        {
            Station best = null; float bd = float.MaxValue;
            foreach (var c in Converter.All) if (c.output == t) Consider(c, ref best, ref bd);
            foreach (var f in ResourceField.All) if (f.produces == t) Consider(f, ref best, ref bd);
            return best;
        }

        void ReturnLeftovers(DriveThruCar car = null)
        {
            var st = carrier.stack;
            foreach (var s in Shelf.All)
                if (!s.container.IsFull && st.CountOf(s.type) > NeedCount(car, s.type)) { Go(s, Job.Return); return; }
            if (car != null) return; // sipariş sürüyor: fazlalık rafa sığmıyorsa elde kalsın
            if (trash != null) { carrier.dumpRequest = true; Go(trash, Job.Dump); }
        }

        static int NeedCount(DriveThruCar car, ItemType t)
        {
            if (car == null) return 0;
            int n = 0;
            foreach (var l in car.order) if (l.type == t) n += l.need - l.got;
            return n;
        }

        bool CarryingNeeded(DriveThruCar car)
        {
            foreach (var it in carrier.stack.items) if (NeedCount(car, it.type) > 0) return true;
            return false;
        }

        /// <summary>Siparişte hâlâ elde olmayan ürün var mı?</summary>
        bool CarryingLess(DriveThruCar car)
        {
            if (car == null) return false;
            foreach (var l in car.order)
                if (l.need - l.got > carrier.stack.CountOf(l.type)) return true;
            return false;
        }

        // ---------------- Depocu ----------------
        // Toptancının teslim alanından en çok azalan bakkal rafının ürününü alır, rafa dizer.

        int stockGoal;

        void StockerUpdate()
        {
            var st = carrier.stack;
            st.capacity = baseCapacity + CapacityBonus;
            if (target == null || !target.isActiveAndEnabled) StThink();
            if (target == null) { MoveTo(idlePoint); return; }
            if (!MoveTo(targetPos)) return;
            waitTimer += Time.deltaTime;
            if (job == Job.Collect)
            {
                var dock = DeliveryDock.I;
                if (st.Count >= stockGoal || st.IsFull || dock == null || dock.CountOf((ItemType)carrier.wantType) == 0 || waitTimer > 6f) Clear();
            }
            else if (job == Job.Deliver)
            {
                if (!Accepts(target) || waitTimer > 4f) Clear();
            }
        }

        void StThink()
        {
            Clear();
            var st = carrier.stack;
            if (st.Count > 0)
            {
                Station best = null; float bestD = float.MaxValue;
                foreach (var s in Shelf.All)
                    if (!s.container.IsFull && st.CountOf(s.type) > 0) Consider(s, ref best, ref bestD);
                if (best != null) Go(best, Job.Deliver);
                return;   // raflar doluysa elindekiyle bekler (çöpe atmaz: parası ödendi)
            }
            var d = DeliveryDock.I;
            if (d == null || d.Count == 0) return;
            Shelf need = null; float ratio = 2f;
            foreach (var s in Shelf.All)
            {
                if (s.container.IsFull || d.CountOf(s.type) == 0) continue;
                float r = (float)s.container.Count / s.container.capacity;
                if (r < ratio) { ratio = r; need = s; }
            }
            if (need == null) return;
            stockGoal = Mathf.Min(st.capacity, need.container.capacity - need.container.Count, d.CountOf(need.type));
            Go(d, Job.Collect);
            carrier.wantType = (int)need.type;
        }

        // ---------------- Hareket ----------------

        bool MoveTo(Vector3 t)
        {
            if (mover == null) mover = new NavMover(gameObject, speed, 0.3f, role == Role.Cashier ? 20 : 30);
            bool arrived = mover.MoveTo(t, speed);
            if (!arrived) bob += Time.deltaTime * 12f; else bob = 0f;
            if (model) model.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(bob)) * 0.07f, 0f);
            return arrived;
        }

        void Face(Vector3 t)
        {
            var d = t - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 1f - Mathf.Exp(-10f * Time.deltaTime));
        }
    }
}

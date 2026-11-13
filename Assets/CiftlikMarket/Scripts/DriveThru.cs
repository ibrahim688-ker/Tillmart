using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>
    /// Drive-thru camı: arabaları sıraya sokar, öndeki arabanın siparişini cam alanındaki taşıyıcıdan alır,
    /// tamamlanınca ödemeyi (fiyat × 1,2 + bahşiş) para yığınına bırakır.
    /// </summary>
    public class DriveThru : Station
    {
        public GameObject carPrefab;
        public GameObject motoPrefab;   // Bölge 2: 1–2 ürün, sabır 25 sn, yüksek bahşiş
        public GameObject vanPrefab;    // Bölge 2: 8–10 ürün, sabır 70 sn, ×1,5
        public Transform spawnPoint, exitPoint, payPoint;
        public Transform[] queuePoints;
        public Transform[] exitRoute;   // pencereden sonra izlenen yol (kavisli sapak); boşsa doğrudan exitPoint
        public MoneyPile moneyPile;
        public WorldLabel orderLabel;
        [Tooltip("Sipariş balonunun öndeki arabaya göre konumu (arabanın tam üstü)")]
        public Vector3 bubbleOffset = new Vector3(1.5f, 2.1f, -0.9f);

        [Header("Denge")]
        public float interval = 40f;
        [Tooltip("Bu şeridin aralık çarpanı (ikinci şerit biraz daha seyrek)")]
        public float intervalScale = 1f;
        public float firstDelay = 4f;
        public int maxCars = 3;
        public int minItems = 2;
        public int maxItems = 4;
        public int maxTypes = 3;
        public float patience = 45f;
        public float priceMultiplier = 1.2f;

        readonly List<DriveThruCar> cars = new List<DriveThruCar>();
        System.Action<Carrier> onCarrier;   // bir kez oluşturulur
        DriveThruCar front;                 // delege yerel değişken yakalamasın diye alan
        // Yol noktalarının dünya konumları bir kez alınır: açılış animasyonu (PopAnim) DriveThru'yu ölçeklerken noktalar kaymasın
        Vector3 spawnPos, exitPos, payPos;
        Vector3[] queuePos, exitPath;

        void Awake()
        {
            spawnPos = spawnPoint.position;
            exitPos = exitPoint.position;
            payPos = payPoint.position;
            queuePos = new Vector3[queuePoints.Length];
            for (int i = 0; i < queuePoints.Length; i++) queuePos[i] = queuePoints[i].position;
            int n = exitRoute != null ? exitRoute.Length : 0;
            exitPath = new Vector3[n + 1];
            for (int i = 0; i < n; i++) exitPath[i] = exitRoute[i].position;
            exitPath[n] = exitPos;
        }
        readonly StringBuilder sb = new StringBuilder();
        float timer;
        int labelKey;

        void OnEnable() { timer = interval - firstDelay; }

        void Update()
        {
            // Sırada araba varken yenisi daha yavaş gelir, iki araba bekliyorsa hiç gelmez:
            // oyuncu arabalara yetişemezken markettekiler de beklemesin (yoğun saatte de en fazla 2).
            float pace = cars.Count == 0 ? 1f : cars.Count == 1 ? 0.5f : 0f;
            if (RushHour.Active) pace = cars.Count == 0 ? 1f : cars.Count == 1 ? 0.8f : 0f;
            timer += Time.deltaTime * pace;
            float baseIv = interval * intervalScale;
            float iv = RushHour.Active ? baseIv * 0.4f : baseIv;
            if (timer >= iv && cars.Count < maxCars && cars.Count < queuePoints.Length && Shelf.All.Count > 0 && DriveThruCar.LaneClear(spawnPos, 4.5f))
            {
                timer = 0f;
                SpawnCar();
            }

            for (int i = 0; i < cars.Count; i++)
                cars[i].SetSlot(queuePos[i], i == 0);

            front = cars.Count > 0 ? cars[0] : null;
            if (front != null && front.AtWindow)
            {
                ForEachCarrier(onCarrier ??= c =>
                {
                    if (!c.ReadyToTransfer()) return;
                    foreach (var line in front.order)
                    {
                        if (line.got >= line.need) continue;
                        var it = c.stack.TakeLast(line.type);
                        if (it == null) continue;
                        front.Receive(it);
                        if (c.isPlayer) AudioFx.Play(Sfx.Drop);
                        c.ResetTransfer();
                        return;
                    }
                });

                if (front.Complete)
                {
                    int pay = front.Payment();
                    moneyPile.Add(pay, payPos);
                    Quests.Report(QuestType.DriveThruOrders);
                    if (front.TipRate >= 0.3f) Quests.Report(QuestType.HappyCars);   // sabır yeşildeyken bitti
                    Quests.Report(QuestType.EarnMoney, pay);
                    AudioFx.Play(Sfx.Cash);
                    front.Leave(exitPath);
                    cars.RemoveAt(0);
                }
            }

            UpdateLabel(front);
        }

        /// <summary>Camda siparişi bekleyen araba (görevli için).</summary>
        public DriveThruCar FrontAtWindow => cars.Count > 0 && cars[0].AtWindow ? cars[0] : null;

        /// <summary>Sırada bekleyen araç sayısı (ekran kenarı göstergesi için).</summary>
        public int CarCount => cars.Count;

        /// <summary>Camın dünya konumu.</summary>
        public Vector3 WindowPos => queuePos != null && queuePos.Length > 0 ? queuePos[0] : transform.position;

        /// <summary>Yoğun saat başlayınca ilk araba hemen gelsin.</summary>
        public void OnRushStart() { timer = Mathf.Max(timer, interval); }

        void SpawnCar()
        {
            // Araç türü (motosiklet ve minibüs Mandıra bölgesiyle gelir)
            bool region2 = GameManager.I != null && GameManager.I.IsUnlocked("genisleme_2");
            float roll = Random.value;
            var prefab = carPrefab;
            int minI = minItems, maxI = maxItems, maxT = maxTypes;
            float pat = patience, mult = priceMultiplier, tipMul = 1f;
            string kind = "SİPARİŞ";
            if (region2 && motoPrefab != null && roll < 0.2f) { prefab = motoPrefab; minI = 1; maxI = 2; maxT = 2; pat = 25f; tipMul = 1.6f; kind = "MOTOSİKLET"; }
            else if (region2 && vanPrefab != null && roll < 0.35f) { prefab = vanPrefab; minI = 8; maxI = 10; maxT = 3; pat = 70f; mult = 1.5f; kind = "MİNİBÜS"; }

            var types = new List<ItemType>();
            var depot = DeliveryDock.I;
            foreach (var s in Shelf.All)
            {
                if (types.Contains(s.type)) continue;
                // toptancı ürünü: görevli depodan alır; depoda yeterince yoksa isteme
                if (Wholesale.IsWholesale(s.type) && (depot == null || !depot.isActiveAndEnabled || depot.CountOf(s.type) < 3)) continue;
                types.Add(s.type);
            }
            for (int i = types.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (types[i], types[j]) = (types[j], types[i]);
            }
            int typeCount = Random.Range(1, Mathf.Min(maxT, types.Count) + 1);
            int total = Mathf.Max(typeCount, Random.Range(minI, maxI + 1));

            var lines = new List<DriveThruCar.Line>();
            for (int i = 0; i < typeCount; i++) lines.Add(new DriveThruCar.Line { type = types[i], need = 1 });
            for (int k = typeCount; k < total; k++) lines[Random.Range(0, typeCount)].need++;
            foreach (var l in lines)
                if (Wholesale.IsWholesale(l.type) && depot != null) l.need = Mathf.Clamp(l.need, 1, depot.CountOf(l.type));

            var face = (queuePos.Length > 0 ? queuePos[0] : exitPos) - spawnPos; face.y = 0f;   // şeridin yönüne baksın (üst şerit güneye, alt şerit kuzeye)
            var go = Instantiate(prefab, spawnPos, Quaternion.LookRotation(face.sqrMagnitude > 0.01f ? face : Vector3.back));
            var car = go.GetComponent<DriveThruCar>();
            car.kindName = kind;
            car.priceMultiplier = mult;
            car.tipMultiplier = tipMul;
            car.Init(lines, pat, queuePos[Mathf.Min(cars.Count, queuePos.Length - 1)]);
            cars.Add(car);
        }

        void UpdateLabel(DriveThruCar front)
        {
            if (!orderLabel) return;
            if (front == null || !front.AtWindow) { orderLabel.Set(""); labelKey = 0; return; }
            // düşünce balonu arabanın tam üstünde (telefonda sol kenar görünmüyordu)
            orderLabel.transform.position = front.transform.position + bubbleOffset;
            orderLabel.tailDir = new Vector2(-0.6f, -1f).normalized;
            int key = front.GetHashCode();
            foreach (var l in front.order) key = key * 31 + l.got * 7 + l.need;
            orderLabel.SetColor(front.LabelColor);
            if (key == labelKey) return;
            labelKey = key;
            sb.Clear();
            // araç türü uzaktan belli; balonda sadece sipariş satırları
            foreach (var l in front.order)
                sb.Append(sb.Length > 0 ? "\n" : "").Append(GameConfig.I.Name(l.type)).Append(' ').Append(l.got).Append('/').Append(l.need);
            orderLabel.Set(sb.ToString());
            orderLabel.SetColor(front.LabelColor);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>Kasa: kasada biri (oyuncu ya da kasiyer) dururken sıradaki müşteriden ödeme alır.</summary>
    public class Checkout : Station
    {
        public Transform[] queuePoints;
        public MoneyPile moneyPile;
        public Transform counterTop;
        public float payTime = 0.5f;
        /// <summary>Kasiyer "ödeme hızı" yükseltmesi: her seviye ödeme süresini kısaltır.</summary>
        public static int PaySpeedLevel;
        public static float PayMul(int lv) => Mathf.Max(0.4f, 1f - 0.2f * lv);
        public GameObject mannedIndicator;

        public static readonly List<Checkout> All = new List<Checkout>();
        readonly List<Customer> queue = new List<Customer>();

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        /// <summary>
        /// Müşterinin gideceği kasa: başında biri (kasiyer/oyuncu) olan kasalardan sırası en kısa olan.
        /// Hiçbirinde kimse yoksa sırası en kısa olan. Tek kasa varsa o.
        /// </summary>
        public static Checkout Best(Checkout fallback)
        {
            Checkout best = null; int bq = int.MaxValue; bool bm = false;
            foreach (var c in All)
            {
                if (!c.isActiveAndEnabled) continue;
                bool m = c.Manned() || c.HasCashier();
                int q = c.queue.Count;
                if (best == null || (m && !bm) || (m == bm && q < bq)) { best = c; bq = q; bm = m; }
            }
            return best != null ? best : fallback;
        }

        /// <summary>Bu kasaya atanmış kasiyer var mı (şu an kasada olmasa bile)?</summary>
        public bool HasCashier()
        {
            foreach (var w in Worker.All) if (w.role == Worker.Role.Cashier && w.checkout == this && w.isActiveAndEnabled) return true;
            return false;
        }
        float timer;

        public void Join(Customer c) { if (!queue.Contains(c)) queue.Add(c); }
        public int IndexOf(Customer c) => queue.IndexOf(c);
        public int QueueCount => queue.Count;

        public Vector3 QueuePos(int i)
        {
            i = Mathf.Clamp(i, 0, queuePoints.Length - 1);
            return queuePoints[i].position;
        }

        public bool Manned()
        {
            foreach (var c in Carrier.All)
                if (c.canCashier && InZone(c.transform.position)) return true;
            return false;
        }

        void Update()
        {
            bool manned = Manned();
            if (mannedIndicator) mannedIndicator.SetActive(manned);

            if (queue.Count > 0 && manned && queue[0].ReadyToPay)
            {
                timer += Time.deltaTime;
                if (timer >= payTime * PayMul(PaySpeedLevel))
                {
                    timer = 0f;
                    var cu = queue[0];
                    queue.RemoveAt(0);
                    int value = cu.Pay(counterTop.position);
                    moneyPile.Add(value, counterTop.position);
                    Quests.Report(QuestType.ServeCustomers);
                    if (RushHour.Active) Quests.Report(QuestType.RushSales);
                    foreach (var pc in Carrier.All)
                        if (pc.isPlayer && InZone(pc.transform.position)) { Quests.Report(QuestType.CashierSelf); break; }
                    Quests.Report(QuestType.EarnMoney, value);
                    AudioFx.Play(Sfx.Cash, 0.6f);
                }
            }
            else timer = 0f;
        }
    }
}

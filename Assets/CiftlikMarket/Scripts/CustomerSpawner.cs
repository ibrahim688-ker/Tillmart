using System.Collections.Generic;
using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>Belirli aralıklarla yaya müşteri gönderir. İstek listesi açık raflardan seçilir.</summary>
    public class CustomerSpawner : MonoBehaviour
    {
        public GameObject customerPrefab;
        public Transform spawnPoint, approachPoint, entryPoint, exitDoor, exitPoint;
        public Checkout checkout;
        public float interval = 8f;
        public int maxCustomers = 4;
        public int maxPerType = 2;

        float timer;

        void Start() { timer = interval - 2f; }

        void Update()
        {
            if (Tutorial.HoldCustomers) return;   // Hasan Amca ortamı tanıtırken müşteri gelmez
            timer += Time.deltaTime;
            float iv = RushHour.Active ? interval * 0.5f : interval;
            if (timer < iv) return;
            int cap = Mathf.Min(10, Mathf.Max(maxCustomers, 2 + Shelf.All.Count)) + (RushHour.Active ? 2 : 0); // raf arttıkça daha çok müşteri; telefonda performans için en fazla 10
            if (Shelf.All.Count == 0 || Customer.Alive >= cap) return;
            timer = 0f;
            Spawn();
        }

        void Spawn()
        {
            // Aynı üründen birden çok raf olabilir: türe göre tekilleştir (hangi raf olduğu müşteri içeride seçer)
            var pool = new List<Shelf>();
            foreach (var s in Shelf.All)
            {
                bool dup = false;
                foreach (var q in pool) if (q.type == s.type) { dup = true; break; }
                if (!dup) pool.Add(s);
            }
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            int types = Random.Range(1, Mathf.Min(2, pool.Count) + 1);
            var shelves = new List<Shelf>();
            var counts = new List<int>();
            for (int i = 0; i < types; i++)
            {
                shelves.Add(pool[i]);
                counts.Add(Random.Range(1, maxPerType + 1));
            }
            var go = Instantiate(customerPrefab, spawnPoint.position, Quaternion.LookRotation(Vector3.forward));
            go.GetComponent<Customer>().Init(checkout, shelves, counts, approachPoint.position, entryPoint.position, exitDoor.position, exitPoint.position);
        }
    }
}

using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>Yaya müşteri: girer, raflardan ister listesini toplar, kasada öder, çıkar.</summary>
    public class Customer : MonoBehaviour
    {
        public static int Alive;

        public float speed = 2.6f;
        public ItemContainer carry;
        public WorldLabel bubble;
        public Renderer body;
        [Tooltip("Kol yenleri (0. materyal gömlek) — gövdeyle aynı renge boyanır")]
        public Renderer[] sleeves;
        public Renderer hair;
        public Renderer[] pants;

        static readonly Color[] HairColors = { new Color(0.36f,0.25f,0.22f), new Color(0.13f,0.11f,0.1f), new Color(0.85f,0.65f,0.3f), new Color(0.6f,0.3f,0.15f), new Color(0.75f,0.75f,0.75f) };
        static readonly Color[] PantsColors = { new Color(0.18f,0.23f,0.35f), new Color(0.25f,0.25f,0.27f), new Color(0.42f,0.33f,0.25f), new Color(0.2f,0.33f,0.25f) };

        class Want { public Shelf shelf; public int need; public int got; }
        enum State { Approaching, Entering, ToShelf, AtShelf, ToQueue, Leaving }

        readonly List<Want> wants = new List<Want>();
        readonly StringBuilder sb = new StringBuilder();
        int wi, leaveLeg;
        State state;
        Checkout checkout;
        Vector3 approach, entry, exitDoor, exitOut;
        float takeTimer;
        NavMover mover;
        Vector3 shelfOffset;
        int bubbleKey = int.MinValue;
        bool iconsOn;

        void OnEnable() { Alive++; }
        void OnDisable() { Alive--; }

        public void Init(Checkout co, List<Shelf> shelves, List<int> counts, Vector3 approachPos, Vector3 entryPos, Vector3 exitDoorPos, Vector3 exitOutPos)
        {
            checkout = co;
            approach = approachPos;
            entry = entryPos;
            exitDoor = exitDoorPos;
            exitOut = exitOutPos;
            for (int i = 0; i < shelves.Count; i++) wants.Add(new Want { shelf = shelves[i], need = counts[i] });
            state = State.Approaching;
            shelfOffset = new Vector3(Random.Range(-0.6f, 0.6f), 0f, Random.Range(-0.15f, 0.15f));
            var shirt = Random.ColorHSV(0f, 1f, 0.45f, 0.75f, 0.85f, 1f);
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_BaseColor", shirt);
            if (body) body.SetPropertyBlock(mpb);
            if (sleeves != null) foreach (var r in sleeves) if (r) r.SetPropertyBlock(mpb, 0);
            if (hair)
            {
                var hb = new MaterialPropertyBlock();
                hb.SetColor("_BaseColor", HairColors[Random.Range(0, HairColors.Length)]);
                hair.SetPropertyBlock(hb);
            }
            if (pants != null)
            {
                var pb = new MaterialPropertyBlock();
                pb.SetColor("_BaseColor", PantsColors[Random.Range(0, PantsColors.Length)]);
                foreach (var r in pants) if (r) r.SetPropertyBlock(pb, 0);
            }
        }

        // Sıranın başındaki müşteri tam noktada olmasa da (biri itmiş olabilir) yakınsa ödeme yapabilir
        public bool ReadyToPay => state == State.ToQueue && checkout.IndexOf(this) == 0 && Near(checkout.QueuePos(0), 0.45f);

        void Update()
        {
            switch (state)
            {
                case State.Approaching:
                    if (MoveTo(approach)) state = State.Entering;
                    break;
                case State.Entering:
                    if (MoveTo(entry)) { state = State.ToShelf; PickShelf(wants[wi]); }
                    break;
                case State.ToShelf:
                    if (MoveTo(wants[wi].shelf.customerPoint.position + shelfOffset)) state = State.AtShelf;
                    break;
                case State.AtShelf:
                {
                    var w = wants[wi];
                    Face(w.shelf.transform.position);
                    takeTimer -= Time.deltaTime;
                    if (takeTimer <= 0f && w.got < w.need)
                    {
                        var it = w.shelf.Take();
                        if (it != null) { carry.Add(it); w.got++; takeTimer = 0.25f; }
                    }
                    if (w.got >= w.need) NextWant();
                    else if (w.shelf.container.Count == 0 && takeTimer < -1.5f && PickShelf(w)) state = State.ToShelf; // aynı üründen dolu başka raf varsa ona geç
                    break;
                }
                case State.ToQueue:
                    if (MoveTo(checkout.QueuePos(checkout.IndexOf(this)))) Face(checkout.transform.position);
                    break;
                case State.Leaving:
                    if (MoveTo(leaveLeg == 0 ? exitDoor : exitOut))
                    {
                        leaveLeg++;
                        if (leaveLeg > 1) Destroy(gameObject);
                    }
                    break;
            }
            UpdateBubble();
        }

        void NextWant()
        {
            wi++;
            if (wi >= wants.Count) { state = State.ToQueue; checkout = Checkout.Best(checkout); checkout.Join(this); }
            else { state = State.ToShelf; PickShelf(wants[wi]); }
        }

        /// <summary>Aynı üründen birden çok raf varsa en dolu (eşitse en yakın) olanı seç. Değiştiyse true.</summary>
        bool PickShelf(Want w)
        {
            Shelf best = w.shelf; int bc = w.shelf != null && w.shelf.isActiveAndEnabled ? w.shelf.container.Count : -1;
            float bd = best != null ? (best.transform.position - transform.position).sqrMagnitude : float.MaxValue;
            foreach (var s in Shelf.All)
            {
                if (s == w.shelf || s.type != w.shelf.type) continue;
                int c = s.container.Count;
                float d = (s.transform.position - transform.position).sqrMagnitude;
                if (c > bc || (c == bc && d < bd)) { best = s; bc = c; bd = d; }
            }
            if (best == w.shelf) return false;
            w.shelf = best;
            takeTimer = 0f;
            return true;
        }

        static GameObject bagPrefab;
        static bool bagLoaded;

        /// <summary>Ödemeden sonra eline beyaz market poşeti (çok ürün aldıysa iki elde birer tane).</summary>
        void GiveBags(int itemCount)
        {
            if (itemCount <= 0) return;
            if (!bagLoaded) { bagPrefab = Resources.Load<GameObject>("Poset"); bagLoaded = true; }
            if (bagPrefab == null) return;
            var anim = GetComponentInChildren<CharacterAnimator>();
            if (anim == null) return;
            AttachBag(anim.armR);
            if (itemCount >= 5) AttachBag(anim.armL);
        }

        void AttachBag(Transform arm)
        {
            if (arm == null) return;
            var go = Instantiate(bagPrefab, arm);
            go.transform.localPosition = new Vector3(0f, -0.44f, 0f);
            go.transform.localRotation = Quaternion.identity;
            var hb = go.GetComponent<HangingBag>();
            if (hb != null) hb.root = transform;
        }

        public int Pay(Vector3 counterPos)
        {
            int value = 0;
            GiveBags(carry.items.Count);
            foreach (var it in carry.items)
            {
                value += GameConfig.I.Price(it.type);
                Quests.Report(QuestType.SellProduct, it.type);
                var go = it.gameObject;
                it.FlyTo(null, counterPos, 0.25f, () => Destroy(go));
            }
            carry.items.Clear();
            state = State.Leaving;
            leaveLeg = 0;
            return value;
        }

        void UpdateBubble()
        {
            if (!bubble) return;
            if (state == State.Leaving) { bubble.Set(""); return; }
            if (state == State.ToQueue) { bubble.Set(""); return; }   // kasaya gittiği belli: yazıya gerek yok
            // istek listesi değişmediyse yazıyı yeniden kurma
            int key = wi * 7919 + wants.Count;
            for (int i = 0; i < wants.Count; i++) key = key * 31 + wants[i].got * 5 + (int)wants[i].shelf.type;
            if (key == bubbleKey) return;
            bubbleKey = key;
            if (!iconsOn) { bubble.UseIcons(); iconsOn = true; }
            sb.Clear();
            for (int i = 0; i < wants.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                var w = wants[i];
                // ürünün adı yerine resmi (okuması hızlı, dil gerektirmez)
                sb.Append(WorldLabel.Icon(w.shelf.type)).Append(' ').Append(w.got).Append('/').Append(w.need);
            }
            bubble.Set(sb.ToString());
        }

        bool MoveTo(Vector3 target)
        {
            if (mover == null) mover = new NavMover(gameObject, speed, 0.3f, Random.Range(40, 70));
            return mover.MoveTo(target, speed);
        }

        void Face(Vector3 target)
        {
            var d = target - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 1f - Mathf.Exp(-10f * Time.deltaTime));
        }

        bool Near(Vector3 target, float dist = 0.1f)
        {
            var d = target - transform.position;
            d.y = 0f;
            return d.magnitude < dist;
        }
    }
}

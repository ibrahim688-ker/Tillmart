using System.Collections.Generic;
using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>Zemindeki kilit alanı: oyuncu üstünde durdukça para dökülür, tutar dolunca hedefler açılır.</summary>
    public class UnlockZone : Station
    {
        /// <summary>Şu an ofisten satın alınabilen kilitler (ör. market genişletme).</summary>
        public static readonly List<UnlockZone> OfficeItems = new List<UnlockZone>();

        public string id;
        public string title;
        public int cost = 50;
        public GameObject[] unlockTargets;
        public GameObject[] nextZones;
        [Tooltip("Açılınca kaldırılacak nesneler (ör. genişlemede eski duvar)")]
        public GameObject[] removeTargets;
        public WorldLabel label;
        public Transform fill;
        [Tooltip("Zeminde değil, ofis bilgisayarından satın alınır")]
        public bool officeOnly;
        [TextArea] public string description;
        [Tooltip("Bilgisayarda görünmesi için hepsinin açılmış olması gereken kilitler")]
        public string[] requiresUnlocked;
        [Tooltip("Satın alınınca yeni şube açılır (onay penceresiyle)")]
        public bool opensNewBranch;
        [Header("Çalışan (tabletteki ÇALIŞANLAR bölümünde listelenir)")]
        public bool isWorker;
        [Tooltip("Tablet kartındaki ikon (UI/Icons altındaki ad)")]
        public string icon;
        [Tooltip("Şart sağlanmadan önce kartta görünen açıklama (ör. Drive-thru açılınca)")]
        public string lockText;
        public int sortOrder;

        public bool RequirementsMet
        {
            get
            {
                if (requiresUnlocked == null) return true;
                foreach (var r in requiresUnlocked) if (!string.IsNullOrEmpty(r) && !GameManager.I.IsUnlocked(r)) return false;
                return true;
            }
        }

        int paid;
        float stand, acc, billTimer;

        public int Remaining => Mathf.Max(0, cost - paid);

        void OnEnable() { if (officeOnly && !OfficeItems.Contains(this)) OfficeItems.Add(this); }
        void OnDisable() { OfficeItems.Remove(this); }

        /// <summary>Ofisten satın alma: kalan tutarın tamamı tek seferde ödenir.</summary>
        public bool TryPurchase()
        {
            if (!RequirementsMet) return false;
            int r = Remaining;
            if (GameManager.I.Money < r) return false;
            if (opensNewBranch)
            {
                if (BranchManager.I != null) BranchManager.I.AskOpenNewBranch(r, BranchManager.OpenNewBranch);
                return false;
            }
            GameManager.I.Spend(r);
            paid = cost;
            GameManager.I.SetPaid(id, paid);
            Complete(false);
            GameManager.I.Save();
            return true;
        }

        void Start()
        {
            if (GameManager.I.IsUnlocked(id)) { Complete(true); return; }
            paid = GameManager.I.GetPaid(id);
            Refresh();
        }

        void Update()
        {
            if (officeOnly) return;
            var player = PlayerInZone();
            if (player == null) { stand = 0f; acc = 0f; return; }

            stand += Time.deltaTime;
            if (stand < 0.3f || GameManager.I.Money <= 0) return;

            acc += Mathf.Max(cost / 1.2f, 25f) * Time.deltaTime;
            int want = (int)acc;
            if (want <= 0) return;
            acc -= want;

            int give = Mathf.Min(want, Mathf.Min(cost - paid, GameManager.I.Money));
            if (give <= 0) return;
            GameManager.I.Spend(give);
            paid += give;
            GameManager.I.SetPaid(id, paid);

            billTimer -= Time.deltaTime;
            if (billTimer <= 0f)
            {
                billTimer = 0.07f;
                var b = GameConfig.I.Spawn(ItemType.Para, player.transform.position + Vector3.up * 1.2f);
                var go = b.gameObject;
                b.FlyTo(transform, Vector3.zero, 0.25f, () => Destroy(go));
                AudioFx.Play(Sfx.Coin, 0.35f);
            }

            Refresh();
            if (paid >= cost) Complete(false);
        }

        void Refresh()
        {
            if (label) label.Set(title + "\n" + (cost - paid));
            if (fill)
            {
                float p = cost > 0 ? Mathf.Clamp01((float)paid / cost) : 1f;
                p = Mathf.Max(0.001f, p);
                fill.localScale = new Vector3(p, 1f, p);
            }
        }

        void Complete(bool instant)
        {
            GameManager.I.MarkUnlocked(id);
            foreach (var t in unlockTargets)
            {
                if (!t) continue;
                t.SetActive(true);
                if (instant) continue;
                var w = t.GetComponent<Worker>();
                if (w != null) HireIntro.Play(w);       // yeni çalışan kapıdan girer
                else t.AddComponent<PopAnim>();
            }
            if (removeTargets != null) foreach (var r in removeTargets) if (r) r.SetActive(false);
            foreach (var n in nextZones) if (n) n.SetActive(true);
            if (!instant) { AudioFx.Play(Sfx.Unlock); Haptics.Medium(); Quests.Report(QuestType.UnlockArea); if (!isWorker) Fx.Confetti(transform.position, true); }
            gameObject.SetActive(false);
        }
    }
}

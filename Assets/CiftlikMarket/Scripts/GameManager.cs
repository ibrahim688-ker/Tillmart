using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace CiftlikMarket
{
    /// <summary>Para, açılan alanlar ve basit kayıt (PlayerPrefs).</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I;

        public Text moneyText;
        public int Money { get; private set; }

        readonly HashSet<string> unlocked = new HashSet<string>();
        float saveTimer;
        bool dirty;
        int shownMoney = -1;

        void Awake()
        {
            I = this;
            FramePacing.Apply();   // 60 FPS (pil tasarrufu açıksa 30)
            Money = PlayerPrefs.GetInt("cm_money", 0);
            if (FindAnyObjectByType<OfflineEarnings>() == null) new GameObject("OfflineEarnings").AddComponent<OfflineEarnings>();
            if (FindAnyObjectByType<RushHour>() == null) new GameObject("RushHour").AddComponent<RushHour>();
            if (FindAnyObjectByType<BranchManager>() == null) new GameObject("BranchManager").AddComponent<BranchManager>();
            if (FindAnyObjectByType<DailyReward>() == null) new GameObject("DailyReward").AddComponent<DailyReward>();
            if (FindAnyObjectByType<Quests>() == null) new GameObject("Quests").AddComponent<Quests>();
            if (FindAnyObjectByType<Wholesale>() == null) new GameObject("Wholesale").AddComponent<Wholesale>();
            if (FindAnyObjectByType<DepoStory>() == null) new GameObject("DepoStory").AddComponent<DepoStory>();
            if (FindAnyObjectByType<Fx>() == null) new GameObject("Fx").AddComponent<Fx>();
            foreach (var s in PlayerPrefs.GetString(SaveKeys.P("cm_unlocked"), "").Split(','))
                if (s.Length > 0) unlocked.Add(s);
        }

        public void AddMoney(int v) { Money += v; dirty = true; }
        public void Spend(int v) { Money = Mathf.Max(0, Money - v); dirty = true; }

        public bool IsUnlocked(string id) => unlocked.Contains(id);
        public void MarkUnlocked(string id) { if (unlocked.Add(id)) Save(); }

        public int GetPaid(string id) => PlayerPrefs.GetInt(SaveKeys.P("cm_paid_" + id), 0);
        public void SetPaid(string id, int v) { PlayerPrefs.SetInt(SaveKeys.P("cm_paid_" + id), v); dirty = true; }

        void Update()
        {
            if (moneyText && shownMoney != Money)
            {
                shownMoney = Money;
                moneyText.text = Money.ToString();
            }
            saveTimer += Time.deltaTime;
            if ((dirty && saveTimer > 3f) || saveTimer > 6f) Save();
        }

        public void Save()
        {
            saveTimer = 0f;
            dirty = false;
            PlayerPrefs.SetInt("cm_money", Money);
            PlayerPrefs.SetString(SaveKeys.P("cm_unlocked"), string.Join(",", unlocked));
            if (stockLoaded) SaveStock();
            if (OfflineEarnings.I != null) OfflineEarnings.I.MarkTime();
            PlayerPrefs.Save();
        }

        // ---------------- Stok kaydı (raflar, hayvan girdi/çıktıları, sırttaki ürünler, yerdeki para) ----------------

        bool stockLoaded;

        void Start() { StartCoroutine(LoadStockNextFrame()); }

        IEnumerator LoadStockNextFrame()
        {
            // Kilitlerin açılması (UnlockZone.Start) bitsin, sonra stokları yerine koy
            yield return null;
            yield return null;
            LoadStock();
            stockLoaded = true;
        }

        static string Key(Transform t)
        {
            var sb = new StringBuilder(t.name);
            for (var p = t.parent; p != null; p = p.parent) sb.Insert(0, p.name + "/");
            return SaveKeys.P("cm_st_" + sb);
        }

        void SaveStock()
        {
            foreach (var s in Shelf.All) PlayerPrefs.SetInt(Key(s.transform), s.container.Count);
            foreach (var c in Converter.All)
            {
                PlayerPrefs.SetInt(Key(c.transform) + "_in", c.inputPile.Count);
                PlayerPrefs.SetInt(Key(c.transform) + "_out", c.outputTray.Count);
                if (c.useInput2 && c.inputPile2 != null) PlayerPrefs.SetInt(Key(c.transform) + "_in2", c.inputPile2.Count);
            }
            foreach (var c in Carrier.All)
            {
                if (!c.isPlayer) continue;
                var sb = new StringBuilder();
                foreach (var it in c.stack.items) { if (sb.Length > 0) sb.Append(','); sb.Append((int)it.type); }
                PlayerPrefs.SetString(SaveKeys.P("cm_st_player"), sb.ToString());
            }
            var pile = FindAnyObjectByType<MoneyPile>();
            if (pile) PlayerPrefs.SetInt(SaveKeys.P("cm_st_pile"), pile.amount);
            if (DeliveryDock.I != null)
            {
                var db = new StringBuilder();
                foreach (var it in DeliveryDock.I.pile.items) { if (db.Length > 0) db.Append(','); db.Append((int)it.type); }
                PlayerPrefs.SetString(SaveKeys.P("cm_st_dock"), db.ToString());
            }
        }

        void LoadStock()
        {
            var cfg = GameConfig.I;
            var dockStr = PlayerPrefs.GetString(SaveKeys.P("cm_st_dock"), "");
            if (dockStr.Length > 0 && DeliveryDock.I != null)
                foreach (var part in dockStr.Split(','))
                    if (int.TryParse(part, out int dt)) DeliveryDock.I.Receive(cfg.Spawn((ItemType)dt, DeliveryDock.I.pile.transform.position), false);
            foreach (var s in Shelf.All)
            {
                int n = Mathf.Min(PlayerPrefs.GetInt(Key(s.transform), 0), s.container.capacity);
                for (int i = 0; i < n; i++) s.container.Add(cfg.Spawn(s.type, s.container.transform.position), false);
            }
            foreach (var c in Converter.All)
            {
                int nin = Mathf.Min(PlayerPrefs.GetInt(Key(c.transform) + "_in", 0), c.inputPile.capacity);
                for (int i = 0; i < nin; i++) c.inputPile.Add(cfg.Spawn(c.input, c.inputPile.transform.position), false);
                int nout = Mathf.Min(PlayerPrefs.GetInt(Key(c.transform) + "_out", 0), c.outputTray.capacity);
                for (int i = 0; i < nout; i++) c.outputTray.Add(cfg.Spawn(c.output, c.outputTray.transform.position), false);
                if (c.useInput2 && c.inputPile2 != null)
                {
                    int nin2 = Mathf.Min(PlayerPrefs.GetInt(Key(c.transform) + "_in2", 0), c.inputPile2.capacity);
                    for (int i = 0; i < nin2; i++) c.inputPile2.Add(cfg.Spawn(c.input2, c.inputPile2.transform.position), false);
                }
            }
            var stackStr = PlayerPrefs.GetString(SaveKeys.P("cm_st_player"), "");
            if (stackStr.Length > 0)
                foreach (var c in Carrier.All)
                {
                    if (!c.isPlayer) continue;
                    foreach (var part in stackStr.Split(','))
                        if (int.TryParse(part, out int t) && !c.stack.IsFull)
                            c.stack.Add(cfg.Spawn((ItemType)t, c.stack.transform.position), false);
                }
            int pileAmount = PlayerPrefs.GetInt(SaveKeys.P("cm_st_pile"), 0);
            var pile = FindAnyObjectByType<MoneyPile>();
            if (pile && pileAmount > 0) pile.Add(pileAmount, pile.transform.position, false);
        }

        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationQuit() { Save(); }
    }
}

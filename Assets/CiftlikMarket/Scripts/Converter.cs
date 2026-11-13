using System.Collections.Generic;
using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>
    /// Dönüştürücü istasyon: girdi koy → çıktı al (inek: saman → süt, tavuk: mısır → yumurta).
    /// İsteğe bağlı ikinci girdi destekler (fırın: un + tereyağı → poğaça).
    /// Aynı alanda taşıyıcı önce girdiyi bırakır, sonra çıktıyı toplar.
    /// </summary>
    public class Converter : Station
    {
        System.Action<Carrier> onCarrier;   // ForEachCarrier için bir kez oluşturulur (karede çöp ayırmasın)
        public static readonly List<Converter> All = new List<Converter>();
        public ItemType input;
        public ItemType output;
        public int inputCapacity = 4;
        [Tooltip("Bir çıktı için kaç girdi gerekir (peynir: 2 süt, tereyağı: 3 süt)")]
        public int inputPerOutput = 1;
        public float unitTime = 1f;
        public ItemContainer inputPile;
        public ItemContainer outputTray;
        public Transform spawnPoint;
        public WorldLabel label;

        [Header("İkinci girdi (opsiyonel)")]
        public bool useInput2;
        public ItemType input2;
        public ItemContainer inputPile2;

        float timer;
        int shownIn = -1, shownIn2 = -2, shownCap = -1;

        /// <summary>Şu an üretim yapıyor mu? (değirmen çarkı gibi görseller için)</summary>
        public bool IsWorking { get; private set; }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public bool UsesInput(ItemType t) => t == input || (useInput2 && t == input2);

        /// <summary>Bu türden girdi için yer var mı?</summary>
        public bool Needs(ItemType t)
        {
            if (t == input && inputPile.Count < inputCapacity) return true;
            if (useInput2 && t == input2 && inputPile2 != null && inputPile2.Count < inputCapacity) return true;
            return false;
        }

        void Update()
        {
            int per = Mathf.Max(1, inputPerOutput);
            bool ready = inputPile.Count >= per && (!useInput2 || (inputPile2 != null && inputPile2.Count >= 1)) && !outputTray.IsFull;
            IsWorking = ready;

            // Üretim
            if (ready)
            {
                timer += Time.deltaTime;
                if (timer >= unitTime)
                {
                    timer = 0f;
                    for (int k = 0; k < per; k++)
                    {
                        var used = inputPile.TakeLastAny();
                        if (used != null) Destroy(used.gameObject);
                    }
                    if (useInput2)
                    {
                        var used2 = inputPile2.TakeLastAny();
                        if (used2 != null) Destroy(used2.gameObject);
                    }
                    var pos = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up;
                    outputTray.Add(GameConfig.I.Spawn(output, pos));
                }
            }
            else timer = 0f;

            // Taşıyıcılarla alışveriş
            ForEachCarrier(onCarrier ??= c =>
            {
                if (!c.ReadyToTransfer()) return;
                if (inputPile.Count < inputCapacity && !c.holdItems)
                {
                    var it = c.stack.TakeLast(input);
                    if (it != null)
                    {
                        inputPile.Add(it);
                        if (c.isPlayer) { AudioFx.Play(Sfx.Drop); Quests.Report(Quests.IsAnimal(this) ? QuestType.FeedAnimals : QuestType.LoadMachines); Fx.Sparkle(inputPile.transform.position + Vector3.up * 0.5f); }
                        c.ResetTransfer();
                        return;
                    }
                }
                if (useInput2 && inputPile2 != null && inputPile2.Count < inputCapacity && !c.holdItems)
                {
                    var it2 = c.stack.TakeLast(input2);
                    if (it2 != null)
                    {
                        inputPile2.Add(it2);
                        if (c.isPlayer) { AudioFx.Play(Sfx.Drop); Quests.Report(QuestType.LoadMachines); Fx.Sparkle(inputPile2.transform.position + Vector3.up * 0.5f); }
                        c.ResetTransfer();
                        return;
                    }
                }
                if (outputTray.Count > 0 && !c.stack.IsFull && c.MayCollectFrom(this))
                {
                    c.stack.Add(outputTray.TakeLastAny());
                    if (c.isPlayer) { AudioFx.Play(Sfx.Pickup); Quests.Report(QuestType.CollectProduct, output); Fx.Sparkle(outputTray.transform.position + Vector3.up * 0.5f); }
                    c.ResetTransfer();
                }
            });

            if (label)
            {
                int c2 = useInput2 && inputPile2 != null ? inputPile2.Count : -1;
                if (inputPile.Count != shownIn || c2 != shownIn2 || inputCapacity != shownCap)
                {
                    shownIn = inputPile.Count; shownIn2 = c2; shownCap = inputCapacity;
                    string s = GameConfig.I.Name(input) + " " + inputPile.Count + "/" + inputCapacity;
                    if (useInput2 && inputPile2 != null) s += "\n" + GameConfig.I.Name(input2) + " " + inputPile2.Count + "/" + inputCapacity;
                    label.Set(s);
                }
            }
        }
    }
}

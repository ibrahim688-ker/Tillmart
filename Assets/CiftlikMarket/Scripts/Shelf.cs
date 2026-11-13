using System.Collections.Generic;
using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>Market rafı: tek ürün türü tutar. Oyuncu doldurur, müşteri alır.</summary>
    public class Shelf : Station
    {
        System.Action<Carrier> onCarrier;   // ForEachCarrier için bir kez oluşturulur (karede çöp ayırmasın)
        public static readonly List<Shelf> All = new List<Shelf>();

        public ItemType type;
        public ItemContainer container;
        public Transform customerPoint;
        public WorldLabel label;

        int shownCount = -1, shownCap = -1;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Update()
        {
            ForEachCarrier(onCarrier ??= c =>
            {
                if (!c.ReadyToTransfer() || container.IsFull || c.holdItems) return;
                var it = c.stack.TakeLast(type);
                if (it != null)
                {
                    container.Add(it);
                    if (c.isPlayer) { AudioFx.Play(Sfx.Drop); Quests.Report(QuestType.StockShelf); Fx.Sparkle(transform.position + Vector3.up * 0.9f); }
                    c.ResetTransfer();
                }
            });
            // yazıyı sadece sayı değişince kur (her karede yeni string = çöp toplayıcı takılması)
            if (label && (container.Count != shownCount || container.capacity != shownCap))
            {
                shownCount = container.Count; shownCap = container.capacity;
                label.Set(GameConfig.I.Name(type) + " " + shownCount + "/" + shownCap);
            }
        }

        public Item Take() => container.TakeLast(type);
    }
}

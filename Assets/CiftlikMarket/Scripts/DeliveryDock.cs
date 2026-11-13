using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>
    /// Toptancı teslim alanı: kamyonetin getirdiği ürünler burada birikir (karışık türler).
    /// Oyuncu üstüne gelince elinin aldığı kadar alır; depocu çalışan sadece istediği türü alır
    /// (Carrier.wantType). Buraya ürün bırakılmaz.
    /// </summary>
    public class DeliveryDock : Station
    {
        public static DeliveryDock I;
        public ItemContainer pile;
        public WorldLabel label;

        System.Action<Carrier> onCarrier;
        int shown = -1;

        void OnEnable() { I = this; }
        void OnDisable() { if (I == this) I = null; }

        public int Count => pile != null ? pile.Count : 0;
        public int CountOf(ItemType t) => pile != null ? pile.CountOf(t) : 0;

        /// <summary>Kamyonetten gelen ürünü yığına koyar (uçarak gelir).</summary>
        public void Receive(Item it, bool animate = true) { pile.Add(it, animate); }

        void Update()
        {
            ForEachCarrier(onCarrier ??= c =>
            {
                if (!c.ReadyToTransfer() || c.stack.IsFull || pile.Count == 0 || !c.MayCollectFrom(this)) return;
                Item it = c.wantType >= 0 ? pile.TakeLast((ItemType)c.wantType) : pile.TakeLastAny();
                if (it == null) return;
                c.stack.Add(it);
                if (c.isPlayer) { AudioFx.Play(Sfx.Pickup); Fx.Sparkle(pile.transform.position + Vector3.up * 0.6f); }
                c.ResetTransfer();
            });

            if (label && pile.Count != shown)
            {
                shown = pile.Count;
                label.Set(shown > 0 ? "DEPO " + shown : "DEPO");
            }
        }
    }
}

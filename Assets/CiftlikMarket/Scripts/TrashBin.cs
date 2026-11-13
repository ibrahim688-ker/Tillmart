using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>Çöp kutusu: oyuncu alana girince sırtındaki her şey atılır.</summary>
    public class TrashBin : Station
    {
        System.Action<Carrier> onCarrier;   // ForEachCarrier için bir kez oluşturulur (karede çöp ayırmasın)
        public Transform mouth;

        void Update()
        {
            ForEachCarrier(onCarrier ??= c =>
            {
                if (!(c.isPlayer || c.dumpRequest) || !c.ReadyToTransfer()) return;
                var it = c.stack.TakeLastAny();
                if (it == null) return;
                var go = it.gameObject;
                it.FlyTo(mouth, Vector3.zero, 0.25f, () => Destroy(go));
                c.ResetTransfer();
            });
        }
    }
}

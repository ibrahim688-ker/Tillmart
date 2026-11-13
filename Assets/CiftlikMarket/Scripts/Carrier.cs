using System.Collections.Generic;
using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>Ürün taşıyabilen her şey (oyuncu, ileride çalışanlar).</summary>
    public class Carrier : MonoBehaviour
    {
        public static readonly List<Carrier> All = new List<Carrier>();

        public ItemContainer stack;
        public bool isPlayer;
        public bool canCashier = true;
        [Tooltip("True iken raflar ve makineler bu taşıyıcının elindekini almaz (drive-thru görevlisi siparişi taşırken)")]
        public bool holdItems;
        [Tooltip("Çalışan elindekini çöpe atmak istediğinde true olur")]
        public bool dumpRequest;
        [Tooltip("Çalışanlar sadece bu istasyondan ürün toplar (yoldan geçerken toplamasın diye). Oyuncu için yok sayılır.")]
        public Station collectTarget;
        [Tooltip("Teslim alanından sadece bu türü al (-1: hepsi). Depocu kullanır.")]
        public int wantType = -1;

        public bool MayCollectFrom(Station s) => isPlayer || collectTarget == s;

        float transferTimer;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public bool ReadyToTransfer() => transferTimer <= 0f;
        /// <summary>Oyuncunun akış hızı yükseltmesi seviyesi (0–5): ürün alma/bırakma aralığını kısaltır. Çalışanları etkilemez.</summary>
        public static int PlayerFlowLevel;
        public static float FlowInterval(int level) => Mathf.Max(0.03f, GameConfig.I.transferInterval - 0.015f * level);   // 0,12 → 0,045 sn

        public void ResetTransfer() { transferTimer = isPlayer ? FlowInterval(PlayerFlowLevel) : GameConfig.I.transferInterval; }

        void Update()
        {
            if (transferTimer > 0f) transferTimer -= Time.deltaTime;
        }
    }
}
